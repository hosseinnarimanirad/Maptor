# API Endpoint Documentation

## Overview

This document describes all REST API endpoints available in the MakanNegarSaba application. The API follows RESTful conventions and uses JWT Bearer token authentication for most endpoints.

**Base URL**: `http://{host}:{port}`

**Authentication**: Most endpoints require JWT Bearer token authentication. Include the token in the `Authorization` header:
```
Authorization: Bearer {token}
```

**Content-Type**: `application/json`

## Authentication Endpoints

### User Registration

#### POST `/api/User/SignUp`

Register a new user account.

**Authentication**: Not required

**Request Body** (Encrypted):
```json
{
  "emailAddress": "user@example.com",
  "plainPassword": "SecurePassword123!"
}
```

**Response**: `200 OK`
```json
{
  "user": {
    "id": 1,
    "emailAddress": "user@example.com",
    "isEmailVerified": false,
    "isActive": false
  },
  "message": "Registration successful. Please verify your email."
}
```

**Error Responses**:
- `400 Bad Request`: Invalid input data
- `409 Conflict`: Email already exists
- `500 Internal Server Error`: Server error

---

### User Login

#### POST `/api/User/Login`

Authenticate user and receive JWT token.

**Authentication**: Not required

**Request Body** (Encrypted):
```json
{
  "emailAddress": "user@example.com",
  "plainPassword": "SecurePassword123!"
}
```

**Response**: `200 OK`
```json
{
  "user": {
    "id": 1,
    "emailAddress": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "isEmailVerified": true,
    "isActive": true
  },
  "accessToken": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-12-31T23:59:59Z"
  }
}
```

**Error Responses**:
- `401 Unauthorized`: Invalid credentials
- `403 Forbidden`: Account locked or inactive
- `500 Internal Server Error`: Server error

---

## User Management Endpoints

### List Users

#### GET `/api/User`

Get list of all users.

**Authentication**: Required (Admin role)

**Query Parameters**:
- `activeOnly` (boolean, optional): Filter for active users only. Default: `false`

**Response**: `200 OK`
```json
[
  {
    "id": 1,
    "emailAddress": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "isEmailVerified": true,
    "isActive": true,
    "registrationTime": "2024-01-15T10:30:00Z"
  }
]
```

---

### Update User Status

#### PUT `/api/User/{userId}/status`

Update user active/inactive status.

**Authentication**: Required (Admin role)

**Path Parameters**:
- `userId` (integer): User ID

**Request Body**:
```json
{
  "isActive": true
}
```

**Response**: `200 OK`
```json
{
  "message": "User status updated successfully"
}
```

**Error Responses**:
- `404 Not Found`: User not found
- `500 Internal Server Error`: Server error

---

### Sync User Roles

#### POST `/api/User/{userId}/roles/sync`

Assign or remove roles for a user. This endpoint replaces all existing role assignments.

**Authentication**: Required (Admin role)

**Path Parameters**:
- `userId` (integer): User ID

**Request Body**:
```json
{
  "roleIds": [1, 2, 3]
}
```

**Response**: `200 OK`
```json
{
  "message": "User roles synced successfully"
}
```

**Error Responses**:
- `404 Not Found`: User not found
- `500 Internal Server Error`: Server error

---

### Verify User Email

#### PUT `/api/User/{userId}/verify-email`

Manually verify a user's email address.

**Authentication**: Required (Admin role)

**Path Parameters**:
- `userId` (integer): User ID

**Response**: `200 OK`
```json
{
  "message": "User email verified successfully"
}
```

**Error Responses**:
- `404 Not Found`: User not found
- `500 Internal Server Error`: Server error

---

### Get User Login Statistics

#### GET `/api/User/{userId}/login-statistics`

Get user login statistics including first login, last login, and login count.

**Authentication**: Required

**Path Parameters**:
- `userId` (integer): User ID

**Response**: `200 OK`
```json
{
  "userId": 1,
  "firstLoginAt": "2024-01-15T10:30:00Z",
  "lastLoginAt": "2024-12-01T14:20:00Z",
  "loginCount": 45
}
```

**Error Responses**:
- `404 Not Found`: User not found
- `500 Internal Server Error`: Server error

---

## Role Management Endpoints

### List Roles

#### GET `/api/Role`

Get all roles in the system.

**Authentication**: Required

**Query Parameters**:
- `activeOnly` (boolean, optional): Filter to only active roles. Default: `true`

