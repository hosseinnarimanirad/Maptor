# User Stories - MakanNegarSaba

## Overview

This document contains user stories organized by epics for the MakanNegarSaba GIS application. Each story follows the format: "As a [persona], I want to [goal] so that [benefit]."

**Story Point Scale**: 1, 2, 3, 5, 8, 13 (Fibonacci)

**Priority**: Critical, High, Medium, Low

---

## Epic 1: User Authentication & Authorization

### US-1.1: User Registration
**As a** new user  
**I want to** register with my email address and password  
**So that** I can create an account and access the system

**Acceptance Criteria**:
- User can enter email address and password
- System validates email format
- System validates password strength
- System encrypts password before storage
- System sends verification email
- User receives confirmation message
- Duplicate email addresses are rejected

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: None

---

### US-1.2: Email Verification
**As a** newly registered user  
**I want to** verify my email address  
**So that** I can activate my account and access all features

**Acceptance Criteria**:
- User receives verification email after registration
- Email contains verification link/token
- User can click link to verify email
- System updates user's verification status
- Unverified users cannot login
- User can request resend of verification email

**Priority**: Critical  
**Story Points**: 3  
**Dependencies**: US-1.1

---

### US-1.3: User Login
**As a** registered user  
**I want to** login securely with my email and password  
**So that** I can access the application and my data

**Acceptance Criteria**:
- User can enter email and password
- System validates credentials
- System issues JWT token upon successful login
- System tracks login attempts
- System locks account after 5 failed attempts
- System records login timestamp and IP address
- User receives error message for invalid credentials

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: US-1.1, US-1.2

---

### US-1.4: Password Security
**As a** user  
**I want to** have my password securely stored  
**So that** my account is protected from unauthorized access

**Acceptance Criteria**:
- Passwords are hashed using MD5 with unique stamp
- Passwords are never stored in plain text
- Password hashing includes user-specific salt (stamp)
- System prevents password reuse attacks

**Priority**: Critical  
**Story Points**: 3  
**Dependencies**: US-1.1

---

### US-1.5: Account Lockout
**As a** system administrator  
**I want to** have accounts automatically locked after failed login attempts  
**So that** brute force attacks are prevented

**Acceptance Criteria**:
- System tracks failed login attempts per user
- Account locks after 5 consecutive failed attempts
- Account remains locked for 30 minutes
- System automatically unlocks account after timeout
- Admin can manually unlock accounts
- System logs all lockout events

**Priority**: High  
**Story Points**: 3  
**Dependencies**: US-1.3

---

### US-1.6: Role Management
**As a** system administrator  
**I want to** create and manage user roles  
**So that** I can control access to different features

**Acceptance Criteria**:
- Admin can create new roles with name and description
- Admin can assign permissions to roles
- Admin can update role details
- Admin can deactivate roles
- System roles cannot be deleted
- Roles assigned to users cannot be deleted
- Admin can view all roles and their permissions

**Priority**: Critical  
**Story Points**: 8  
**Dependencies**: US-1.3

---

### US-1.7: Permission Assignment
**As a** system administrator  
**I want to** assign permissions to roles  
**So that** I can control what actions each role can perform

**Acceptance Criteria**:
- Admin can view all available permissions
- Permissions are organized by category
- Admin can assign multiple permissions to a role
- Admin can remove permissions from roles
- System validates permission assignments
- Permission changes take effect immediately

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: US-1.6

---

### US-1.8: User Role Assignment
**As a** system administrator  
**I want to** assign roles to users  
**So that** users have appropriate access levels

**Acceptance Criteria**:
- Admin can view all users
- Admin can assign multiple roles to a user
- Admin can remove roles from users
- Admin can set role expiration dates
- System tracks who assigned roles and when
- User permissions update immediately upon role assignment

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: US-1.6, US-1.7

---

### US-1.9: User Account Management
**As a** system administrator  
**I want to** manage user accounts  
**So that** I can activate, deactivate, and verify user accounts

**Acceptance Criteria**:
- Admin can view list of all users
- Admin can filter users by active status
- Admin can activate/deactivate user accounts
- Admin can manually verify user emails
- Admin can view user login statistics
- Admin can see user registration date and last login

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-1.3

---

### US-1.10: Login Statistics
**As a** system administrator  
**I want to** view user login statistics  
**So that** I can monitor user activity and security

**Acceptance Criteria**:
- Admin can view first login date for each user
- Admin can view last login date for each user
- Admin can view total login count per user
- System tracks login IP addresses
- System tracks login timestamps
- Statistics are available via API endpoint

**Priority**: Medium  
**Story Points**: 3  
**Dependencies**: US-1.3

---

## Epic 2: Spatial Data Visualization

### US-2.1: View Substations on Map
**As a** GIS analyst  
**I want to** view substations on an interactive map  
**So that** I can see their geographic locations and relationships

