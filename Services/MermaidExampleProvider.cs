using System.Collections.Generic;
using MarkdownEditor.Models;

namespace MarkdownEditor.Services
{
    public static class MermaidExampleProvider
    {
        public static IReadOnlyList<MermaidExample> GetExamples()
        {
            return new List<MermaidExample>
            {
                new MermaidExample("Flowchart", "Flowchart with nodes and connections",
@"```mermaid
graph TD
    A[Start] --> B{Condition ?}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
```"),
                new MermaidExample("Sequence Diagram", "Sequence diagram between actors",
@"```mermaid
sequenceDiagram
    participant U as User
    participant S as Server
    participant DB as Database
    U->>S: HTTP request
    S->>DB: SELECT query
    DB-->>S: Results
    S-->>U: JSON response
```"),
                new MermaidExample("Class Diagram", "UML class diagram",
@"```mermaid
classDiagram
    class Animal {
        +String name
        +int age
        +eat()
        +sleep()
    }
    class Cat {
        +meow()
    }
    class Dog {
        +bark()
    }
    Animal <|-- Cat
    Animal <|-- Dog
```"),
                new MermaidExample("State Diagram", "State machine",
@"```mermaid
stateDiagram-v2
    [*] --> Inactive
    Inactive --> Active : Start
    Active --> Paused : Pause
    Paused --> Active : Resume
    Active --> Finished : Stop
    Finished --> [*]
```"),
                new MermaidExample("ER Diagram", "Entity-relationship diagram",
@"```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ ORDER_LINE : contains
    PRODUCT ||--o{ ORDER_LINE : ""is in""
    CUSTOMER {
        int id PK
        string name
        string email
    }
    ORDER {
        int id PK
        date orderDate
        float total
    }
    PRODUCT {
        int id PK
        string name
        float price
    }
```"),
                new MermaidExample("User Journey", "User journey",
@"```mermaid
journey
    title Purchase journey
    section Navigation
        Visit site: 5: User
        Search product: 4: User
    section Purchase
        Add to cart: 3: User
        Payment: 2: User
        Confirmation: 5: User
```"),
                new MermaidExample("Gantt Chart", "Gantt chart for planning",
@"```mermaid
gantt
    title Project Plan
    dateFormat  YYYY-MM-DD
    section Design
        Analysis         :a1, 2025-01-01, 15d
        Design           :a2, after a1, 10d
    section Development
        Backend          :b1, after a2, 20d
        Frontend         :b2, after a2, 18d
    section Tests
        Unit tests       :c1, after b1, 10d
        Integration      :c2, after c1, 5d
```"),
                new MermaidExample("Pie Chart", "Pie chart",
@"```mermaid
pie title Budget breakdown
    ""Development"" : 45
    ""Design"" : 20
    ""Marketing"" : 15
    ""Infrastructure"" : 12
    ""Support"" : 8
```"),
                new MermaidExample("Git Graph", "Visualisation de branches Git",
@"```mermaid
gitGraph
    commit
    commit
    branch develop
    checkout develop
    commit
    commit
    branch feature
    checkout feature
    commit
    checkout develop
    merge feature
    checkout main
    merge develop
    commit
```"),
                new MermaidExample("Mindmap", "Mindmap / Brainstorming",
@"```mermaid
mindmap
    root((Project))
        Architecture
            Frontend
            Backend
            Database
        Team
            Developers
            Designers
            QA
        Planning
            Sprint 1
            Sprint 2
            Release
```"),
                new MermaidExample("Timeline", "Timeline of events",
@"```mermaid
timeline
    title Project timeline
    2024-Q1 : Design : Requirements analysis
    2024-Q2 : Development : Phase 1
    2024-Q3 : Testing : Acceptance
    2024-Q4 : Deployment : Production release
```"),
                new MermaidExample("Requirement Diagram", "Requirement diagram",
@"```mermaid
requirementDiagram
    requirement req_auth {
        id: REQ-001
        text: The system must authenticate users
        risk: high
        verifymethod: test
    }
    requirement req_perf {
        id: REQ-002
        text: Response time < 200ms
        risk: medium
        verifymethod: analysis
    }
    element app_server {
        type: ""Application Server""
    }
    app_server - satisfies -> req_auth
    app_server - satisfies -> req_perf
```"),

                new MermaidExample("Quadrant Chart", "Quadrant chart — effort / impact matrix",
@"```mermaid
quadrantChart
    title Effort vs Impact
    x-axis Low Effort --> High Effort
    y-axis Low Impact --> High Impact
    quadrant-1 Plan carefully
    quadrant-2 Do it now
    quadrant-3 Avoid
    quadrant-4 Delegate
    Feature A: [0.20, 0.75]
    Feature B: [0.60, 0.85]
    Feature C: [0.40, 0.30]
    Feature D: [0.75, 0.25]
    Feature E: [0.55, 0.60]
```"),

                new MermaidExample("XY Chart", "XY chart — bar and line series (beta)",
@"```mermaid
xychart-beta
    title Monthly Revenue
    x-axis [Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec]
    y-axis Revenue 0 --> 120
    bar [45, 52, 48, 61, 70, 83, 90, 78, 65, 72, 88, 95]
    line [42, 50, 48, 60, 68, 80, 88, 76, 64, 70, 85, 92]
```"),

                new MermaidExample("C4 Context", "C4 model — system context diagram",
@"```mermaid
C4Context
    title System Context
    Person(user, ""Customer"", ""An end user of the application"")
    System(app, ""Web Application"", ""Core system"")
    System_Ext(mail, ""E-Mail Service"", ""External provider"")
    SystemDb_Ext(db, ""Mainframe"", ""Legacy banking backend"")
    Rel(user, app, ""Uses"", ""HTTPS"")
    Rel(app, mail, ""Sends emails via"", ""SMTP"")
    Rel(app, db, ""Reads / writes"", ""TCP"")
```"),

                new MermaidExample("Sankey Diagram", "Sankey — energy or flow visualisation (beta)",
@"```mermaid
sankey-beta
    Solar,Electricity grid,80
    Wind,Electricity grid,60
    Electricity grid,Homes,70
    Electricity grid,Industry,45
    Electricity grid,Losses,25
```"),

                new MermaidExample("Block Diagram", "Block diagram — system building blocks (beta)",
@"```mermaid
block-beta
    columns 3
    frontend[""Frontend""]
    gateway[""API Gateway""]
    backend[""Backend""]
    db[""Database""]
    cache[""Cache""]
    auth[""Auth""]
    frontend --> gateway
    gateway --> backend
    gateway --> auth
    backend --> db
    backend --> cache
```"),

                new MermaidExample("Architecture Diagram", "Architecture — services and groups (beta)",
@"```mermaid
architecture-beta
    group cloud[""Cloud""]
    service db(database)[""Database""] in cloud
    service api(server)[""API Server""] in cloud
    service store(disk)[""Storage""] in cloud
    service cdn(internet)[""CDN""]
    cdn:R --> L:api
    api:B --> T:db
    api:R --> L:store
```"),

                new MermaidExample("Packet Diagram", "Packet — network packet field layout (beta)",
@"```mermaid
packet-beta
    title IPv4 Header
    0-3: ""Version""
    4-7: ""IHL""
    8-15: ""DSCP / ECN""
    16-31: ""Total Length""
    32-47: ""Identification""
    48-50: ""Flags""
    51-63: ""Fragment Offset""
    64-71: ""TTL""
    72-79: ""Protocol""
    80-95: ""Header Checksum""
    96-127: ""Source IP""
    128-159: ""Destination IP""
```"),

                new MermaidExample("Kanban", "Kanban — task board",
@"```mermaid
kanban
    todo[""To Do""]
        t1[""Write unit tests""]
        t2[""Update README""]
    inprogress[""In Progress""]
        t3[""Implement auth""]
        t4[""Code review""]
    done[""Done""]
        t5[""Setup CI/CD""]
        t6[""Deploy to staging""]
```"),

                new MermaidExample("Treemap", "Treemap — hierarchical data (beta)",
@"```mermaid
treemap-beta
    title Codebase Size
    Frontend
        Components 420
        Pages 180
        Hooks 95
    Backend
        Controllers 310
        Services 270
        Repositories 140
    Shared
        Utils 85
        Types 60
```"),

                new MermaidExample("ZenUML Sequence", "ZenUML — alternative sequence diagram",
@"```mermaid
zenuml
    title API Call Flow
    @Actor Client
    @Boundary API
    @Database DB
    Client -> API: GET /users
    API -> DB: SELECT users
    DB --> API: rows
    API --> Client: 200 OK
```"),
            };
        }
    }
}
