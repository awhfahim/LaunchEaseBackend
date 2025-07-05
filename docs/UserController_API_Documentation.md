# User Controller API Documentation

## Overview
The UserController provides RESTful API endpoints for managing users within a multi-tenant system. All endpoints require authentication and proper permissions.

**Base URL:** `/api/users`

**Authorization:** Bearer token required for all endpoints

**Tenant Context:** All endpoints operate within a tenant context (requires `[RequireTenant]` attribute)

---

## Endpoints

### 1. Get Users (List)
**Endpoint:** `GET /api/users`

**Permission Required:** `UsersView`

**Description:** Retrieves a paginated list of users for the current tenant.

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `page` | `int` | Query | Yes | Page number for pagination |
| `limit` | `int` | Query | Yes | Number of users per page |

#### Request Example
```http
GET /api/users?page=1&limit=10
Authorization: Bearer <token>
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "fullName": "John Doe",
      "phoneNumber": "+1234567890",
      "isEmailConfirmed": true,
      "isLocked": false,
      "lockoutEnd": null,
      "lastLoginAt": "2025-07-05T10:30:00Z",
      "createdAt": "2025-07-01T08:00:00Z"
    }
  ],
  "message": null
}
```

**Error (500 Internal Server Error):**
```json
{
  "success": false,
  "data": null,
  "message": "An error occurred while fetching users"
}
```

---

### 2. Get User by ID
**Endpoint:** `GET /api/users/{id}`

**Permission Required:** `UsersView`

**Description:** Retrieves a specific user by their ID within the current tenant.

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `id` | `Guid` | Route | Yes | User ID |

#### Request Example
```http
GET /api/users/12345678-1234-1234-1234-123456789012
Authorization: Bearer <token>
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "12345678-1234-1234-1234-123456789012",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "phoneNumber": "+1234567890",
    "isEmailConfirmed": true,
    "isLocked": false,
    "lockoutEnd": null,
    "lastLoginAt": "2025-07-05T10:30:00Z",
    "createdAt": "2025-07-01T08:00:00Z"
  },
  "message": null
}
```

---

### 3. Create User
**Endpoint:** `POST /api/users`

**Permission Required:** `UsersCreate`

**Description:** Creates a new user within the current tenant.

#### Request Body
```json
{
  "email": "newuser@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "password": "SecurePassword123!",
  "phoneNumber": "+1987654321",
  "roleId": "role-guid-here"
}
```

#### Request Example
```http
POST /api/users
Authorization: Bearer <token>
Content-Type: application/json

{
  "email": "newuser@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "password": "SecurePassword123!",
  "phoneNumber": "+1987654321",
  "roleId": "87654321-4321-4321-4321-210987654321"
}
```

#### Response
**Success (201 Created):**
```json
{
  "success": true,
  "data": {
    "id": "new-user-guid",
    "email": "newuser@example.com",
    "firstName": "Jane",
    "lastName": "Smith",
    "fullName": "Jane Smith",
    "phoneNumber": "+1987654321",
    "isEmailConfirmed": false,
    "isLocked": false,
    "lockoutEnd": null,
    "lastLoginAt": null,
    "createdAt": "2025-07-05T12:00:00Z"
  },
  "message": null
}
```

---

### 4. Update User
**Endpoint:** `PUT /api/users/{id}`

**Permission Required:** `UsersEdit`

**Description:** Updates an existing user's information.

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `id` | `Guid` | Route | Yes | User ID to update |

#### Request Body
```json
{
  "firstName": "UpdatedFirstName",
  "lastName": "UpdatedLastName",
  "phoneNumber": "+1555123456"
}
```

#### Request Example
```http
PUT /api/users/12345678-1234-1234-1234-123456789012
Authorization: Bearer <token>
Content-Type: application/json

{
  "firstName": "UpdatedJohn",
  "lastName": "UpdatedDoe",
  "phoneNumber": "+1555123456"
}
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": {
    "id": "12345678-1234-1234-1234-123456789012",
    "email": "user@example.com",
    "firstName": "UpdatedJohn",
    "lastName": "UpdatedDoe",
    "fullName": "UpdatedJohn UpdatedDoe",
    "phoneNumber": "+1555123456",
    "isEmailConfirmed": true,
    "isLocked": false,
    "lockoutEnd": null,
    "lastLoginAt": "2025-07-05T10:30:00Z",
    "createdAt": "2025-07-01T08:00:00Z"
  },
  "message": "User updated successfully"
}
```

---

### 5. Delete User
**Endpoint:** `DELETE /api/users/{id}`

**Permission Required:** `UsersDelete`

**Description:** Deletes a user from the current tenant.

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `id` | `Guid` | Route | Yes | User ID to delete |

#### Request Example
```http
DELETE /api/users/12345678-1234-1234-1234-123456789012
Authorization: Bearer <token>
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "User deleted successfully"
}
```

---

### 6. Assign Roles to User
**Endpoint:** `POST /api/users/{id}/assign-roles`

**Permission Required:** `UsersEdit`

