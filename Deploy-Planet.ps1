<#
.SYNOPSIS
    Automatic deployment of the Planet application from a ZIP package dropped on the FAAS share.

.DESCRIPTION
    Script driven by a CTRL-M job that triggers when the Planet.ZIP file arrives
    on the FAAS share (uncFaas\se-xf-in).

    Performed steps:
      1. Read the environment (environment variable "Env": C = QA, P = Production)
      2. Resolve the FAAS UNC according to the environment
      3. Ensure the ZIP is fully transferred (not write-locked)
      4. Read the ProcessList.txt file contained INSIDE the ZIP
      5. Stop (kill) the listed processes in order
      6. Delete the contents of the destination folder
      7. Extract the ZIP into the destination folder
      8. Start PlanetRunner.exe with the correct working directory (app.config)
      9. Release the lock and return a status code to CTRL-M (0 = OK, <> 0 = KO)

.NOTES
    Product     : Planet
    Author      : Planet Team / Operations
    Version     : 1.0
    Prerequisites: PowerShell 5.1+ (Windows)
                  The CTRL-M service account must have the following rights:
                    - Read/Write on the relevant FAAS shares
                    - Kill process right (Stop-Process) on the server
                    - Right to launch PlanetRunner.exe
    Convention  : Exit codes follow the CTRL-M standard (0 = OK, <>0 = ABEND/KO)

.PARAMETER EnvOverride
    Allows forcing the environment ("C" or "P") from the command line for testing.
    If not provided, the script reads the "Env" environment variable.

.EXAMPLE
    # Standard call from CTRL-M (environment comes from the Env variable)
    powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\Deploy-Planet.ps1"

.EXAMPLE
    # Manual test forcing the QA environment
    powershell.exe -ExecutionPolicy Bypass -NoProfile -File "C:\Scripts\Deploy-Planet.ps1" -EnvOverride "C"
#>

[CmdletBinding()]
param(
    [string]$EnvOverride
)

# ============================================================================
#  CONFIGURATION - All configurable values are located here.
#  Environment-dependent values are split between QA / PROD.
# ============================================================================

# --- File / folder names (common to all environments) ---
$ZipFileName        = "Planet.ZIP"                                   # Name of the ZIP package expected on the FAAS
$ProcessListFile    = "ProcessList.txt"                              # Process list file contained INSIDE the ZIP
$FaasInSubPath      = "se-xf-in"                                     # FAAS subfolder where the ZIP arrives
$FaasExecSubPath    = "se-planet\executables"                        # Target deployment folder
$RunnerSubPath      = "PlanetRunner\PlanetRunner.exe"               # Main executable to launch after deployment
$RunnerWorkSubPath  = "PlanetRunner"                                # Working directory (so C# can find app.config)
$LockFileName       = "Deploy-Planet.lock"                          # Lock file to prevent concurrent execution

# --- FAAS UNC per environment (DIFFERENT in QA vs PROD) ---
$UncFaasByEnv = @{
    "C" = "\\srv-faas-qa.local\partage$"        # FAAS UNC - Qualification (C = QA)
    "P" = "\\srv-faas-prod.local\partage$"      # FAAS UNC - Production
}

# --- Behavior parameters ---
$FileTransferWaitSec     = 120      # Max duration (s) to wait for a ZIP transfer to complete
$FileLockCheckIntervalSec = 5       # Interval (s) between two ZIP lock checks
$PostKillWaitMs          = 5000     # Delay (ms) after kill before deletion (release file handles)
$ProcessKillTimeoutMs    = 30000    # Timeout (ms) waiting for a process to actually stop after Stop-Process

# ============================================================================
#  SCRIPT GLOBAL VARIABLES
# ============================================================================
$ErrorActionPreference = "Stop"     # Uncaptured errors cause the script to fail
$script:ExitCode       = 0          # 0 = OK, <> 0 = KO (returned to CTRL-M)
$script:HadError       = $false     # Cumulative error indicator
$script:LockFile       = $null      # Lock file path

# ============================================================================
#  UTILITY FUNCTIONS
# ============================================================================

