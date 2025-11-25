# MakanNegarSaba Documentation

Welcome to the MakanNegarSaba (مکان‌نگار صبا) documentation. This directory contains comprehensive documentation for the GIS application developed for Barg Regional Power Company.

---

## Documentation Index

### Core Documentation

- **[Software Requirements Specification (SRS)](srs/srs.md)**  
  Complete functional and non-functional requirements for the system.

- **[Statement of Work (SOW)](sow.md)**  
  Project scope, deliverables, timeline, and responsibilities.

- **[API Endpoint Documentation](api/endpoints.md)**  
  Complete REST API reference with examples and authentication details.

- **[Database ER Diagram](database/er-diagram.md)**  
  Entity relationship diagram and database schema documentation.

### User Documentation

- **[User Stories](user-stories/user-stories.md)**  
  Comprehensive user stories organized by epics with acceptance criteria.

- **[User Journey Maps](user-journey-maps/)**  
  Detailed user journey maps for key personas:
  - [New User Registration Journey](user-journey-maps/new-user-journey.md)
  - [Power Infrastructure Manager Journey](user-journey-maps/power-manager-journey.md)
  - [System Administrator Journey](user-journey-maps/administrator-journey.md)
  - [GIS Analyst Journey](user-journey-maps/gis-analyst-journey.md)

### Process Documentation

- **[Change Request Template](change-requests/template.md)**  
  Template for documenting and tracking change requests.

- **[Change Request Example](change-requests/example-cr-001.md)**  
  Example change request for reference.

- **[Documentation Maintenance Guide](MAINTENANCE.md)**  
  Guide for updating and maintaining documentation.

### Technical Documentation

- **[Developer Guide](developer-guide/README.md)**  
  Setup instructions, coding standards, and development practices.

- **[Architecture Decision Records (ADRs)](adr/)**  
  Documented architectural decisions:
  - [ADR-0001: Use Clean Architecture](adr/0001-use-clean-architecture.md)
  - [ADR-0002: Use CQRS with MediatR](adr/0002-use-cqrs-with-mediatr.md)
  - [ADR-0003: Use JWT for Authentication](adr/0003-use-jwt-for-authentication.md)

- **[Security Documentation](security/security-overview.md)**  
  Security architecture, authentication, authorization, and best practices.

- **[Deployment Guide](deployment/deployment-guide.md)**  
  Step-by-step deployment instructions for all environments.

---

## Quick Start

### For Developers

1. Read the [Developer Guide](developer-guide/README.md)
2. Review [Architecture Decision Records](adr/)
3. Check [API Documentation](api/endpoints.md) for endpoints
4. Review [Database ER Diagram](database/er-diagram.md) for data model

### For Project Managers

1. Review [Statement of Work](sow.md) for project scope
2. Check [User Stories](user-stories/user-stories.md) for features
3. Review [User Journey Maps](user-journey-maps/) for user experience
4. Use [Change Request Template](change-requests/template.md) for changes

### For System Administrators

1. Read [Deployment Guide](deployment/deployment-guide.md)
2. Review [Security Documentation](security/security-overview.md)
3. Check [API Documentation](api/endpoints.md) for integration

### For Business Analysts

1. Review [Software Requirements Specification](srs/srs.md)
2. Check [User Stories](user-stories/user-stories.md)
3. Review [User Journey Maps](user-journey-maps/)

---

## Documentation Structure

```
docs/
├── README.md (this file)
├── MAINTENANCE.md
├── sow.md
├── api/
│   └── endpoints.md
├── database/
│   └── er-diagram.md
├── user-stories/
│   └── user-stories.md
├── user-journey-maps/
│   ├── new-user-journey.md
│   ├── power-manager-journey.md
│   ├── administrator-journey.md
│   └── gis-analyst-journey.md
├── srs/
│   └── srs.md
├── change-requests/
│   ├── template.md
│   └── example-cr-001.md
├── developer-guide/
│   └── README.md
├── adr/
│   ├── 0001-use-clean-architecture.md
│   ├── 0002-use-cqrs-with-mediatr.md
│   └── 0003-use-jwt-for-authentication.md
├── security/
│   └── security-overview.md
└── deployment/
    └── deployment-guide.md
```

---

## Updating Documentation

See the [Documentation Maintenance Guide](MAINTENANCE.md) for instructions on how to update documentation.

**Quick Request Format**:
- "Update API docs for new endpoint `/api/Substation/GetById`"
- "Add user story for map export feature"
- "Update ER diagram after adding table `MaintenanceLog`"
- "Regenerate API docs after endpoint changes"

---

## Documentation Standards

- **Format**: Markdown (.md files)
- **Diagrams**: Mermaid syntax for ER diagrams and journey maps
- **Code Examples**: Syntax-highlighted code blocks
- **Links**: Relative paths for internal links
- **Version Control**: All documentation in Git repository

---

## Additional Resources

### External Documentation

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [JWT Authentication](https://jwt.io/)

### Internal Resources

- Source Code: `src/IRI.App/Barg/`
- API Swagger UI: `http://localhost:{port}/swagger` (when running)
- Database Migrations: `Infrastructure/IRI.App.MakanNegarSaba.Ef/Migrations/`

---

## Contributing to Documentation

1. Follow the [Documentation Maintenance Guide](MAINTENANCE.md)
2. Use consistent formatting and style
3. Include examples where helpful
4. Keep documentation up-to-date with code changes
5. Request review before committing major changes

---

## Questions?

- Check the relevant documentation section
- Review the [Maintenance Guide](MAINTENANCE.md)
- Ask in team chat or create an issue

---

**Last Updated**: 2024  
**Documentation Version**: 1.0

