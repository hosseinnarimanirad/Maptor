# ADR-0001: Use Clean Architecture

## Status
Accepted

## Context
We need to choose an architecture pattern for the MakanNegarSaba application that ensures:
- Separation of concerns
- Testability
- Maintainability
- Independence from frameworks and UI
- Business logic independence from database

## Decision
We will use Clean Architecture (also known as Onion Architecture) as the architectural pattern for this project.

## Consequences

### Positive
- **Separation of Concerns**: Clear boundaries between layers
- **Testability**: Business logic can be tested without dependencies
- **Maintainability**: Changes in one layer don't affect others
- **Framework Independence**: Can swap frameworks without changing business logic
- **Database Independence**: Can change database without affecting business logic

### Negative
- **Initial Complexity**: More files and structure initially
- **Learning Curve**: Team needs to understand Clean Architecture principles
- **Overhead**: More abstraction layers may seem like overkill for simple features

## Implementation
The project structure follows Clean Architecture with these layers:

1. **Core**: Domain entities and business logic (no dependencies)
2. **Application**: Use cases, DTOs, interfaces (depends on Core)
3. **Infrastructure**: Data access, external services (depends on Application and Core)
4. **Presentation**: API controllers, WPF views (depends on Application and Core)

## References
- Clean Architecture by Robert C. Martin
- ASP.NET Core Clean Architecture templates