**Response**: `200 OK`
```json
[
  {
    "id": 1,
    "name": "SuperAdmin",
    "displayName": "Super Administrator",
    "description": "Full system access",
    "isSystemRole": true,
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

---

### Get Role by ID

#### GET `/api/Role/{id}`

Get a specific role by ID.

**Authentication**: Required

**Path Parameters**:
- `id` (integer): Role ID

**Response**: `200 OK`
```json
{
  "id": 1,
  "name": "SuperAdmin",
  "displayName": "Super Administrator",
  "description": "Full system access",
  "isSystemRole": true,
  "isActive": true
}
```

**Error Responses**:
- `404 Not Found`: Role not found

---

### Create Role

#### POST `/api/Role`

Create a new role.

**Authentication**: Required (Admin role)

**Request Body**:
```json
{
  "name": "CustomRole",
  "displayName": "Custom Role",
  "description": "Custom role description",
  "permissionIds": [1, 2, 3, 5]
}
```

**Response**: `201 Created`
```json
{
  "success": true,
  "roleId": 6,
  "message": "Role created successfully"
}
```

**Error Responses**:
- `400 Bad Request`: Invalid input or role name already exists
- `500 Internal Server Error`: Server error

---

### Update Role

#### PUT `/api/Role/{id}`

Update an existing role.

**Authentication**: Required (Admin role)

**Path Parameters**:
- `id` (integer): Role ID

**Request Body**:
```json
{
  "id": 6,
  "displayName": "Updated Custom Role",
  "description": "Updated description",
  "isActive": true,
  "permissionIds": [1, 2, 3, 5, 7]
}
```

**Response**: `200 OK`

**Error Responses**:
- `400 Bad Request`: Invalid input or ID mismatch
- `404 Not Found`: Role not found or cannot be modified (system role)
- `500 Internal Server Error`: Server error

---

### Delete Role

#### DELETE `/api/Role/{id}`

Delete a role.

**Authentication**: Required (Admin role)

**Path Parameters**:
- `id` (integer): Role ID

**Response**: `204 No Content`

**Error Responses**:
- `400 Bad Request`: Role is system role or assigned to users
- `404 Not Found`: Role not found
- `500 Internal Server Error`: Server error

---

## Permission Endpoints

### List Permissions

#### GET `/api/Permission`

Get all available permissions.

**Authentication**: Required

**Query Parameters**:
- `category` (string, optional): Filter by permission category

**Response**: `200 OK`
```json
[
  {
    "id": 1,
    "name": "UsersView",
    "displayName": "View Users",
    "category": "UserManagement",
    "description": "Permission to view user list"
  }
]
```

---

### Get Permission Categories

#### GET `/api/Permission/categories`

Get all permission categories.

**Authentication**: Required

**Response**: `200 OK`
```json
[
  "UserManagement",
  "RoleManagement",
  "Substations",
  "TransmissionLines",
  "PowerStations",
  "Communications",
  "LayerSettings",
  "Reports"
]
```

---

### Get Permissions Grouped by Category

#### GET `/api/Permission/grouped`

Get permissions organized by category.

**Authentication**: Required

**Response**: `200 OK`
```json
{
  "UserManagement": [
    {
      "id": 1,
      "name": "UsersView",
      "displayName": "View Users"
    }
  ],
  "Substations": [
    {
      "id": 10,
      "name": "SubstationsViewAll",
      "displayName": "View All Substations"
    }
  ]
}
```

---

## Substation Endpoints

All substation endpoints require authentication and return `FeatureSetDto` containing GeoJSON features.

### List Substations

#### GET `/Substation/ListSubstat`

Get all transmission and distribution substations.

**Authentication**: Required

**Query Parameters**: None (currently)

**Response**: `200 OK`
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "Point",
        "coordinates": [51.3890, 35.6892]
      },
      "properties": {
        "id": 1,
        "gis_id": "SUB001",
        "subf_name": "ایستگاه تهران",
        "sube_name": "Tehran Substation",
        "max_vol": "400",
        "sub_kind": "Transmission",
        "sul_curcap": 500.0
      }
    }
  ]
}
```

---

### List Under Construction Substations

#### GET `/Substation/ListUnConSubstation`

Get substations currently under construction.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Approved Substations

#### GET `/Substation/ListAppSubstation`

Get approved substations that are not yet constructed.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Switchyard Areas

#### GET `/Substation/ListSwitchyardArea`

Get switchyard areas within substations.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Power Transformers

#### GET `/Substation/ListPowTran`

Get power transformers located in substations.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Busbars

#### GET `/Substation/ListBusbar`

Get busbar equipment within substations.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Conductors Between Equipment

#### GET `/Substation/ListCondBeEq`

Get conductors connecting equipment within substations.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

## Transmission Line Endpoints

All transmission line endpoints require authentication and return `FeatureSetDto`.

### List Transmission Line Segments

#### GET `/TransmissionLine/ListTrLineSeg`

Get all transmission line segments.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