function Write-Log {
    <#
        .SYNOPSIS
            Writes a timestamped message to the console (stdout) with a severity level.
            CTRL-M captures stdout/stderr for its job logs.
    #>
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Message,

        [ValidateSet("INFO", "WARN", "ERROR", "STEP", "SUCCESS")]
        [string]$Level = "INFO"
    )

    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
    $prefix    = switch ($Level) {
        "INFO"    { "[INFO]   " }
        "WARN"    { "[WARN]   " }
        "ERROR"   { "[ERROR]  " }
        "STEP"    { "[STEP]   " }
        "SUCCESS" { "[SUCCESS]" }
    }

    $line = "$timestamp $prefix $Message"

    switch ($Level) {
        "ERROR"   { Write-Host $line -ForegroundColor Red }
        "WARN"    { Write-Host $line -ForegroundColor Yellow }
        "STEP"    { Write-Host $line -ForegroundColor Cyan }
        "SUCCESS" { Write-Host $line -ForegroundColor Green }
        default   { Write-Host $line -ForegroundColor White }
    }

    # Errors also go to stderr (visible in CTRL-M logs)
    if ($Level -eq "ERROR") { Write-Error $line }
}

function Set-ScriptError {
    <#
        .SYNOPSIS
            Marks the script as failed (without interrupting immediately) by setting
            the exit code that will be returned to CTRL-M at the end.
    #>
    param(
        [string]$Message,
        [int]$Code = 1
    )
    Write-Log $Message -Level "ERROR"
    $script:HadError = $true
    if ($Code -gt $script:ExitCode) { $script:ExitCode = $Code }
}

function Write-LogStep {
    param([string]$Message)
    Write-Log $Message -Level "STEP"
}

function Resolve-Environment {
    <#
        .SYNOPSIS
            Determines the target environment (QA or PROD) from the -EnvOverride
            parameter or, failing that, from the "Env" environment variable.
    #>
    $envValue = if ($EnvOverride) { $EnvOverride.Trim().ToUpper() } else { $env:Env }
    if ([string]::IsNullOrWhiteSpace($envValue)) {
        throw "Unable to determine the environment: the 'Env' environment variable is not defined (expected values: C = QA, P = Production)."
    }
    $envValue = $envValue.Trim().ToUpper()
    if (-not $UncFaasByEnv.ContainsKey($envValue)) {
        throw "Unrecognized environment value: '$envValue'. Valid values: C (QA), P (Production)."
    }
    $envLabel = switch ($envValue) { "C" { "Qualification (QA)" } "P" { "Production (PROD)" } default { "Unknown" } }
    Write-Log "Detected environment: $envValue - $envLabel"
    return $envValue
}

function Join-FaasPath {
    param([string]$Root, [string]$Relative)
    return (Join-Path $Root $Relative)
}

function Test-ZipFullyTransferred {
    <#
        .SYNOPSIS
            Checks that a file is no longer being written by trying to obtain
            exclusive access to it. Retries for $FileTransferWaitSec.
            Useful because CTRL-M may trigger as soon as the file appears
            while the transfer is not yet complete.
    #>
    param([string]$Path)

    $deadline = (Get-Date).AddSeconds($FileTransferWaitSec)
    do {
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
            $stream.Close()
            $stream.Dispose()
            return $true
        }
        catch {
            Write-Log "File '$Path' is still locked (transfer in progress?). Retrying in $FileLockCheckIntervalSec s..." -Level "WARN"
            Start-Sleep -Seconds $FileLockCheckIntervalSec
        }
    } while ((Get-Date) -lt $deadline)

    return $false
}

function Get-ProcessListFromZip {
    <#
        .SYNOPSIS
            Reads the contents of ProcessList.txt directly INSIDE the ZIP
            (without extracting everything) and returns an ordered array of
            process names. The file order is preserved.

            Accepted lines:
              - Executable name (planet.exe, Process_Blue.exe ...)
              - Lines starting with '-' or '*' (bullets): the prefix is removed
              - Comments: empty lines or lines starting with '#'
    #>
    param([string]$ZipPath, [string]$EntryName)

    Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null

    $processes = [System.Collections.Generic.List[string]]::new()

    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    }
    catch {
        throw "Unable to open ZIP '$ZipPath' for reading: $($_.Exception.Message)"
    }

    try {
        # Case-insensitive search of the entry (the zip may store relative paths)
        $entry = $archive.Entries | Where-Object {
            $_.Name -ieq $EntryName
        } | Select-Object -First 1

        if (-not $entry) {
            throw "File '$EntryName' was not found in ZIP '$ZipPath'."
        }

        Write-Log "Reading '$($entry.FullName)' from the ZIP."

        $reader = New-Object System.IO.StreamReader($entry.Open())
        try {
            while (-not $reader.EndOfStream) {
                $line = $reader.ReadLine()
                if ($null -eq $line) { continue }

                $line = $line.Trim()
                if ($line -eq "") { continue }
                if ($line.StartsWith("#")) { continue }

                # Remove optional bullets: '- planet.exe' -> 'planet.exe'
                $line = $line -replace '^\s*[-*]\s*', ''

                if ($line -ne "") {
                    $processes.Add($line.Trim())
                }
            }
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }

    return $processes
}

