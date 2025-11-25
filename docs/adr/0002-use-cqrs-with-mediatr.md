# ADR-0002: Use CQRS with MediatR

## Status
Accepted

## Context
We need a pattern for handling business logic that:
- Separates read and write operations
- Provides clear request/response patterns
- Supports cross-cutting concerns (logging, validation)
- Scales well as the application grows

## Decision
We will use CQRS (Command Query Responsibility Segregation) pattern with MediatR library for request handling.

## Consequences

### Positive
- **Separation**: Clear separation between commands (writes) and queries (reads)
- **Scalability**: Can scale reads and writes independently
- **Testability**: Each handler is independently testable
- **Cross-Cutting**: MediatR pipelines support behaviors (logging, validation)
- **Organization**: Features organized by domain (Substations, Users, etc.)

### Negative
- **Complexity**: More files per feature (Command/Query, Handler, Response)
- **Learning Curve**: Team needs to understand CQRS and MediatR
- **Overhead**: May be overkill for simple CRUD operations

## Implementation
- Commands for write operations (Create, Update, Delete)
- Queries for read operations (Get, List)
- MediatR handles request routing
- Handlers contain business logic
- Behaviors for cross-cutting concerns

## Example Structure
```
Features/
  Substations/
    Create/
      CreateSubstationCommand.cs
      CreateSubstationCommandHandler.cs
    List/
      ListSubstationsQuery.cs
      ListSubstationsQueryHandler.cs
```

## References
- MediatR documentation
- CQRS pattern