**Example Response**:
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "LineString",
        "coordinates": [[51.3890, 35.6892], [51.3900, 35.6900]]
      },
      "properties": {
        "id": 1,
        "gis_id": "TL001",
        "line_name": "خط انتقال تهران-اصفهان",
        "path_name": "مسیر 1",
        "denomi_vol": 400,
        "cir_num": 2,
        "path_length": 250.5
      }
    }
  ]
}
```

---

### List Under Construction Transmission Lines

#### GET `/TransmissionLine/ListUnConTrLine`

Get transmission lines currently under construction.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Circuits

#### GET `/TransmissionLine/ListCircuit`

Get electrical circuits on transmission lines.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Overhead Circuit Segments

#### GET `/TransmissionLine/ListOvciSeg`

Get overhead circuit segments.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Towers

#### GET `/TransmissionLine/ListTower`

Get transmission line towers/poles.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

## Power Station Endpoints

### List Power Plants

#### GET `/PowerStation/ListPowerPlant`

Get all power generation plants.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Distributed Generation Power Plants

#### GET `/PowerStation/ListDgPowerPlant`

Get distributed generation power plants.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

## Communication Endpoints

### List Optical Fiber

#### GET `/Communication/ListOpticFi`

Get optical fiber communication infrastructure.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Join Boxes

#### GET `/Communication/ListJoinBox`

Get fiber optic junction boxes.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

### List Communication Towers

#### GET `/Communication/ListComTowr`

Get communication towers.

**Authentication**: Required

**Response**: `200 OK` (FeatureSetDto)

---

## Search Endpoints

### General Search

#### GET `/api/Search/GeneralSearch`

Perform a general search across all searchable entities.

**Authentication**: Required

**Query Parameters**:
- `searchString` (string, required): Search query string
- `pageNumber` (integer, optional): Page number for pagination. Default: `1`
- `pageSize` (integer, optional): Number of results per page. Default: `50`

**Response**: `200 OK`
```json
{
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 50,
  "totalPages": 3,
  "results": [
    {
      "entityType": "Substation",
      "id": 1,
      "displayName": "ایستگاه تهران",
      "gisId": "SUB001",
      "properties": {
        "max_vol": "400",
        "sub_kind": "Transmission"
      }
    }
  ]
}
```

---

## Layer Settings Endpoints

### List Layer Settings

#### GET `/LayerSetting/List`

Get all layer configuration settings for map display.

**Authentication**: Not required (public endpoint)

**Response**: `200 OK`
```json
{
  "layerSettings": [
    {
      "id": 100,
      "title": "ایستگاه انتقال و فوق توزیع",
      "tableName": "Substat",
      "isActive": true,
      "minLayerZoomLevel": 10,
      "maxLayerZoomLevel": null,
      "hexFill": null,
      "hexStroke": "#FFEEAE09",
      "strokeThickness": 4,
      "opacity": 1,
      "isLayerOn": true,
      "serviceUrl": "/Substation/ListSubstat",
      "isGroupLayer": false,
      "groupLayerId": 11,
      "isSearchable": true,
      "isLabeled": true,
      "labelColumn": "subf_name",
      "labelSize": 13,
      "minLabelZoomLevel": 11,
      "mapOrder": 4,
      "legendOrder": 1
    }
  ]
}
```

---

## Tile Service Endpoints

### Get Map Tile

#### GET `/api/Tile`

Proxy and cache map tiles from external tile services.

**Authentication**: Not required (public endpoint)

**Query Parameters**:
- `url` (string, required): Base URL of tile service
- `provider` (string, required): Tile provider name (e.g., "OpenStreetMap", "Google")
- `mapType` (string, required): Map type (e.g., "satellite", "roadmap")
- `z` (integer, required): Zoom level
- `x` (integer, required): Tile X coordinate
- `y` (integer, required): Tile Y coordinate

**Response**: `200 OK`
- Content-Type: `image/png` or `image/jpeg`
- Tile image binary data

**Error Responses**:
- `400 Bad Request`: Missing or invalid parameters
- `404 Not Found`: Tile not found or could not be downloaded
- `500 Internal Server Error`: Server error

**Example**:
```
GET /api/Tile?url=https://tile.openstreetmap.org&provider=OpenStreetMap&mapType=standard&z=10&x=512&y=256
```

---

## Common Response Types

### FeatureSetDto

GeoJSON FeatureCollection format for spatial data:

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "Point|LineString|Polygon",
        "coordinates": [...]
      },
      "properties": {
        // Entity-specific properties
      }
    }
  ]
}
```

### Error Response

Standard error response format:

```json
{
  "error": "ErrorType",
  "message": "Human-readable error message",
  "details": {
    // Additional error details
  }
}
```

---

## Authentication & Authorization

### JWT Token Format

JWT tokens are issued upon successful login and must be included in the `Authorization` header:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Token Claims

JWT tokens contain the following claims:
- `sub`: User ID
- `email`: User email address
- `roles`: Array of role names
- `exp`: Token expiration timestamp
- `iat`: Token issuance timestamp

### Permission-Based Access

Most endpoints check user permissions based on their assigned roles. If a user lacks the required permission, the endpoint returns `403 Forbidden`.

---

## Rate Limiting

Currently, no rate limiting is implemented. Future versions may include rate limiting to prevent abuse.

---

## Encryption

User registration and login endpoints use RSA encryption for request/response data. The client must encrypt sensitive data using the server's public key before sending requests.

---

## Versioning

API versioning is not currently implemented. All endpoints use the base URL without version prefixes. Future versions may implement URL-based versioning (e.g., `/api/v1/...`).

---

## Swagger/OpenAPI

Interactive API documentation is available via Swagger UI when running the application:
- Development: `http://localhost:{port}/swagger`
- Production: Swagger may be disabled for security

The OpenAPI specification can be exported from the Swagger UI for use with API testing tools.

