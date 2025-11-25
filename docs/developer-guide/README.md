# Developer Guide - MakanNegarSaba

## Overview

This guide helps developers understand the codebase structure, setup development environment, coding standards, and contribution process for the MakanNegarSaba project.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Project Structure](#project-structure)
3. [Development Environment Setup](#development-environment-setup)
4. [Coding Standards](#coding-standards)
5. [Architecture Overview](#architecture-overview)
6. [Common Tasks](#common-tasks)
7. [Testing](#testing)
8. [Debugging](#debugging)
9. [Contributing](#contributing)

---

## Getting Started

### Prerequisites

- **.NET 8.0 SDK**: Latest version
- **Visual Studio 2022** or **VS Code**: IDE
- **SQL Server**: 2016 or later (with spatial support)
- **Git**: Version control
- **Postman** or **Swagger**: API testing (optional)

### Clone Repository

```bash
git clone [repository-url]
cd 100.IRI.Maptor
```

---

## Project Structure

### Solution Structure

```
src/IRI.App/Barg/
├── Application/                    # Application layer (use cases)
│   └── IRI.App.MakanNegarSaba.Application/
│       ├── Features/               # CQRS features organized by domain
│       ├── Dtos/                   # Data Transfer Objects
│       └── Gateways/               # Repository interfaces
├── Core/                           # Domain layer
│   └── IRI.App.MakanNegarSaba.Core/
│       ├── Entities/               # Domain entities
│       ├── Common/                 # Shared domain logic
│       └── Exceptions/             # Domain exceptions
├── Infrastructure/                 # Infrastructure layer
│   ├── IRI.App.MakanNegarSaba.Ef/ # Entity Framework
│   ├── IRI.App.MakanNegarSaba.Mongo/
│   ├── IRI.App.MakanNegarSaba.Kafka/
│   └── IRI.App.MakanNegarSaba.Grpc/
├── Presentation/                   # Presentation layer
│   ├── IRI.App.MakanNegarSaba.Api/        # API project
│   └── IRI.App.MakanNegarSaba.Presentation/ # API controllers
└── IRI.App.MakanNegarSaba/        # WPF desktop client
    ├── View/                       # WPF views
    ├── ViewModel/                  # ViewModels
    └── Services/                   # Client services
```

### Clean Architecture Layers

1. **Core**: Domain entities, business logic (no dependencies)
2. **Application**: Use cases, DTOs (depends on Core)
3. **Infrastructure**: Data access, external services (depends on Application, Core)
4. **Presentation**: Controllers, Views (depends on Application, Core)

---

## Development Environment Setup

### 1. Install .NET 8.0 SDK

Download and install from: https://dotnet.microsoft.com/download

Verify installation:
```bash
dotnet --version
```

### 2. Install SQL Server

Install SQL Server 2016 or later with spatial data support.

### 3. Configure Database

1. Create database:
```sql
CREATE DATABASE Barg;
```

2. Update connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Barg;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
  }
}
```

### 4. Run Migrations

```bash
cd src/IRI.App/Barg/Presentation/IRI.App.MakanNegarSaba.Api
dotnet ef database update --project ../../Infrastructure/IRI.App.MakanNegarSaba.Ef
```

### 5. Run API

```bash
cd src/IRI.App/Barg/Presentation/IRI.App.MakanNegarSaba.Api
dotnet run
```

API will be available at: `http://localhost:5000` (or configured port)

Swagger UI: `http://localhost:5000/swagger`

### 6. Run WPF Client

```bash
cd src/IRI.App/Barg/IRI.App.MakanNegarSaba
dotnet run
```

Or open solution in Visual Studio and run.

---

## Coding Standards

### C# Coding Conventions

- Use **PascalCase** for classes, methods, properties
- Use **camelCase** for local variables, parameters
- Use **PascalCase** for constants
- Use **_camelCase** for private fields

### Naming Conventions

- **Entities**: `Substat`, `TrLineSeg`, `User`
- **DTOs**: `UserDto`, `SubstationDto`
- **Commands**: `CreateSubstationCommand`
- **Queries**: `ListSubstationsQuery`
- **Handlers**: `CreateSubstationCommandHandler`
- **Controllers**: `SubstationController`
- **Repositories**: `ISubstationRepository`

### File Organization

- One class per file
- File name matches class name
- Organize by feature/domain

### Code Style

```csharp
// Good
public class SubstationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubstationController> _logger;

    public SubstationController(
        IMediator mediator,
        ILogger<SubstationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
}

// Bad
public class SubstationController:ControllerBase{
    private IMediator mediator;
    public SubstationController(IMediator m){mediator=m;}
}
```

### Comments and Documentation

- Use XML comments for public APIs
- Explain "why" not "what" in comments
- Keep comments up-to-date with code

```csharp
/// <summary>
/// Gets all substations in the system.
/// </summary>
/// <param name="activeOnly">Filter for active substations only</param>
/// <returns>List of substations</returns>
[HttpGet]
public async Task<ActionResult<List<SubstationDto>>> GetSubstations(bool activeOnly = false)
{
    // Implementation
}
```

---

## Architecture Overview

### Clean Architecture

The project follows Clean Architecture principles:

- **Dependency Rule**: Dependencies point inward (Core has no dependencies)
- **Separation of Concerns**: Each layer has a specific responsibility
- **Independence**: Business logic independent of frameworks and UI

### CQRS Pattern

- **Commands**: Write operations (Create, Update, Delete)
- **Queries**: Read operations (Get, List)
- **MediatR**: Handles request routing

### Example Feature Structure

```
Features/Substations/Create/
├── CreateSubstationCommand.cs          # Command
├── CreateSubstationCommandHandler.cs  # Handler
└── CreateSubstationResponse.cs         # Response
```

---

## Common Tasks

### Adding a New API Endpoint

1. **Create Command/Query**:
```csharp
// Application/Features/Substations/GetById/GetSubstationByIdQuery.cs
public class GetSubstationByIdQuery : IRequest<SubstationDto>
{
    public int Id { get; set; }
}
```

2. **Create Handler**:
```csharp
// Application/Features/Substations/GetById/GetSubstationByIdQueryHandler.cs
public class GetSubstationByIdQueryHandler 
    : IRequestHandler<GetSubstationByIdQuery, SubstationDto>
{
    private readonly IQueryRepository _repository;
    
    public async Task<SubstationDto> Handle(
        GetSubstationByIdQuery request, 
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

3. **Add Controller Endpoint**:
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<SubstationDto>> GetSubstation(int id)
{
    var query = new GetSubstationByIdQuery { Id = id };
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### Adding a New Entity

1. **Create Entity** in `Core/Entities/`:
```csharp
public class NewEntity : IHasKey<int>, IFeatureEntity
{
    public int Id { get; set; }
    public Geometry SHAPE { get; set; }
    // Properties
}
```

2. **Add DbSet** to `BargContext.cs`:
```csharp
public virtual DbSet<NewEntity> NewEntities { get; set; }
```

3. **Create Configuration** in `Infrastructure/Ef/Configurations/`:
```csharp
public class NewEntityConfiguration : IEntityTypeConfiguration<NewEntity>
{
    public void Configure(EntityTypeBuilder<NewEntity> builder)
    {
        // Configuration
    }
}
```

4. **Create Migration**:
```bash
dotnet ef migrations add AddNewEntity --project Infrastructure/IRI.App.MakanNegarSaba.Ef
```

### Adding a New Permission

1. **Add to Permission enum** in `Core/Common/Permissions/`:
```csharp
public enum Permission
{
    // Existing permissions
    NewFeatureView = 100,
    NewFeatureCreate = 101,
}
```

2. **Update PermissionExtensions** if needed
3. **Seed permissions** in `RolePermissionConfiguration.cs`

---

## Testing

### Unit Testing

Create unit tests for business logic:

```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsSuccess()
{
    // Arrange
    var command = new CreateSubstationCommand { /* ... */ };
    var handler = new CreateSubstationCommandHandler(/* ... */);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.True(result.Success);
}
```

### Integration Testing

Test API endpoints:

```csharp
[Fact]
public async Task GetSubstations_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/Substation");
    
    // Assert
    response.EnsureSuccessStatusCode();
}
```

---

## Debugging

### API Debugging

1. Set breakpoints in controller or handler
2. Run API in debug mode
3. Use Swagger UI or Postman to trigger requests
4. Step through code

### WPF Client Debugging

1. Set breakpoints in ViewModel or service
2. Run WPF application in debug mode
3. Interact with UI to trigger code
4. Use Visual Studio debugger tools

### Database Debugging

- Use SQL Server Profiler to see queries
- Check Entity Framework logs
- Use SQL Server Management Studio

---

## Contributing

### Git Workflow

1. **Create Feature Branch**:
```bash
git checkout -b feature/new-feature
```

2. **Make Changes**:
- Write code
- Write tests
- Update documentation

3. **Commit Changes**:
```bash
git add .
git commit -m "feat: add new feature"
```

4. **Push and Create Pull Request**:
```bash
git push origin feature/new-feature
```

### Commit Message Format

Follow conventional commits:
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation
- `refactor:` Code refactoring
- `test:` Tests
- `chore:` Maintenance

### Code Review

- All code must be reviewed before merging
- Address review feedback
- Ensure tests pass
- Update documentation if needed

---

## Resources

### Documentation
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [MediatR](https://github.com/jbogard/MediatR)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

### Internal Documentation
- API Documentation: `docs/api/endpoints.md`
- ER Diagram: `docs/database/er-diagram.md`
- Architecture Decisions: `docs/adr/`

---

## Getting Help

- Check this guide first
- Review existing code for examples
- Ask in team chat
- Create issue for questions

---

**Last Updated**: 2024

