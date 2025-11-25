# Statement of Work (SOW)
## MakanNegarSaba GIS Application

**Project Name**: MakanNegarSaba (مکان‌نگار صبا)  
**Client**: Barg Regional Power Company (شرکت برق منطقه‌ای باختر)  
**Version**: 1.0  
**Date**: 2024

---

## 1. Project Overview

### 1.1 Purpose

This Statement of Work (SOW) defines the scope, deliverables, timeline, and responsibilities for the development and delivery of the MakanNegarSaba GIS application. MakanNegarSaba is a Geographic Information System designed to manage and visualize spatial data related to electrical infrastructure in Iran's power industry.

### 1.2 Background

Barg Regional Power Company requires a comprehensive GIS solution to manage, visualize, and analyze electrical infrastructure including substations, transmission lines, power stations, and communication networks. The system must support multiple user roles, provide secure access control, and enable efficient spatial data management.

### 1.3 Objectives

The primary objectives of this project are:

1. Develop a robust GIS application for electrical infrastructure management
2. Provide secure user authentication and role-based access control
3. Enable efficient visualization and analysis of spatial data
4. Support Persian/Farsi language throughout the application
5. Ensure scalability and maintainability through Clean Architecture

---

## 2. Scope of Work

### 2.1 In-Scope

#### 2.1.1 Backend API Development

- **ASP.NET Core Web API** with Clean Architecture
- RESTful API endpoints for:
  - User authentication and authorization
  - Spatial data access (substations, transmission lines, power stations, communications)
  - User and role management
  - Search functionality
  - Layer configuration
- JWT-based authentication
- Role-based access control (RBAC)
- Swagger/OpenAPI documentation
- Database integration with Entity Framework Core
- Spatial data support (NetTopologySuite)

#### 2.1.2 Desktop Client Application

- **WPF Desktop Application** (.NET 8.0)
- Interactive map interface with:
  - Base map display
  - Spatial feature overlay
  - Zoom and pan controls
  - Layer management
  - Feature labeling
- User authentication interface
- User management interface (for administrators)
- Role management interface (for administrators)
- Settings and configuration dialogs
- Persian/Farsi language support

#### 2.1.3 Database Design and Implementation

- **SQL Server Database** with spatial data support
- Database schema design
- Entity relationships
- Spatial data storage
- Migration scripts
- Seed data for roles and permissions

#### 2.1.4 Security Implementation

- User registration with email verification
- Secure password storage (MD5 with stamp)
- RSA encryption for sensitive communications
- JWT token authentication
- Role-based permission system
- Account lockout mechanism
- Audit logging

#### 2.1.5 Documentation

- Software Requirements Specification (SRS)
- API endpoint documentation
- Database ER diagram
- User stories and journey maps
- Developer guide
- User manual
- Deployment guide

### 2.2 Out-of-Scope

The following items are explicitly out of scope for this project:

1. **Mobile Applications**: iOS or Android mobile apps
2. **Web-Based Client**: Browser-based web application (future phase)
3. **Real-Time Updates**: WebSocket-based real-time data updates
4. **Advanced Spatial Analysis**: Buffer analysis, overlay operations, network analysis
5. **Data Editing**: Create, update, delete operations for spatial features
6. **Reporting Module**: Advanced reporting and dashboard features
7. **Integration with External Systems**: SCADA, ERP, or other enterprise systems
8. **Multi-Tenancy**: Support for multiple organizations
9. **Offline Mode**: Full offline functionality for desktop client
10. **Automated Testing**: Comprehensive automated test suite (unit, integration, E2E)

---

## 3. Deliverables

### 3.1 Software Deliverables

#### 3.1.1 Backend API

- **Deliverable**: ASP.NET Core Web API application
- **Format**: Source code, compiled binaries, configuration files
- **Location**: Git repository
- **Components**:
  - Core domain layer
  - Application layer (CQRS with MediatR)
  - Infrastructure layer (EF Core, external services)
  - Presentation layer (API controllers)
  - Configuration files (appsettings.json)
  - Dockerfile (if containerized)

#### 3.1.2 Desktop Client Application

- **Deliverable**: WPF desktop application
- **Format**: Source code, installer package, executable
- **Location**: Git repository, installer distribution
- **Components**:
  - WPF views and view models
  - Map visualization components
  - User interface components
  - Application configuration
  - Installer package (.msi or .exe)

#### 3.1.3 Database

- **Deliverable**: SQL Server database
- **Format**: Database schema, migration scripts, seed data
- **Location**: Database server, migration files in repository
- **Components**:
  - Database schema (tables, indexes, constraints)
  - Entity Framework migrations
  - Seed data scripts
  - Spatial data configuration