function Get-ProcessNameWithoutExtension {
    <#
        .SYNOPSIS
            Returns the process name without the .exe extension
            (Get-Process expects the name without extension).
            'planet.exe' -> 'planet'
    #>
    param([string]$ExeName)
    return [System.IO.Path]::GetFileNameWithoutExtension($ExeName)
}

function Stop-PlanetProcess {
    <#
        .SYNOPSIS
            Kills all processes matching the given name, preserving the order.
            - No error if the process does not exist (nominal case).
            - Error (abend) if Stop-Process fails or if the process does not stop
              within the allotted time.
    #>
    param([string]$ExeName)

    $procName = Get-ProcessNameWithoutExtension -ExeName $ExeName
    Write-Log "Searching for process '$procName' (from '$ExeName')..."

    $procs = @()
    try {
        $procs = @(Get-Process -Name $procName -ErrorAction SilentlyContinue)
    }
    catch {
        # Get-Process may throw if the name contains invalid characters
        Set-ScriptError "Error while searching for process '$procName': $($_.Exception.Message)" -Code 10
        return
    }

    if ($procs.Count -eq 0) {
        Write-Log "Process '$procName' is not currently running. Nothing to stop."
        return
    }

    foreach ($p in $procs) {
        try {
            Write-Log "Stopping process '$($p.ProcessName)' (PID $($p.Id))..."
            $p | Stop-Process -Force -ErrorAction Stop

            # Wait for the process to actually exit (release of handles / DLLs)
            $p.WaitForExit($ProcessKillTimeoutMs) | Out-Null
            if (-not $p.HasExited) {
                Set-ScriptError "Process '$procName' (PID $($p.Id)) did not stop within the allotted time ($ProcessKillTimeoutMs ms)." -Code 11
            }
            else {
                Write-Log "Process '$($p.ProcessName)' (PID $($p.Id)) stopped successfully." -Level "SUCCESS"
            }
        }
        catch {
            Set-ScriptError "Unable to stop process '$procName' (PID $($p.Id)): $($_.Exception.Message)" -Code 12
        }
    }
}

function Clear-Destination {
    <#
        .SYNOPSIS
            Deletes the contents (files + subfolders) of the destination folder.
            The folder itself is recreated to guarantee a clean target.
    #>
    param([string]$DestinationPath)

    Write-Log "Deleting contents of '$DestinationPath'..."
    try {
        if (Test-Path -LiteralPath $DestinationPath) {
            Remove-Item -LiteralPath $DestinationPath -Recurse -Force -ErrorAction Stop
            Write-Log "Contents of '$DestinationPath' deleted."
        }
        New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
        Write-Log "Destination folder (re)created: '$DestinationPath'." -Level "SUCCESS"
    }
    catch {
        Set-ScriptError "Failed to delete/create '$DestinationPath': $($_.Exception.Message)" -Code 30
        throw  # Blocking error: cannot deploy to a locked target
    }
}