**Description:** Assigns one or more roles to a user.

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `id` | `Guid` | Route | Yes | User ID |

#### Request Body
```json
[
  "role-guid-1",
  "role-guid-2",
  "role-guid-3"
]
```

#### Request Example
```http
POST /api/users/12345678-1234-1234-1234-123456789012/assign-roles
Authorization: Bearer <token>
Content-Type: application/json

[
  "87654321-4321-4321-4321-210987654321",
  "11111111-1111-1111-1111-111111111111"
]
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "Roles assigned successfully"
}
```

**Error (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Role IDs are required"
}
```

**Error (500 Internal Server Error):**
```json
{
  "success": false,
  "data": null,
  "message": "An error occurred while assigning roles"
}
```

---

### 7. Unassign Roles from User
**Endpoint:** `POST /api/users/{id}/unassign-roles`

**Permission Required:** `UsersEdit`

**Description:** Removes one or more roles from a user.

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `id` | `Guid` | Route | Yes | User ID |

#### Request Body
```json
[
  "role-guid-1",
  "role-guid-2"
]
```

#### Request Example
```http
POST /api/users/12345678-1234-1234-1234-123456789012/unassign-roles
Authorization: Bearer <token>
Content-Type: application/json

[
  "87654321-4321-4321-4321-210987654321"
]
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "Roles unassigned successfully"
}
```

**Error (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Role IDs are required"
}
```

---

### 8. Invite User to Tenant
**Endpoint:** `POST /api/users/invite`

**Permission Required:** `UsersCreate`

**Description:** Invites an existing user to join the current tenant.

#### Request Body
```json
{
  "email": "existinguser@example.com"
}
```

#### Request Example
```http
POST /api/users/invite
Authorization: Bearer <token>
Content-Type: application/json

{
  "email": "existinguser@example.com"
}
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": {},
  "message": "User invited to tenant successfully"
}
```

**Error (500 Internal Server Error):**
```json
{
  "success": false,
  "data": null,
  "message": "An error occurred while inviting user"
}
```

---

### 9. Check Email Exists
**Endpoint:** `GET /api/users/email-exists`

**Permission Required:** `UsersCreate`

**Description:** Checks if an email address already exists in the system.

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `email` | `string` | Query | Yes | Email address to check |

#### Request Example
```http
GET /api/users/email-exists?email=test@example.com
Authorization: Bearer <token>
```

#### Response
**Success (200 OK) - Email exists:**
```json
{
  "success": true,
  "data": true,
  "message": "Email exists"
}
```

**Success (200 OK) - Email doesn't exist:**
```json
{
  "success": true,
  "data": false,
  "message": "Email does not exist"
}
```

**Error (400 Bad Request):**
```json
{
  "success": false,
  "data": null,
  "message": "Email is required"
}
```

---

### 10. Get User Tenants
**Endpoint:** `GET /api/users/user-tenants`

**Permission Required:** `GlobalUsersView`

**Description:** Retrieves all tenants associated with a specific user (global operation).

#### Parameters
| Parameter | Type | Location | Required | Description |
|-----------|------|----------|----------|-------------|
| `userId` | `Guid` | Query | Yes | User ID to get tenants for |

#### Request Example
```http
GET /api/users/user-tenants?userId=12345678-1234-1234-1234-123456789012
Authorization: Bearer <token>
```

#### Response
**Success (200 OK):**
```json
{
  "success": true,
  "data": {
    // User with tenants data structure
  },
  "message": "User tenants fetched successfully"
}
```

---

## Common Response Structure

All API responses follow a consistent structure:

```json
{
  "success": boolean,
  "data": object | array | null,
  "message": string | null
}
```

### HTTP Status Codes Used
- **200 OK**: Successful operation
- **201 Created**: Resource successfully created
- **400 Bad Request**: Invalid request parameters
- **401 Unauthorized**: Authentication required
- **403 Forbidden**: Insufficient permissions
- **500 Internal Server Error**: Server error occurred

## UserResponse Object Structure

```json
{
  "id": "guid",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "fullName": "string",
  "phoneNumber": "string",
  "isEmailConfirmed": boolean,
  "isLocked": boolean,
  "lockoutEnd": "datetime | null",
  "lastLoginAt": "datetime | null",
  "createdAt": "datetime"
}
```

## Error Handling

The API uses consistent error responses with appropriate HTTP status codes. Common error scenarios include:

- **Authentication errors**: Missing or invalid bearer tokens
- **Authorization errors**: Insufficient permissions for the requested operation
- **Validation errors**: Invalid input parameters or request body
- **Business logic errors**: Violations of business rules (handled by service layer)
- **Server errors**: Unexpected system errors

## Notes

1. All datetime values are in UTC format (ISO 8601)
2. GUIDs must be in standard format: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`
3. Email confirmation is set to `false` by default for new users (email verification required)
4. Password hashing is handled automatically by the authentication service
5. Security stamps are generated automatically for new users
6. The API operates within a multi-tenant context - users can only access data within their tenant scope (except for global operations)
