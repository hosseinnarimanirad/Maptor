# Software Requirements Specification (SRS)
## MakanNegarSaba GIS Application

**Version**: 1.0  
**Date**: 2024  
**Project**: MakanNegarSaba (مکان‌نگار صبا)  
**Client**: Barg Regional Power Company (شرکت برق منطقه‌ای باختر)

---

## Table of Contents

1. [Introduction](#introduction)
2. [Overall Description](#overall-description)
3. [System Features](#system-features)
4. [External Interface Requirements](#external-interface-requirements)
5. [System Architecture](#system-architecture)
6. [Non-Functional Requirements](#non-functional-requirements)
7. [Data Requirements](#data-requirements)
8. [Security Requirements](#security-requirements)
9. [Performance Requirements](#performance-requirements)
10. [Usability Requirements](#usability-requirements)
11. [Compatibility Requirements](#compatibility-requirements)
12. [Deployment Requirements](#deployment-requirements)

---

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) document describes the functional and non-functional requirements for the MakanNegarSaba GIS application. This document is intended for developers, testers, project managers, and stakeholders.

### 1.2 Scope

MakanNegarSaba is a Geographic Information System (GIS) application designed for managing and visualizing spatial data related to electrical infrastructure in Iran's power industry. The system consists of:

- **ASP.NET Core Web API**: RESTful API for data access and business logic
- **WPF Desktop Client**: Windows desktop application for map visualization and user interaction
- **SQL Server Database**: Spatial database storing infrastructure data

### 1.3 Definitions, Acronyms, and Abbreviations

- **GIS**: Geographic Information System
- **WPF**: Windows Presentation Foundation
- **API**: Application Programming Interface
- **JWT**: JSON Web Token
- **RBAC**: Role-Based Access Control
- **RSA**: Rivest-Shamir-Adleman encryption algorithm
- **GeoJSON**: Geographic JSON format
- **EPSG**: European Petroleum Survey Group (coordinate system standards)

### 1.4 References

- Clean Architecture principles
- ASP.NET Core documentation
- WPF documentation
- SQL Server spatial data documentation
- GeoJSON specification

### 1.5 Overview

This document is organized into sections covering system features, interfaces, architecture, and requirements. Each requirement is uniquely identified and includes acceptance criteria.

---

## 2. Overall Description

### 2.1 Product Perspective

MakanNegarSaba is a standalone GIS application that integrates with:
- External map tile services (OpenStreetMap, Google Maps, etc.)
- Email service for user verification
- SQL Server database for data persistence

### 2.2 Product Functions

The system provides the following major functions:

1. **User Authentication & Authorization**
   - User registration and email verification
   - Secure login with JWT tokens
   - Role-based access control

2. **Spatial Data Visualization**
   - Interactive map display
   - Multiple layer support
   - Zoom and pan controls
   - Feature labeling

3. **Infrastructure Management**
   - View substations, transmission lines, power stations
   - Search and filter capabilities
   - Feature detail viewing

4. **User & Role Management**
   - User account management
   - Role creation and assignment
   - Permission configuration

5. **Layer Configuration**
   - Layer visibility control
   - Styling configuration
   - Zoom level management

### 2.3 User Classes and Characteristics

1. **System Administrator**
   - Manages users and roles
   - Configures system settings
   - Monitors system security

2. **Power Infrastructure Manager**
   - Views and analyzes infrastructure
   - Makes operational decisions
   - Monitors grid status

3. **GIS Analyst**
   - Performs spatial analysis
   - Creates maps and reports
   - Exports data

4. **Regular User**
   - Views infrastructure data
   - Searches for features
   - Accesses basic functionality

### 2.4 Operating Environment

- **Server**: Windows Server or Linux with .NET 8.0 runtime
- **Client**: Windows 10/11 with .NET 8.0 Desktop Runtime
- **Database**: SQL Server with spatial data support
- **Browser**: Not applicable (desktop application)

### 2.5 Design and Implementation Constraints

- Must use Clean Architecture
- Must support Persian/Farsi language
- Must use JWT for authentication
- Must support spatial data (geometry types)
- Must be compatible with existing Maptor framework

### 2.6 Assumptions and Dependencies

- Users have Windows operating system
- Network connectivity to API server
- SQL Server database is available
- External map tile services are accessible
- Email service is configured for verification

---

## 3. System Features

### 3.1 User Authentication & Authorization

#### 3.1.1 User Registration

**Requirement ID**: REQ-AUTH-001  
**Priority**: Critical

**Description**: Users must be able to register new accounts using email and password.

**Functional Requirements**:
- System shall accept email address and password
- System shall validate email format
- System shall validate password strength
- System shall encrypt password using MD5 with unique stamp
- System shall check for duplicate email addresses
- System shall send verification email
- System shall create user account with `IsActive = false`
- System shall generate unique share code for user

**Inputs**:
- Email address (string)
- Password (string, encrypted)

**Outputs**:
- Success message
- Verification email sent

**Acceptance Criteria**:
- User can register with valid email and password
- Invalid emails are rejected
- Duplicate emails are rejected
- Verification email is sent within 30 seconds

---

#### 3.1.2 Email Verification

**Requirement ID**: REQ-AUTH-002  
**Priority**: Critical

**Description**: Users must verify their email addresses before full account activation.

**Functional Requirements**:
- System shall send verification email upon registration
- System shall generate unique verification token
- System shall validate verification token
- System shall update `IsEmailVerified` flag
- System shall allow resending verification email
- System shall expire tokens after 7 days

**Inputs**:
- Verification token (string)

**Outputs**:
- Verification status
- Account activation status

**Acceptance Criteria**:
- Verification email received within 5 minutes
- Token validation works correctly
- Expired tokens are rejected

---

#### 3.1.3 User Login

**Requirement ID**: REQ-AUTH-003  
**Priority**: Critical

**Description**: Users must be able to authenticate and receive access tokens.

**Functional Requirements**:
- System shall accept email and password
- System shall validate credentials
- System shall check email verification status
- System shall check account active status
- System shall issue JWT token upon success
- System shall track login attempts
- System shall lock account after 5 failed attempts
- System shall record login timestamp and IP address

**Inputs**:
- Email address (string, encrypted)
- Password (string, encrypted)

**Outputs**:
- JWT access token
- User information
- Token expiration time

**Acceptance Criteria**:
- Valid credentials result in successful login
- Invalid credentials result in error message
- Account locks after 5 failed attempts
- JWT token is valid for 24 hours

---

#### 3.1.4 Role-Based Access Control

**Requirement ID**: REQ-AUTH-004  
**Priority**: Critical

**Description**: System shall enforce permissions based on user roles.

**Functional Requirements**:
- System shall support multiple roles per user
- System shall check permissions before allowing actions
- System shall deny access if user lacks required permission
- System shall support role expiration dates
- System shall track who assigned roles

**Inputs**:
- User ID
- Action/Resource being accessed

**Outputs**:
- Access granted or denied

**Acceptance Criteria**:
- Users can only access permitted resources
- Permission changes take effect immediately
- Expired roles are not considered

---

### 3.2 Spatial Data Management

#### 3.2.1 Substation Data Access

**Requirement ID**: REQ-SPATIAL-001  
**Priority**: Critical

**Description**: System shall provide access to substation spatial data.

**Functional Requirements**:
- System shall return substations as GeoJSON features
- System shall support filtering by operational status
- System shall include all substation attributes
- System shall support spatial queries
- System shall respect user permissions

**Inputs**:
- Optional filters (status, region)

**Outputs**:
- GeoJSON FeatureCollection
- Substation features with geometry and attributes

**Acceptance Criteria**:
- All active substations are returned
- Geometry data is accurate
- Attributes are complete
- Response time < 2 seconds for 1000 features

---

#### 3.2.2 Transmission Line Data Access

**Requirement ID**: REQ-SPATIAL-002  
**Priority**: Critical

**Description**: System shall provide access to transmission line spatial data.

**Functional Requirements**:
- System shall return transmission line segments as GeoJSON
- System shall include line attributes (voltage, capacity, etc.)
- System shall support circuit and tower data
- System shall support filtering by status

**Inputs**:
- Optional filters

**Outputs**:
- GeoJSON FeatureCollection
- Line features with LineString geometry

**Acceptance Criteria**:
- All transmission lines are returned
- Line geometry is accurate
- Circuit information is included
- Response time < 3 seconds for 5000 segments

---

#### 3.2.3 Power Station Data Access

**Requirement ID**: REQ-SPATIAL-003  
**Priority**: High

**Description**: System shall provide access to power station data.

**Functional Requirements**:
- System shall return power plants as GeoJSON points
- System shall distinguish between main plants and distributed generation
- System shall include capacity and type information

**Inputs**:
- Optional type filter

**Outputs**:
- GeoJSON FeatureCollection
- Power plant features

**Acceptance Criteria**:
- All power stations are returned
- Types are correctly identified
- Capacity data is accurate

---

#### 3.2.4 Communication Infrastructure Data Access

**Requirement ID**: REQ-SPATIAL-004  
**Priority**: Medium

**Description**: System shall provide access to communication infrastructure data.

**Functional Requirements**:
- System shall return optical fiber routes
- System shall return junction boxes
- System shall return communication towers
- System shall include technical specifications

**Inputs**:
- Optional type filter

**Outputs**:
- GeoJSON FeatureCollection
- Communication features

**Acceptance Criteria**:
- All communication infrastructure is accessible
- Data is complete and accurate

---

### 3.3 Map Visualization

#### 3.3.1 Interactive Map Display

**Requirement ID**: REQ-MAP-001  
**Priority**: Critical

**Description**: System shall display an interactive map with spatial features.

**Functional Requirements**:
- System shall display base map tiles
- System shall overlay spatial features
- System shall support zoom and pan
- System shall support mouse and touch interactions
- System shall maintain good performance with many features

**Inputs**:
- Map extent (bounding box)
- Zoom level
- Visible layers

**Outputs**:
- Rendered map with features

**Acceptance Criteria**:
- Map renders within 1 second
- Smooth zoom and pan (60 FPS)
- Features appear at correct locations
- No visual artifacts

---

#### 3.3.2 Layer Management

**Requirement ID**: REQ-MAP-002  
**Priority**: High

**Description**: System shall support multiple map layers with visibility control.

**Functional Requirements**:
- System shall load layer configurations from database
- System shall support layer groups
- System shall allow toggling layer visibility
- System shall respect zoom level ranges
- System shall apply layer styling

**Inputs**:
- Layer settings from database
- User visibility selections

**Outputs**:
- Visible layers on map

**Acceptance Criteria**:
- Layers load correctly
- Visibility toggles work instantly
- Zoom-based display works correctly
- Styling is applied correctly

---

#### 3.3.3 Feature Labeling

**Requirement ID**: REQ-MAP-003  
**Priority**: High

**Description**: System shall display labels for map features.

**Functional Requirements**:
- System shall display labels based on configuration
- System shall respect label zoom level ranges
- System shall support Persian/Farsi text
- System shall allow label styling (size, color)
- System shall prevent label overlap

**Inputs**:
- Label configuration
- Feature attributes
- Current zoom level

**Outputs**:
- Text labels on map

**Acceptance Criteria**:
- Labels display correctly
- Persian text renders properly
- Labels appear at appropriate zoom levels
- Labels are readable

---

### 3.4 Search Functionality

#### 3.4.1 General Search

**Requirement ID**: REQ-SEARCH-001  
**Priority**: High

**Description**: System shall provide search across all searchable entities.

**Functional Requirements**:
- System shall search by name or GIS ID
- System shall support Persian/Farsi text
- System shall return paginated results
- System shall indicate entity type in results
- System shall allow navigation to feature on map

**Inputs**:
- Search query string
- Page number
- Page size

**Outputs**:
- Paginated search results
- Total count
- Result details

**Acceptance Criteria**:
- Search returns relevant results
- Persian text search works correctly
- Results are paginated correctly
- Response time < 1 second

---

### 3.5 User Management

#### 3.5.1 User Account Management

**Requirement ID**: REQ-USER-001  
**Priority**: Critical

**Description**: Administrators shall manage user accounts.

**Functional Requirements**:
- System shall list all users
- System shall filter users by status
- System shall activate/deactivate accounts
- System shall manually verify emails
- System shall view user login statistics

**Inputs**:
- User ID
- Status changes
- Filter criteria

**Outputs**:
- User list
- User details
- Statistics

**Acceptance Criteria**:
- All users are accessible
- Status changes take effect immediately
- Statistics are accurate

---

#### 3.5.2 Role Management

**Requirement ID**: REQ-USER-002  
**Priority**: Critical

**Description**: Administrators shall create and manage roles.

**Functional Requirements**:
- System shall create new roles
- System shall assign permissions to roles
- System shall update role details
- System shall delete roles (if not system role)
- System shall prevent deletion of roles assigned to users

**Inputs**:
- Role name, display name, description
- Permission IDs

**Outputs**:
- Role information
- Success/failure status

**Acceptance Criteria**:
- Roles can be created successfully
- Permissions are assigned correctly
- System roles cannot be deleted
- Roles with users cannot be deleted

---

#### 3.5.3 User Role Assignment

**Requirement ID**: REQ-USER-003  
**Priority**: Critical

**Description**: Administrators shall assign roles to users.

**Functional Requirements**:
- System shall assign multiple roles to user
- System shall support role expiration dates
- System shall track assignment metadata
- System shall update user permissions immediately

**Inputs**:
- User ID
- Role IDs
- Expiration date (optional)

**Outputs**:
- Assignment confirmation

**Acceptance Criteria**:
- Roles are assigned correctly
- Permissions update immediately
- Expiration dates are respected

---

### 3.6 Layer Configuration

#### 3.6.1 Layer Settings Management

**Requirement ID**: REQ-CONFIG-001  
**Priority**: Medium

**Description**: System shall manage layer display settings.

**Functional Requirements**:
- System shall store layer settings in database
- System shall support styling (colors, stroke)
- System shall support zoom level ranges
- System shall support label configuration
- System shall support layer grouping

**Inputs**:
- Layer configuration data

**Outputs**:
- Layer settings

**Acceptance Criteria**:
- Settings are stored correctly
- Changes are reflected immediately
- Settings persist across sessions

---

## 4. External Interface Requirements

### 4.1 User Interfaces

#### 4.1.1 WPF Desktop Application

**Requirement ID**: REQ-UI-001  
**Priority**: Critical

**Description**: System shall provide Windows desktop application interface.

**Requirements**:
- Interface shall support Persian/Farsi language
- Interface shall use right-to-left layout where appropriate
- Interface shall be responsive and intuitive
- Interface shall provide clear error messages
- Interface shall support keyboard shortcuts

**Screen Requirements**:
- Login dialog
- Main map window
- User management views
- Role management views
- Settings dialogs

---

### 4.2 Hardware Interfaces

**Requirement ID**: REQ-HW-001  
**Priority**: Medium

**Description**: System shall support standard input devices.

**Requirements**:
- Mouse input for map interaction
- Keyboard input for text entry
- Touch input (optional, for touchscreen devices)

---

### 4.3 Software Interfaces

#### 4.3.1 REST API

**Requirement ID**: REQ-API-001  
**Priority**: Critical

**Description**: System shall provide RESTful API.

**Requirements**:
- API shall use JSON for data exchange
- API shall use HTTP/HTTPS protocols
- API shall support CORS (if web clients added)
- API shall provide Swagger documentation
- API shall return standard HTTP status codes

---

#### 4.3.2 Database Interface

**Requirement ID**: REQ-DB-001  
**Priority**: Critical

**Description**: System shall interface with SQL Server database.

**Requirements**:
- System shall use Entity Framework Core
- System shall support spatial data types
- System shall use connection pooling
- System shall handle connection failures gracefully

---

#### 4.3.3 Map Tile Services

**Requirement ID**: REQ-TILE-001  
**Priority**: High

**Description**: System shall interface with external map tile services.

**Requirements**:
- System shall support multiple tile providers
- System shall cache tiles locally
- System shall handle tile service failures
- System shall support different map styles

---

#### 4.3.4 Email Service

**Requirement ID**: REQ-EMAIL-001  
**Priority**: High

**Description**: System shall send verification emails.

**Requirements**:
- System shall send HTML emails
- System shall support Persian/Farsi content
- System shall handle email service failures
- System shall retry failed sends

---

## 5. System Architecture

### 5.1 Architecture Overview

The system follows Clean Architecture principles with the following layers:

1. **Core Layer**: Domain entities and business logic
2. **Application Layer**: Use cases and business rules (CQRS with MediatR)
3. **Infrastructure Layer**: Data access, external services
4. **Presentation Layer**: API controllers, WPF views

### 5.2 Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Frontend**: WPF (.NET 8.0)
- **Database**: SQL Server with spatial support
- **ORM**: Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Architecture Pattern**: Clean Architecture, CQRS

---

## 6. Non-Functional Requirements

### 6.1 Performance Requirements

#### 6.1.1 Response Time

**Requirement ID**: REQ-PERF-001  
**Priority**: High

- API endpoints shall respond within 2 seconds for standard queries
- Map rendering shall maintain 60 FPS during interaction
- Search results shall return within 1 second
- Database queries shall complete within 500ms

#### 6.1.2 Throughput

**Requirement ID**: REQ-PERF-002  
**Priority**: Medium

- System shall support 100 concurrent users
- API shall handle 1000 requests per minute
- Database shall handle 500 queries per second

#### 6.1.3 Scalability

**Requirement ID**: REQ-PERF-003  
**Priority**: Medium

- System shall scale horizontally (multiple API instances)
- Database shall support 1 million features
- System shall handle increasing user load

---

### 6.2 Security Requirements

#### 6.2.1 Authentication

**Requirement ID**: REQ-SEC-001  
**Priority**: Critical

- System shall use JWT tokens for authentication
- Tokens shall expire after 24 hours
- Passwords shall be hashed (MD5 with stamp)
- Login requests shall be encrypted (RSA)

#### 6.2.2 Authorization

**Requirement ID**: REQ-SEC-002  
**Priority**: Critical

- System shall enforce role-based permissions
- Unauthorized access shall be denied
- Permission checks shall occur at API level
- System shall log security events

#### 6.2.3 Data Protection

**Requirement ID**: REQ-SEC-003  
**Priority**: High

- Sensitive data shall be encrypted in transit (HTTPS)
- Passwords shall never be stored in plain text
- User data shall be protected from unauthorized access

---

### 6.3 Reliability Requirements

#### 6.3.1 Availability

**Requirement ID**: REQ-REL-001  
**Priority**: High

- System shall be available 99% of the time
- Planned maintenance windows shall be scheduled
- System shall recover from failures automatically

#### 6.3.2 Error Handling

**Requirement ID**: REQ-REL-002  
**Priority**: High

- System shall handle errors gracefully
- Error messages shall be user-friendly (Persian)
- System shall log errors for debugging
- System shall prevent data corruption

---

### 6.4 Usability Requirements

#### 6.4.1 User Interface

**Requirement ID**: REQ-USE-001  
**Priority**: High

- Interface shall be intuitive and easy to learn
- Interface shall support Persian/Farsi language
- Interface shall provide helpful tooltips
- Interface shall be consistent throughout

#### 6.4.2 Accessibility

**Requirement ID**: REQ-USE-002  
**Priority**: Medium

- Interface shall support keyboard navigation
- Text shall be readable (adequate contrast)
- Interface shall be responsive to user actions

---

### 6.5 Compatibility Requirements

#### 6.5.1 Operating System

**Requirement ID**: REQ-COMP-001  
**Priority**: Critical

- Client application shall run on Windows 10/11
- Server shall run on Windows Server or Linux
- .NET 8.0 runtime required

#### 6.5.2 Database

**Requirement ID**: REQ-COMP-002  
**Priority**: Critical

- System shall support SQL Server 2016 or later
- System shall support spatial data types
- System shall support NetTopologySuite

---

### 6.6 Maintainability Requirements

#### 6.6.1 Code Quality

**Requirement ID**: REQ-MAIN-001  
**Priority**: Medium

- Code shall follow Clean Architecture principles
- Code shall be well-documented
- Code shall follow C# coding standards
- Code shall have unit test coverage > 70%

#### 6.6.2 Documentation

**Requirement ID**: REQ-MAIN-002  
**Priority**: Medium

- API shall have Swagger documentation
- Code shall have XML comments
- System shall have user manual
- System shall have developer guide

---

## 7. Data Requirements

### 7.1 Data Storage

- Spatial data stored in SQL Server with geometry types
- User data stored in relational tables
- Configuration data stored in database
- Cache data stored on file system

### 7.2 Data Formats

- GeoJSON for spatial data exchange
- JSON for API requests/responses
- WKT/WKB for geometry storage

### 7.3 Data Volume

- Estimated 10,000+ substations
- Estimated 50,000+ transmission line segments
- Estimated 1,000+ power stations
- Estimated 100+ users

---

## 8. Security Requirements

### 8.1 Authentication Security

- Passwords must be at least 8 characters
- Account lockout after 5 failed attempts
- JWT tokens expire after 24 hours
- RSA encryption for sensitive requests

### 8.2 Authorization Security

- Permission checks at API level
- Role-based access control
- Audit logging for security events
- Secure session management

### 8.3 Data Security

- Encrypted communication (HTTPS)
- Hashed passwords
- Protected database connections
- Secure file storage

---

## 9. Performance Requirements

### 9.1 Response Times

- API endpoints: < 2 seconds
- Map rendering: 60 FPS
- Search: < 1 second
- Database queries: < 500ms

### 9.2 Resource Usage

- Memory usage: < 2GB for client application
- CPU usage: < 50% during normal operation
- Network bandwidth: Optimized tile caching

---

## 10. Usability Requirements

### 10.1 Language Support

- Primary language: Persian/Farsi
- Right-to-left text layout
- Persian number formatting
- Persian date/time formatting

### 10.2 User Experience

- Intuitive navigation
- Clear error messages
- Helpful tooltips
- Consistent design

---

## 11. Compatibility Requirements

### 11.1 Platform Support

- Windows 10/11 (client)
- Windows Server or Linux (server)
- .NET 8.0 runtime

### 11.2 Database Support

- SQL Server 2016 or later
- Spatial data support required

---

## 12. Deployment Requirements

### 12.1 Server Deployment

- API deployed as web service
- Database deployed on SQL Server
- Configuration via appsettings.json
- Environment-specific settings

### 12.2 Client Deployment

- WPF application installer
- .NET 8.0 Desktop Runtime included
- Configuration file for API endpoint
- Automatic updates (future)

---

## Appendix A: Glossary

- **Substation**: Electrical facility for voltage transformation
- **Transmission Line**: High-voltage power lines
- **Power Station**: Electricity generation facility
- **GIS ID**: Unique identifier for geographic features
- **Feature**: Spatial entity with geometry and attributes

---

## Appendix B: Change History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2024 | Documentation Team | Initial SRS |

---

**Document Status**: Approved  
**Next Review Date**: As needed