function Expand-ZipToDestination {
    <#
        .SYNOPSIS
            Extracts the ZIP entirely into the destination folder.
            Uses System.IO.Compression for reliability and performance.
    #>
    param([string]$ZipPath, [string]$DestinationPath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null

    Write-Log "Extracting '$ZipPath' to '$DestinationPath'..."
    try {
        # ExtractToDirectory fails if the target already exists with content -> already cleaned
        [System.IO.Compression.ZipFile]::ExtractToDirectory($ZipPath, $DestinationPath)
        Write-Log "Extraction completed." -Level "SUCCESS"
    }
    catch [System.IO.IOException] {
        Set-ScriptError "I/O error during extraction (check long paths / FAAS rights): $($_.Exception.Message)" -Code 40
        throw
    }
    catch {
        Set-ScriptError "Failed to extract '$ZipPath': $($_.Exception.Message)" -Code 41
        throw
    }
}

function Start-PlanetRunner {
    <#
        .SYNOPSIS
            Starts PlanetRunner.exe with the working directory set to the
            PlanetRunner folder, so the C# application can find its app.config
            file and its local resources.
    #>
    param(
        [string]$ExePath,
        [string]$WorkingDir
    )

    if (-not (Test-Path -LiteralPath $ExePath)) {
        Set-ScriptError "Executable '$ExePath' not found after deployment." -Code 60
        return
    }

    if (-not (Test-Path -LiteralPath $WorkingDir)) {
        Set-ScriptError "Working directory '$WorkingDir' not found after deployment." -Code 61
        return
    }

    Write-Log "Starting '$ExePath' (WorkingDir = '$WorkingDir')..."

    try {
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName               = $ExePath
        $startInfo.WorkingDirectory       = $WorkingDir
        $startInfo.UseShellExecute        = $false  # required to redirect streams reliably
        $startInfo.CreateNoWindow         = $true

        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo

        $started = $process.Start()
        if ($started) {
            Write-Log "PlanetRunner.exe started (PID $($process.Id))." -Level "SUCCESS"
        }
        else {
            Set-ScriptError "Launch of PlanetRunner.exe failed (Start returned false)." -Code 62
        }
    }
    catch {
        Set-ScriptError "Error while starting PlanetRunner.exe: $($_.Exception.Message)" -Code 63
    }
}

function New-LockFile {
    <#
        .SYNOPSIS
            Creates a lock file to prevent two concurrent executions of the
            script (overlapping CTRL-M jobs).
    #>
    param([string]$LockPath)

    if (Test-Path -LiteralPath $LockPath) {
        # Check whether the owning process still exists (orphan lock)
        $staleContent = Get-Content -LiteralPath $LockPath -ErrorAction SilentlyContinue
        if ($staleContent -match "PID=(\d+)") {
            $ownerPid = $Matches[1]
            if (Get-Process -Id $ownerPid -ErrorAction SilentlyContinue) {
                throw "Another execution of the script is in progress (lock held by PID $ownerPid). Aborting."
            }
            else {
                Write-Log "Orphan lock detected (PID $ownerPid not found). Removing stale lock." -Level "WARN"
                Remove-Item -LiteralPath $LockPath -Force -ErrorAction SilentlyContinue
            }
        }
    }

    try {
        "$env:COMPUTERNAME PID=$PID Date=$(Get-Date)" | Out-File -LiteralPath $LockPath -Force -ErrorAction Stop
        Write-Log "Lock acquired: '$LockPath'."
    }
    catch {
        throw "Unable to create lock file '$LockPath': $($_.Exception.Message)"
    }
}

function Remove-LockFile {
    param([string]$LockPath)
    if ($LockPath -and (Test-Path -LiteralPath $LockPath)) {
        try {
            Remove-Item -LiteralPath $LockPath -Force -ErrorAction Stop
        }
        catch {
            Write-Log "Unable to remove lock (non-blocking): $($_.Exception.Message)" -Level "WARN"
        }
    }
}

# ============================================================================
#  MAIN PROGRAM
# ============================================================================

try {
    Write-LogStep "==== PLANET DEPLOYMENT START ===="
    Write-Log "Host        : $env:COMPUTERNAME"
    Write-Log "User        : $env:USERNAME"
    Write-Log "PID         : $PID"

    # ---------------------------------------------------------------
    # STEP 1 - Resolve environment and FAAS UNC
    # ---------------------------------------------------------------
    Write-LogStep "[1/8] Resolving environment"
    $envValue = Resolve-Environment
    $uncFaas  = $UncFaasByEnv[$envValue]
    Write-Log "Selected FAAS UNC: $uncFaas"

    # Build full paths
    $zipPath         = Join-FaasPath -Root $uncFaas -Relative (Join-FaasPath -Root $FaasInSubPath -Relative $ZipFileName)
    $destinationPath = Join-FaasPath -Root $uncFaas -Relative $FaasExecSubPath
    $runnerExePath   = Join-FaasPath -Root $destinationPath -Relative $RunnerSubPath
    $runnerWorkDir   = Join-FaasPath -Root $destinationPath -Relative $RunnerWorkSubPath
    $script:LockFile = Join-FaasPath -Root $uncFaas -Relative (Join-FaasPath -Root $FaasInSubPath -Relative $LockFileName)

    Write-Log "ZIP inbox path      : $zipPath"
    Write-Log "Destination folder  : $destinationPath"
    Write-Log "Runner executable   : $runnerExePath"
    Write-Log "Working directory   : $runnerWorkDir"

    # ---------------------------------------------------------------
    # STEP 2 - Concurrency lock
    # ---------------------------------------------------------------
    Write-LogStep "[2/8] Acquiring execution lock"
    New-LockFile -LockPath $script:LockFile

    # ---------------------------------------------------------------
    # STEP 3 - Check ZIP presence and integrity
    # ---------------------------------------------------------------
    Write-LogStep "[3/8] Checking ZIP package"
    if (-not (Test-Path -LiteralPath $zipPath)) {
        throw "ZIP file '$zipPath' not found on the FAAS. Aborting."
    }
    Write-Log "ZIP present: '$zipPath'."

    # Ensure the transfer is complete (file not locked)
    Write-Log "Checking ZIP transfer completion..."
    if (-not (Test-ZipFullyTransferred -Path $zipPath)) {
        throw "ZIP file '$zipPath' is still locked after $FileTransferWaitSec s. Transfer likely incomplete. Aborting."
    }
    Write-Log "ZIP is accessible (transfer complete)." -Level "SUCCESS"

    # ---------------------------------------------------------------
    # STEP 4 - Read the process list from the ZIP
    # ---------------------------------------------------------------
    Write-LogStep "[4/8] Reading process list ($ProcessListFile)"
    $processList = Get-ProcessListFromZip -ZipPath $zipPath -EntryName $ProcessListFile

    if ($processList.Count -eq 0) {
        Write-Log "The process list is empty. Nothing to stop." -Level "WARN"
    }
    else {
        Write-Log "Processes to stop ($($processList.Count)), in order:"
        for ($i = 0; $i -lt $processList.Count; $i++) {
            Write-Log ("  {0}. {1}" -f ($i + 1), $processList[$i])
        }
    }

    # ---------------------------------------------------------------
    # STEP 5 - Stop processes in order
    # ---------------------------------------------------------------
    Write-LogStep "[5/8] Stopping running processes"
    foreach ($exeName in $processList) {
        Stop-PlanetProcess -ExeName $exeName
    }

    # Safety delay for the release of file handles / DLLs
    if ($processList.Count -gt 0) {
        Write-Log "Waiting $PostKillWaitMs ms for handles to be released..."
        Start-Sleep -Milliseconds $PostKillWaitMs
    }

    # ---------------------------------------------------------------
    # STEP 6 - Cleanup / preparation of the target
    # ---------------------------------------------------------------
    Write-LogStep "[6/8] Cleaning the destination folder"
    Clear-Destination -DestinationPath $destinationPath

    # ---------------------------------------------------------------
    # STEP 7 - Extract the ZIP
    # ---------------------------------------------------------------
    Write-LogStep "[7/8] Extracting ZIP into the destination folder"
    try {
        Expand-ZipToDestination -ZipPath $zipPath -DestinationPath $destinationPath
    }
    catch {
        throw
    }

    # Check the presence of the main executable after extraction
    if (-not (Test-Path -LiteralPath $runnerExePath)) {
        Write-Log "WARNING: '$runnerExePath' missing after extraction." -Level "WARN"
    }

    # ---------------------------------------------------------------
    # STEP 8 - Start PlanetRunner.exe
    # ---------------------------------------------------------------
    Write-LogStep "[8/8] Starting PlanetRunner.exe"
    Start-PlanetRunner -ExePath $runnerExePath -WorkingDir $runnerWorkDir
}
catch {
    # Any uncaught error: make sure to surface it to CTRL-M
    Set-ScriptError "ABEND: $($_.Exception.Message)" -Code 99
}
finally {
    # Release the lock in all cases
    Remove-LockFile -LockPath $script:LockFile

    # ----------------------------------------------------------------
    # SUMMARY / EXIT CODE FOR CTRL-M
    # ----------------------------------------------------------------
    Write-LogStep "==== DEPLOYMENT SUMMARY ===="
    if ($script:HadError) {
        Write-Log "Deployment ended in FAILURE (KO). See errors above." -Level "ERROR"
        Write-Log "Exit code returned to CTRL-M: $($script:ExitCode) (KO / ABEND)" -Level "ERROR"
    }
    else {
        Write-Log "Deployment ended with SUCCESS (OK)." -Level "SUCCESS"
        Write-Log "Exit code returned to CTRL-M: 0 (OK)" -Level "SUCCESS"
    }
    Write-LogStep "==== PLANET DEPLOYMENT END ===="

    exit $script:ExitCode
}
