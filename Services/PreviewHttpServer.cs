using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownEditor.Services
{
    public sealed class PreviewHttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _listenTask;
        private volatile string _markdown = string.Empty;
        private long _version;
        private volatile int _cursorLine = 1;

        public int Port { get; }
        public string Url => $"http://localhost:{Port}/";

        public PreviewHttpServer()
        {
            Port = FindAvailablePort();
            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();
            _listenTask = Task.Run(() => ListenLoop(_cts.Token));
        }

        public void UpdateContent(string markdown)
        {
            _markdown = markdown ?? string.Empty;
            var newVersion = Interlocked.Increment(ref _version);
            NotifySseClients(newVersion, _cursorLine);
        }

        public void UpdateCursorLine(int line)
        {
            _cursorLine = line;
        }

        private static int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleRequestAsync(ctx, ct), ct);
                }
                catch (ObjectDisposedException) { break; }
                catch (HttpListenerException) { break; }
                catch { }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            try
            {
                var path = ctx.Request.Url?.AbsolutePath ?? "/";

                switch (path)
                {
                    case "/":
                        await WriteResponseAsync(ctx, "text/html; charset=utf-8",
                            HtmlTemplateService.GetLiveTemplate());
                        break;

                    case "/content":
                        var v = Interlocked.Read(ref _version);
                        var cl = _cursorLine;
                        var escaped = EscapeJsonString(_markdown);
                        var json = string.Format(CultureInfo.InvariantCulture,
                            "{{\"v\":{0},\"cursorLine\":{1},\"md\":\"{2}\"}}", v, cl, escaped);
                        await WriteResponseAsync(ctx, "application/json; charset=utf-8", json);
                        break;

                    case "/events":
                        await HandleSseAsync(ctx, ct);
                        break;

                    default:
                        ctx.Response.StatusCode = 404;
                        ctx.Response.Close();
                        break;
                }
            }
            catch
            {
                try { ctx.Response.Close(); } catch { }
            }
        }

        private static async Task WriteResponseAsync(HttpListenerContext ctx, string contentType, string body)
        {
            var buffer = Encoding.UTF8.GetBytes(body);
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = buffer.Length;
            ctx.Response.Headers["Cache-Control"] = "no-store";
            await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            ctx.Response.Close();
        }

        private readonly object _sseLock = new object();
        private volatile List<HttpListenerResponse> _sseClients = new List<HttpListenerResponse>();
        private int _disposed;

        private void NotifySseClients(long version, int cursorLine)
        {
            List<HttpListenerResponse> clients;
            lock (_sseLock)
            {
                clients = _sseClients;
            }
            var data = $"event: version\ndata: {version}\n\nevent: cursor\ndata: {cursorLine}\n\n";
            var buffer = Encoding.UTF8.GetBytes(data);
            List<HttpListenerResponse>? dead = null;
            foreach (var client in clients)
            {
                try
                {
                    client.OutputStream.Write(buffer, 0, buffer.Length);
                    client.OutputStream.Flush();
                }
                catch
                {
                    dead ??= new List<HttpListenerResponse>();
                    dead.Add(client);
                }
            }
            if (dead != null)
            {
                lock (_sseLock)
                {
                    var updated = new List<HttpListenerResponse>(_sseClients);
                    foreach (var d in dead) updated.Remove(d);
                    _sseClients = updated;
                }
                foreach (var d in dead)
                    try { d.Close(); } catch { }
            }
        }

        private async Task HandleSseAsync(HttpListenerContext ctx, CancellationToken ct)
        {
            ctx.Response.ContentType = "text/event-stream; charset=utf-8";
            ctx.Response.Headers["Cache-Control"] = "no-cache";
            ctx.Response.Headers["Connection"] = "keep-alive";

            var hello = $": connected\n\nevent: version\ndata: {Interlocked.Read(ref _version)}\n\n";
            var helloBuffer = Encoding.UTF8.GetBytes(hello);
            await ctx.Response.OutputStream.WriteAsync(helloBuffer, 0, helloBuffer.Length);
            ctx.Response.OutputStream.Flush();

            lock (_sseLock)
            {
                var newList = new List<HttpListenerResponse>(_sseClients) { ctx.Response };
                _sseClients = newList;
            }

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(1000, ct);
                }
            }
            catch { }
            finally
            {
                lock (_sseLock)
                {
                    _sseClients = new List<HttpListenerResponse>(_sseClients);
                    _sseClients.Remove(ctx.Response);
                }
                try { ctx.Response.Close(); } catch { }
            }
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            var sb = new StringBuilder(s.Length + 64);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"':  sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '<':  sb.Append("\\u003C"); break;
                    case '>':  sb.Append("\\u003E"); break;
                    case '\u2028': sb.Append("\\u2028"); break;
                    case '\u2029': sb.Append("\\u2029"); break;
                    default:
                        if (c < ' ')
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _cts.Cancel();
            try { _listener.Stop(); } catch { }
            try { _listener.Close(); } catch { }
            List<HttpListenerResponse> clients;
            lock (_sseLock)
            {
                clients = _sseClients;
                _sseClients = new List<HttpListenerResponse>();
            }
            foreach (var c in clients)
                try { c.Close(); } catch { }
            _cts.Dispose();
        }
    }
}