**Acceptance Criteria**:
- Map displays substations as point features
- Substations are styled according to layer settings
- User can zoom and pan the map
- Substations appear/disappear based on zoom level
- User can click substation to view details
- Map shows substation labels at appropriate zoom levels

**Priority**: Critical  
**Story Points**: 8  
**Dependencies**: US-1.3

---

### US-2.2: View Transmission Lines
**As a** GIS analyst  
**I want to** view transmission lines on the map  
**So that** I can analyze the power grid network

**Acceptance Criteria**:
- Map displays transmission line segments as line features
- Lines are styled with different colors by voltage
- User can see line names and paths
- Lines appear at appropriate zoom levels
- User can click lines to view details
- Map shows circuit information when available

**Priority**: Critical  
**Story Points**: 8  
**Dependencies**: US-1.3

---

### US-2.3: View Power Stations
**As a** power grid operator  
**I want to** view power stations on the map  
**So that** I can see generation facilities and their locations

**Acceptance Criteria**:
- Map displays power plants as point features
- Different types of power plants are visually distinct
- User can view both main power plants and distributed generation
- Power plant labels show capacity information
- User can filter by power plant type

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-1.3

---

### US-2.4: Layer Visibility Control
**As a** map user  
**I want to** control which layers are visible  
**So that** I can focus on relevant information

**Acceptance Criteria**:
- User can toggle layer visibility on/off
- Layers are organized in groups
- User can expand/collapse layer groups
- Layer visibility persists during session
- Map updates immediately when layers are toggled

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-2.1, US-2.2, US-2.3

---

### US-2.5: Zoom-Based Layer Display
**As a** map user  
**I want to** see layers appear/disappear based on zoom level  
**So that** the map is not cluttered at different scales

**Acceptance Criteria**:
- Each layer has minimum and maximum zoom levels
- Layers automatically show/hide based on current zoom
- Different detail levels appear at different zooms
- Substation equipment appears at high zoom levels
- Transmission lines appear at medium zoom levels
- Power stations appear at low zoom levels

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-2.1, US-2.2, US-2.3

---

### US-2.6: Map Tile Caching
**As a** system administrator  
**I want to** cache map tiles locally  
**So that** map loading is faster and reduces external API calls

**Acceptance Criteria**:
- System caches tiles from external providers
- Cached tiles are served when available
- Cache directory is configurable
- System checks cache before requesting external tiles
- Cache improves map loading performance
- Cache can be cleared if needed

**Priority**: Medium  
**Story Points**: 5  
**Dependencies**: None

---

### US-2.7: Layer Styling Configuration
**As a** system administrator  
**I want to** configure layer styling  
**So that** layers are displayed with appropriate colors and symbols

**Acceptance Criteria**:
- Admin can set fill color for polygon layers
- Admin can set stroke color and thickness for all layers
- Admin can set layer opacity
- Admin can configure label colors and sizes
- Styling changes are reflected immediately on map
- Layer settings are stored in database

**Priority**: Medium  
**Story Points**: 8  
**Dependencies**: US-2.1

---

### US-2.8: View Communication Infrastructure
**As a** communication engineer  
**I want to** view optical fiber and communication towers on the map  
**So that** I can plan and maintain communication networks

**Acceptance Criteria**:
- Map displays optical fiber routes as line features
- Map displays junction boxes as point features
- Map displays communication towers as point features
- User can view communication infrastructure details
- Communication layers can be toggled on/off

**Priority**: Medium  
**Story Points**: 5  
**Dependencies**: US-1.3

---

## Epic 3: Infrastructure Management

### US-3.1: View Substation Details
**As an** electrical engineer  
**I want to** view detailed information about substations  
**So that** I can understand their configuration and capacity

**Acceptance Criteria**:
- User can click substation to view details
- Details include name, voltage, capacity, status
- Details include operational information
- Details include contact information
- Details include equipment list
- Details panel is easy to read and navigate

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: US-2.1

---

### US-3.2: View Transmission Line Details
**As a** transmission line engineer  
**I want to** view detailed information about transmission lines  
**So that** I can analyze line capacity and condition

**Acceptance Criteria**:
- User can click transmission line to view details
- Details include line name, voltage, length
- Details include number of circuits
- Details include tower count and types
- Details include construction and operation dates
- Details include pollution and terrain information

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: US-2.2

---

### US-3.3: View Substation Equipment
**As an** electrical engineer  
**I want to** view equipment within substations  
**So that** I can understand substation configuration

**Acceptance Criteria**:
- User can view power transformers in substations
- User can view busbars
- User can view conductors between equipment
- User can view switchyard areas
- Equipment appears at appropriate zoom levels
- Equipment details are accessible via click

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-2.1, US-3.1

---

### US-3.4: View Transmission Line Components
**As a** transmission line engineer  
**I want to** view components of transmission lines  
**So that** I can analyze line structure

**Acceptance Criteria**:
- User can view circuits on transmission lines
- User can view overhead circuit segments
- User can view towers along transmission lines
- Components appear at appropriate zoom levels
- User can view component details

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-2.2, US-3.2