### 3.2 Documentation Deliverables

#### 3.2.1 Technical Documentation

- **Software Requirements Specification (SRS)**
- **API Endpoint Documentation** (OpenAPI/Swagger)
- **Database ER Diagram** (Mermaid, PNG)
- **Architecture Documentation**
- **Developer Guide**

#### 3.2.2 User Documentation

- **User Stories**
- **User Journey Maps**
- **User Manual** (Persian/Farsi)
- **Administrator Guide**

#### 3.2.3 Project Documentation

- **Statement of Work (this document)**
- **Change Request Template**
- **Deployment Guide**
- **Maintenance Guide**

### 3.3 Configuration and Deployment

- **Configuration Files**: Environment-specific settings
- **Deployment Scripts**: Automated deployment scripts (if applicable)
- **Installation Guide**: Step-by-step installation instructions
- **Environment Setup**: Development, staging, production configurations

---

## 4. Technical Requirements

### 4.1 Technology Stack

#### Backend
- **Framework**: ASP.NET Core 8.0
- **Architecture**: Clean Architecture
- **ORM**: Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Spatial Data**: NetTopologySuite
- **API Documentation**: Swagger/OpenAPI

#### Frontend
- **Framework**: WPF (.NET 8.0)
- **UI Framework**: MahApps.Metro (if used)
- **Map Library**: Custom integration with Maptor framework
- **MVVM Pattern**: ViewModel-based architecture

#### Database
- **Database**: SQL Server 2016 or later
- **Spatial Support**: Geometry data types
- **Migration Tool**: Entity Framework Migrations

### 4.2 Performance Requirements

- API response time: < 2 seconds for standard queries
- Map rendering: 60 FPS during interaction
- Search response time: < 1 second
- Support for 100 concurrent users
- Handle 10,000+ spatial features efficiently

### 4.3 Security Requirements

- JWT token authentication
- Role-based access control
- Encrypted password storage
- RSA encryption for sensitive communications
- HTTPS for API communication
- Account lockout after failed login attempts
- Audit logging for security events

### 4.4 Compatibility Requirements

- **Client**: Windows 10/11
- **Server**: Windows Server or Linux
- **Database**: SQL Server 2016 or later
- **.NET Runtime**: .NET 8.0

---

## 5. Timeline and Milestones

### 5.1 Project Phases

#### Phase 1: Foundation (Weeks 1-4)
- **Milestone**: Project setup and architecture
- **Deliverables**:
  - Project structure
  - Database schema design
  - Core domain entities
  - Basic API structure

#### Phase 2: Authentication & Authorization (Weeks 5-8)
- **Milestone**: User management complete
- **Deliverables**:
  - User registration and login
  - Email verification
  - Role and permission system
  - User management API

#### Phase 3: Spatial Data API (Weeks 9-12)
- **Milestone**: Spatial data endpoints complete
- **Deliverables**:
  - Substation endpoints
  - Transmission line endpoints
  - Power station endpoints
  - Communication endpoints
  - Search functionality

#### Phase 4: Desktop Client - Core (Weeks 13-16)
- **Milestone**: Basic map functionality
- **Deliverables**:
  - Map display
  - Layer management
  - Feature visualization
  - Basic interactions

#### Phase 5: Desktop Client - Advanced (Weeks 17-20)
- **Milestone**: Full client functionality
- **Deliverables**:
  - User management UI
  - Role management UI
  - Search interface
  - Settings and configuration

#### Phase 6: Testing & Documentation (Weeks 21-24)
- **Milestone**: System ready for deployment
- **Deliverables**:
  - Testing completed
  - Documentation finalized
  - Deployment packages
  - User training materials

### 5.2 Key Milestones

| Milestone | Target Date | Deliverables |
|-----------|-------------|--------------|
| M1: Architecture Complete | Week 4 | Project structure, database design |
| M2: Authentication Complete | Week 8 | User management, RBAC |
| M3: API Complete | Week 12 | All API endpoints |
| M4: Client Core Complete | Week 16 | Basic map functionality |
| M5: Client Complete | Week 20 | Full client application |
| M6: System Ready | Week 24 | Testing, documentation, deployment |

---

## 6. Roles and Responsibilities

### 6.1 Development Team

#### Backend Developers
- Design and implement API endpoints
- Implement business logic
- Database design and migrations
- Security implementation
- API documentation

#### Frontend Developers
- Design and implement WPF application
- Map visualization
- User interface design
- Client-side integration
- User experience optimization

#### Database Administrator
- Database design review
- Performance optimization
- Backup and recovery planning
- Migration support

### 6.2 Project Management

