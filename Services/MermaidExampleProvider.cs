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
            };
        }
    }
}