---

### US-3.5: Filter by Status
**As a** power grid planner  
**I want to** filter infrastructure by status  
**So that** I can focus on operational vs. under-construction assets

**Acceptance Criteria**:
- User can filter substations by status (operational, under construction, approved)
- User can filter transmission lines by status
- Filter controls are easy to use
- Map updates immediately when filters are applied
- Filter state persists during session

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-2.1, US-2.2

---

### US-3.6: Search Infrastructure
**As a** user  
**I want to** search for infrastructure by name or ID  
**So that** I can quickly find specific assets

**Acceptance Criteria**:
- User can enter search query
- Search searches across all searchable entities
- Search results show entity type and key information
- User can click result to navigate to location on map
- Search supports Persian/Farsi text
- Search results are paginated

**Priority**: High  
**Story Points**: 8  
**Dependencies**: US-1.3

---

### US-3.7: View Layer Settings
**As a** system administrator  
**I want to** view and manage layer settings  
**So that** I can configure how layers are displayed

**Acceptance Criteria**:
- Admin can view all layer settings
- Layer settings include visibility, styling, zoom levels
- Layer settings include searchability and labeling
- Layer settings are organized by groups
- Admin can update layer settings via API

**Priority**: Medium  
**Story Points**: 5  
**Dependencies**: US-1.3

---

## Epic 4: Data Access & Security

### US-4.1: Permission-Based Data Access
**As a** system user  
**I want to** only see data I have permission to access  
**So that** sensitive information is protected

**Acceptance Criteria**:
- System checks user permissions before returning data
- Users without permission see empty results or error
- Permission checks are enforced at API level
- Permission changes take effect immediately
- System logs permission denials for audit

**Priority**: Critical  
**Story Points**: 8  
**Dependencies**: US-1.7, US-1.8

---

### US-4.2: Encrypted Communication
**As a** security-conscious user  
**I want to** have my authentication data encrypted  
**So that** my credentials are protected during transmission

**Acceptance Criteria**:
- Registration requests are encrypted using RSA
- Login requests are encrypted using RSA
- Server has public/private key pair
- Client encrypts sensitive data before sending
- Server decrypts data upon receipt

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: US-1.1, US-1.3

---

### US-4.3: JWT Token Authentication
**As a** system user  
**I want to** authenticate using JWT tokens  
**So that** I can access protected resources securely

**Acceptance Criteria**:
- System issues JWT token upon login
- Token contains user ID, email, and roles
- Token has expiration time
- Client includes token in Authorization header
- System validates token on each request
- Expired tokens are rejected

**Priority**: Critical  
**Story Points**: 5  
**Dependencies**: US-1.3

---

## Epic 5: User Experience

### US-5.1: Persian/Farsi Language Support
**As a** Persian-speaking user  
**I want to** use the application in Persian/Farsi  
**So that** I can understand and use it effectively

**Acceptance Criteria**:
- Application UI displays in Persian/Farsi
- Map labels support Persian text
- Error messages are in Persian
- User input supports Persian characters
- Right-to-left text layout is supported

**Priority**: Critical  
**Story Points**: 8  
**Dependencies**: None

---

### US-5.2: Responsive Map Interface
**As a** map user  
**I want to** interact with the map smoothly  
**So that** I can navigate efficiently

**Acceptance Criteria**:
- Map responds quickly to zoom and pan operations
- Map tiles load efficiently
- Map maintains good performance with many layers
- Map supports mouse and touch interactions
- Map provides visual feedback during operations

**Priority**: High  
**Story Points**: 5  
**Dependencies**: US-2.1

---

### US-5.3: User-Friendly Error Messages
**As a** user  
**I want to** receive clear error messages  
**So that** I understand what went wrong and how to fix it

**Acceptance Criteria**:
- Error messages are in Persian/Farsi
- Error messages explain the problem clearly
- Error messages suggest solutions when possible
- Error messages are displayed prominently
- System logs detailed errors for debugging

**Priority**: Medium  
**Story Points**: 3  
**Dependencies**: US-5.1

---

## Story Summary

### By Epic
- **Epic 1: Authentication & Authorization**: 10 stories, 47 story points
- **Epic 2: Spatial Data Visualization**: 8 stories, 49 story points
- **Epic 3: Infrastructure Management**: 7 stories, 38 story points
- **Epic 4: Data Access & Security**: 3 stories, 18 story points
- **Epic 5: User Experience**: 3 stories, 16 story points

### By Priority
- **Critical**: 15 stories
- **High**: 10 stories
- **Medium**: 6 stories

### Total
- **31 user stories**
- **168 total story points**

---

## Definition of Done

A user story is considered "Done" when:
1. All acceptance criteria are met
2. Code is reviewed and approved
3. Unit tests are written and passing
4. Integration tests are written and passing
5. Documentation is updated
6. Feature is tested in staging environment
7. No critical bugs remain
8. Performance meets requirements
9. Security review is completed
10. Persian/Farsi translations are complete