#### Project Manager
- Project planning and coordination
- Risk management
- Stakeholder communication
- Timeline management
- Quality assurance

#### Business Analyst
- Requirements gathering
- User story creation
- Acceptance criteria definition
- User acceptance testing coordination

### 6.3 Client Responsibilities

- Provide business requirements
- Provide access to infrastructure data
- Review and approve deliverables
- Participate in user acceptance testing
- Provide feedback and change requests
- Ensure availability of technical resources (database access, etc.)

---

## 7. Success Criteria

The project will be considered successful when:

1. ✅ All functional requirements are implemented and tested
2. ✅ All API endpoints are functional and documented
3. ✅ Desktop client application is fully functional
4. ✅ User authentication and authorization work correctly
5. ✅ Spatial data is displayed accurately on the map
6. ✅ System performance meets specified requirements
7. ✅ Security requirements are met
8. ✅ Documentation is complete and accurate
9. ✅ User acceptance testing is passed
10. ✅ System is deployed to production environment

---

## 8. Quality Assurance

### 8.1 Testing Requirements

- **Unit Testing**: Core business logic
- **Integration Testing**: API endpoints
- **System Testing**: End-to-end workflows
- **User Acceptance Testing**: Client validation
- **Performance Testing**: Load and stress testing
- **Security Testing**: Penetration testing, vulnerability assessment

### 8.2 Code Quality

- Code reviews for all changes
- Adherence to coding standards
- Clean Architecture principles
- Documentation standards
- Code coverage targets

### 8.3 Documentation Quality

- Technical accuracy
- Completeness
- Usability
- Persian/Farsi language support
- Regular updates

---

## 9. Risk Management

### 9.1 Identified Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Data migration issues | High | Medium | Early data analysis, test migrations |
| Performance issues with large datasets | High | Medium | Performance testing, optimization |
| Third-party service dependencies | Medium | Low | Fallback options, caching |
| Scope creep | Medium | High | Change control process |
| Resource availability | Medium | Medium | Resource planning, backup resources |
| Security vulnerabilities | High | Low | Security reviews, penetration testing |

### 9.2 Risk Mitigation Strategies

- Regular risk assessment meetings
- Early identification and escalation
- Contingency planning
- Regular communication with stakeholders
- Proactive issue resolution

---

## 10. Change Management

### 10.1 Change Request Process

All changes to scope, timeline, or deliverables must be submitted through the Change Request (CR) process:

1. Submit Change Request form
2. Impact analysis
3. Approval/rejection decision
4. Implementation if approved
5. Documentation update

### 10.2 Change Request Template

See `docs/change-requests/template.md` for the Change Request template.

---

## 11. Communication Plan

### 11.1 Communication Channels

- **Project Meetings**: Weekly status meetings
- **Email**: For formal communications
- **Issue Tracking**: Git issues or project management tool
- **Documentation**: Shared documentation repository

### 11.2 Reporting

- **Weekly Status Reports**: Progress, issues, risks
- **Milestone Reports**: Milestone completion summaries
- **Final Report**: Project completion summary

---

## 12. Acceptance Criteria

### 12.1 Deliverable Acceptance

Each deliverable must meet the following criteria:

1. Meets specified requirements
2. Passes quality assurance testing
3. Documentation is complete
4. Code is reviewed and approved
5. No critical bugs remain
6. Performance requirements met
7. Security requirements met

### 12.2 Final Acceptance

Final project acceptance requires:

1. All deliverables completed and accepted
2. User acceptance testing passed
3. Production deployment successful
4. Documentation complete
5. Knowledge transfer completed
6. Sign-off from client

---

## 13. Payment and Terms

### 13.1 Payment Schedule

[To be defined based on contract]

### 13.2 Terms and Conditions

[To be defined based on contract]

---

## 14. Support and Maintenance

### 14.1 Warranty Period

[To be defined - typically 30-90 days post-deployment]

### 14.2 Support Levels

- **Critical Issues**: Response within 4 hours
- **High Priority**: Response within 24 hours
- **Medium Priority**: Response within 3 business days
- **Low Priority**: Response within 1 week

### 14.3 Maintenance

- Bug fixes during warranty period
- Security patches
- Performance optimizations
- Documentation updates

---

## 15. Appendices

### Appendix A: Glossary

See SRS document for glossary of terms.

### Appendix B: References

- Clean Architecture principles
- ASP.NET Core documentation
- WPF documentation
- SQL Server spatial data documentation

### Appendix C: Contact Information

[Project team contact information]

---

## Sign-Off

**Client Representative**: _________________ Date: _________

**Project Manager**: _________________ Date: _________

---

**Document Status**: Draft / Approved  
**Version**: 1.0  
**Last Updated**: 2024

