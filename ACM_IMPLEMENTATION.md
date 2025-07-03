# LaunchEase Backend - Multi-Tenant Access Control Management Implementation

This document outlines the complete implementation of a multi-tenant access control system with **global users** and **multi-tenant session handling**, similar to Azure Portal.

## Architecture Overview

The system implements a **multi-tenant SaaS architecture** with the following key components:

- **Global Users**: Users are now global entities that can belong to multiple tenants
- **Multi-Tenant Session Handling**: Users can switch between tenants after authentication
- **Two-Step Authentication**: Initial login + tenant selection/switching
- **Data Access Layer**: Dapper-based repositories for high-performance database operations
- **Identity System**: Custom ASP.NET Core Identity stores backed by Dapper
- **Authentication**: JWT-based authentication with tenant isolation
- **Authorization**: Policy-based authorization with permission claims
- **Multi-tenancy**: Complete tenant isolation with middleware enforcement

## Key Features

### 1. Global User System

#### User Entity Changes:
- **Removed TenantId**: Users are now global entities
- **Global Lockout**: `IsGloballyLocked`, `GlobalLockoutEnd`, `GlobalAccessFailedCount`
- **Tenant-Independent**: Users can belong to multiple tenants

#### User-Tenant Relationship:
- **UserTenant Entity**: Many-to-many relationship between users and tenants
- **Tenant Membership**: `IsActive`, `JoinedAt`, `LeftAt`, `InvitedBy`
- **Flexible Membership**: Users can be added/removed from tenants dynamically

### 2. Multi-Tenant Authentication Flow

#### Step 1: Initial Authentication
```http
POST /api/auth/initial-login
{
  "email": "user@example.com",
  "password": "password"
}
```

**Response**: List of accessible tenants for the user
```json
{
  "success": true,
  "data": {
    "userId": "guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "accessibleTenants": [
      {
        "id": "tenant-guid-1",
        "name": "Tenant A",
        "slug": "tenant-a",
        "logoUrl": "https://...",
        "userRoles": ["Admin", "Editor"]
      },
      {
        "id": "tenant-guid-2", 
        "name": "Tenant B",
        "slug": "tenant-b",
        "userRoles": ["Viewer"]
      }
    ]
  }
}
```

#### Step 2: Tenant Selection
```http
POST /api/auth/tenant-login
{
  "userId": "user-guid",
  "tenantId": "tenant-guid"
}
```

**Response**: Tenant-specific JWT token
```json
{
  "success": true,
  "data": {
    "accessToken": "jwt-token-with-tenant-context",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": "user-guid",
      "email": "user@example.com",
      "tenantId": "tenant-guid",
      "roles": ["Admin", "Editor"],
      "permissions": ["users.view", "users.create", ...]
    },
    "tenant": {
      "id": "tenant-guid",
      "name": "Tenant A",
      "slug": "tenant-a"
    }
  }
}
```

#### Step 3: Tenant Switching (Optional)
```http
POST /api/auth/switch-tenant
Authorization: Bearer current-jwt-token
{
  "tenantId": "new-tenant-guid"
}
```

**Response**: New JWT token for the selected tenant

### 3. Database Schema Changes

#### Updated Tables:

**users** (Global Users)
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,  -- Global unique email
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    security_stamp VARCHAR(255) NOT NULL,
    is_email_confirmed BOOLEAN DEFAULT FALSE,
    is_globally_locked BOOLEAN DEFAULT FALSE,  -- Global lockout
    global_lockout_end TIMESTAMP,
    global_access_failed_count INTEGER DEFAULT 0,
    last_login_at TIMESTAMP,
    phone_number VARCHAR(20),
    is_phone_number_confirmed BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);
```

**user_tenants** (Many-to-Many Relationship)
```sql
CREATE TABLE user_tenants (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    is_active BOOLEAN DEFAULT TRUE,
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    left_at TIMESTAMP,
    invited_by VARCHAR(255),  -- Email of inviter
    UNIQUE(user_id, tenant_id)
);
```

**user_roles** (Tenant-Scoped Roles)
```sql
CREATE TABLE user_roles (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    role_id UUID NOT NULL REFERENCES roles(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),  -- Explicit tenant context
    UNIQUE(user_id, role_id, tenant_id)
);
```

**user_claims** (Tenant-Scoped Claims)
```sql
CREATE TABLE user_claims (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),  -- Tenant-specific claims
    claim_type VARCHAR(255) NOT NULL,
    claim_value VARCHAR(255) NOT NULL
);
```

### 4. Updated Repository Interfaces

#### IUserRepository
```csharp
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default); // Global lookup
    Task<IEnumerable<User>> GetByTenantIdAsync(Guid tenantId, int page, int limit, CancellationToken cancellationToken = default);
    // ... other methods
}
```

#### IUserTenantRepository (New)
```csharp
public interface IUserTenantRepository
{
    Task<IEnumerable<UserTenant>> GetUserTenantsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tenant>> GetUserAccessibleTenantsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberOfTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid> AddUserToTenantAsync(UserTenant userTenant, CancellationToken cancellationToken = default);
    Task RemoveUserFromTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
```

#### Tenant-Specific Query Support
All role and claim repositories now support tenant-specific queries:
```csharp
Task<IEnumerable<string>> GetRoleNamesForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
Task<IEnumerable<Claim>> GetClaimsForUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
```

### 5. Authentication Service Updates

#### IAuthenticationService
```csharp
public interface IAuthenticationService
{
    // Step 1: Initial login without tenant context
    Task<InitialAuthenticationResult> AuthenticateUserAsync(string email, string password, CancellationToken cancellationToken = default);
    
    // Step 2: Select tenant and get tenant-specific token
    Task<TenantAuthenticationResult> AuthenticateWithTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    
    // Tenant switching for already authenticated users
    Task<TenantAuthenticationResult> SwitchTenantAsync(Guid userId, Guid newTenantId, CancellationToken cancellationToken = default);
    
    // Tenant-specific JWT generation
    Task<string> GenerateJwtTokenAsync(Guid userId, Guid tenantId, IEnumerable<Claim> claims, CancellationToken cancellationToken = default);
    
    // Get user claims for specific tenant
    Task<IEnumerable<Claim>> GetUserClaimsForTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    
    // Legacy methods for backward compatibility
    Task<AuthenticationResult> AuthenticateAsync(string email, string password, Guid tenantId, CancellationToken cancellationToken = default);
}
```

## API Endpoints

### New Multi-Tenant Authentication Endpoints

```http
POST /api/auth/initial-login     # Step 1: User authentication
POST /api/auth/tenant-login      # Step 2: Tenant selection  
POST /api/auth/switch-tenant     # Tenant switching
```

### Legacy Endpoints (Backward Compatibility)

```http
POST /api/auth/login            # Traditional single-tenant login
POST /api/auth/refresh          # Token refresh
POST /api/auth/logout           # Logout
GET  /api/auth/me              # Current user info
```

### Tenant Management (Unchanged)

```http
POST /api/tenants/register      # Self-service tenant registration
GET  /api/tenants/{slug}        # Get tenant by slug
GET  /api/tenants              # List tenants
```

## User Experience Flow

### 1. New User Registration
1. User registers globally (no tenant required)
2. User receives invitation to join specific tenant(s)
3. User accepts invitation and gains access to tenant

### 2. Existing User Login
1. **Step 1**: User enters email/password → Receives list of accessible tenants
2. **Step 2**: User selects tenant → Receives tenant-specific JWT token
3. **Step 3**: User can switch tenants anytime without re-authentication

### 3. Multi-Tenant Session Handling
- **Single Authentication**: Login once, access multiple tenants
- **Tenant Context**: JWT token contains tenant-specific roles and permissions
- **Seamless Switching**: Switch tenants with a single API call
- **Session Persistence**: Each tenant context maintained separately

## Benefits of This Implementation

### 1. **Improved User Experience**
- **No Tenant ID Required**: Users don't need to know tenant identifiers
- **Single Login**: Authenticate once, access multiple tenants
- **Azure Portal-like Experience**: Familiar multi-tenant switching
- **Reduced Friction**: Eliminates the chicken-and-egg problem

### 2. **Enhanced Security**
- **Global User Management**: Centralized user authentication
- **Tenant Isolation**: Complete separation of tenant data and permissions
- **Granular Access Control**: Tenant-specific roles and permissions
- **Secure Token Switching**: JWT tokens are tenant-scoped

### 3. **Scalability & Flexibility**
- **Multi-Tenant Membership**: Users can belong to multiple organizations
- **Dynamic Tenant Management**: Add/remove users from tenants easily
- **Cross-Tenant Collaboration**: Support for shared users across tenants
- **Future-Proof Architecture**: Ready for advanced multi-tenancy features

### 4. **Backward Compatibility**
- **Legacy Support**: Old login endpoints still work
- **Gradual Migration**: Can migrate existing users incrementally
- **API Consistency**: Existing client applications continue to work

## Migration Strategy

### 1. Database Migration
```sql
-- Add new columns to users table
ALTER TABLE users ADD COLUMN is_globally_locked BOOLEAN DEFAULT FALSE;
ALTER TABLE users ADD COLUMN global_lockout_end TIMESTAMP;
ALTER TABLE users ADD COLUMN global_access_failed_count INTEGER DEFAULT 0;

-- Create user_tenants table
CREATE TABLE user_tenants (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    is_active BOOLEAN DEFAULT TRUE,
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    left_at TIMESTAMP,
    invited_by VARCHAR(255),
    UNIQUE(user_id, tenant_id)
);

-- Migrate existing users to user_tenants
INSERT INTO user_tenants (id, user_id, tenant_id, is_active, joined_at)
SELECT gen_random_uuid(), id, tenant_id, true, created_at
FROM users;

-- Add tenant_id to user_roles and user_claims
ALTER TABLE user_roles ADD COLUMN tenant_id UUID REFERENCES tenants(id);
ALTER TABLE user_claims ADD COLUMN tenant_id UUID REFERENCES tenants(id);

-- Update tenant_id based on user's original tenant
UPDATE user_roles SET tenant_id = (SELECT tenant_id FROM users WHERE users.id = user_roles.user_id);
UPDATE user_claims SET tenant_id = (SELECT tenant_id FROM users WHERE users.id = user_claims.user_id);

-- Remove tenant_id from users table (make email globally unique)
ALTER TABLE users DROP COLUMN tenant_id;
```

### 2. Application Migration
1. **Phase 1**: Deploy new authentication endpoints alongside legacy ones
2. **Phase 2**: Update client applications to use new flow
3. **Phase 3**: Migrate existing users to global user model
4. **Phase 4**: Remove legacy endpoints (optional)

## Testing Strategy

### 1. Unit Tests
- Repository tests with new multi-tenant queries
- Service tests for authentication flows
- Authorization handler tests for tenant context

### 2. Integration Tests
- End-to-end multi-tenant authentication flow
- Tenant switching scenarios
- Cross-tenant access prevention
- Legacy compatibility tests

### 3. User Acceptance Testing
- Multi-tenant user journey testing
- Performance testing with multiple tenants
- Security testing for tenant isolation

## Deployment Considerations

### Prerequisites:
- .NET 9.0 Runtime
- PostgreSQL 12+ (with new schema)
- Updated connection strings
- JWT configuration

### Configuration Updates:
```json
{
  "Jwt": {
    "Key": "your-jwt-signing-key",
    "Issuer": "LaunchEase",
    "Audience": "LaunchEase.Client",
    "AccessTokenExpiryMinutes": 60
  },
  "AcmConnectionString": {
    "AcmDb": "Host=localhost;Database=launchease;Username=postgres;Password=password"
  }
}
```

## Monitoring & Analytics

### Key Metrics to Track:
1. **Authentication Success Rate** by tenant
2. **Tenant Switch Frequency** per user
3. **Cross-Tenant User Activity**
4. **Token Refresh Patterns**
5. **Failed Authentication Attempts**

### Logging Enhancements:
- Multi-tenant context in all log entries
- Tenant switching audit trail
- Cross-tenant access attempts
- Performance metrics per tenant

## Future Enhancements

### 1. Advanced Multi-Tenancy
- **Tenant Hierarchies**: Parent-child tenant relationships
- **Cross-Tenant Permissions**: Controlled access across tenants
- **Tenant Templates**: Standardized tenant setups
- **Tenant Analytics**: Usage patterns and insights

### 2. Enhanced User Experience
- **Tenant Switching UI**: Visual tenant selector
- **Recent Tenants**: Quick access to frequently used tenants
- **Tenant Bookmarks**: Favorite tenant shortcuts
- **Unified Dashboard**: Cross-tenant overview

### 3. Enterprise Features
- **SSO Integration**: SAML/OAuth with tenant mapping
- **Directory Sync**: Automated user provisioning
- **Compliance Features**: Audit trails and reporting
- **Advanced Security**: Multi-factor authentication per tenant

This implementation provides a robust foundation for multi-tenant SaaS applications with excellent user experience and enterprise-grade security.

## Project Structure

```
src/
├── Modules/AccessControlManagement/
│   ├── Acm.Domain/
│   │   └── Entities/           # Domain entities (User, Role, Tenant, etc.)
│   ├── Acm.Application/
│   │   ├── Repositories/       # Repository interfaces
│   │   └── Services/          # Service interfaces
│   ├── Acm.Infrastructure/
│   │   ├── Persistence/       # Dapper repository implementations
│   │   ├── Identity/         # ASP.NET Core Identity stores
│   │   ├── Authorization/    # Authorization handlers and attributes
│   │   ├── Middleware/       # Tenant isolation middleware
│   │   └── Services/         # Service implementations
│   └── Acm.Api/
│       ├── Controllers/      # API controllers
│       └── DTOs/            # Request/Response models
```

## Features Implemented

### 1. Data Access Layer (Dapper)

#### Repository Implementations:
- **UserRepository**: Complete CRUD operations with tenant isolation
- **RoleRepository**: Role management with tenant scoping
- **UserRoleRepository**: User-role assignments
- **UserClaimRepository**: User-specific claims management
- **RoleClaimRepository**: Role-based claims (permissions)
- **TenantRepository**: Tenant management operations

#### Key Features:
- Parameterized queries to prevent SQL injection
- Async/await pattern throughout
- Proper connection management
- Tenant isolation at database level

### 2. ASP.NET Core Identity Integration

#### Custom Identity Stores:
- **UserStore**: Implements IUserStore, IUserPasswordStore, IUserClaimStore, IUserRoleStore, IUserLockoutStore
- **RoleStore**: Implements IRoleStore, IRoleClaimStore

#### Features:
- Complete integration with ASP.NET Core Identity
- Password hashing using Argon2
- Account lockout support
- Claims-based authorization
- Security stamp management

### 3. Authentication System

#### JWT Authentication:
- Secure JWT token generation
- Claims-based user identity
- Tenant isolation in tokens
- Automatic token refresh
- Cookie and header token support

#### Password Security:
- Argon2id password hashing
- Configurable hash parameters
- Salt-based security

### 4. Authorization System

#### Permission-Based Authorization:
- Custom authorization attributes
- Policy-based permissions
- Role-based permissions inheritance
- Tenant-scoped authorization

#### Available Permissions:
- `users.view`, `users.create`, `users.edit`, `users.delete`
- `roles.view`, `roles.create`, `roles.edit`, `roles.delete`
- `tenants.view`, `tenants.edit`
- `dashboard.view`, `system.admin`

### 5. Multi-Tenant Architecture

#### Tenant Isolation:
- Complete data isolation per tenant
- Tenant-specific roles and permissions
- Middleware-enforced tenant context
- Subdomain-based tenant resolution

#### Self-Service Tenant Registration:
- Automatic tenant creation
- Admin user provisioning
- Default role assignment

## API Endpoints

### Authentication Endpoints

```http
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
```

### Tenant Management

```http
POST /api/tenants/register
GET  /api/tenants/{slug}
GET  /api/tenants
```

### User Management

```http
GET    /api/users
GET    /api/users/{id}
POST   /api/users
PUT    /api/users/{id}
DELETE /api/users/{id}
POST   /api/users/{id}/roles
```

### Role Management

```http
GET    /api/roles
GET    /api/roles/{id}
POST   /api/roles
PUT    /api/roles/{id}
DELETE /api/roles/{id}
POST   /api/roles/{id}/permissions
GET    /api/roles/permissions
```

## Database Schema

The system uses PostgreSQL with the following tables:

- `tenants`: Tenant information
- `users`: User accounts with tenant isolation
- `roles`: Tenant-scoped roles
- `user_roles`: User-role assignments
- `user_claims`: User-specific claims
- `role_claims`: Role-based permissions

See `database/acm_tables.sql` for the complete schema.

## Configuration

### appsettings.json Example

```json
{
  "Jwt": {
    "Key": "your-super-secret-jwt-signing-key-here",
    "Issuer": "LaunchEase",
    "Audience": "LaunchEase.Client"
  },
  "AcmConnectionString": {
    "AcmDb": "Host=localhost;Database=launchease;Username=postgres;Password=password"
  },
  "App": {
    "AllowedOriginsForCors": ["http://localhost:3000", "https://yourdomain.com"]
  }
}
```

## Security Considerations

### Implemented Security Measures:

1. **Password Security**: Argon2id hashing with proper parameters
2. **SQL Injection Prevention**: Parameterized queries throughout
3. **Tenant Isolation**: Middleware-enforced tenant boundaries
4. **JWT Security**: Secure token generation and validation
5. **Account Lockout**: Configurable failed attempt limits
6. **CORS Configuration**: Proper cross-origin request handling

### Security Best Practices:

- Regular security stamp updates
- Secure random token generation
- Proper error handling without information leakage
- Input validation and sanitization
- Rate limiting (should be implemented)

## Usage Examples

### 1. Tenant Registration

```bash
curl -X POST http://localhost:5000/api/tenants/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Corp",
    "slug": "acme",
    "contactEmail": "admin@acme.com",
    "adminEmail": "admin@acme.com",
    "adminFirstName": "John",
    "adminLastName": "Doe",
    "adminPassword": "SecurePassword123!"
  }'
```

### 2. User Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@acme.com",
    "password": "SecurePassword123!",
    "tenantId": "tenant-uuid-here"
  }'
```

### 3. Create User (with JWT token)

```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "email": "user@acme.com",
    "firstName": "Jane",
    "lastName": "Smith",
    "password": "UserPassword123!",
    "roles": ["Editor"]
  }'
```

## Testing

### Unit Tests
- Repository tests with in-memory database
- Service tests with mocked dependencies
- Authorization handler tests

### Integration Tests
- End-to-end API tests
- Database integration tests
- Authentication flow tests

## Performance Considerations

1. **Database Indexing**: Proper indexes on frequently queried columns
2. **Connection Pooling**: Efficient database connection management
3. **Caching**: Claims and permission caching (can be implemented)
4. **Async Operations**: Non-blocking I/O throughout

## Deployment

### Prerequisites:
- .NET 9.0 Runtime
- PostgreSQL 12+
- Redis (optional, for distributed caching)

### Environment Variables:
```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__AcmDb="your-production-db-connection"
Jwt__Key="your-production-jwt-key"
```

## Monitoring and Logging

The system includes comprehensive logging using Serilog:
- Authentication events
- Authorization failures
- Database operations
- Error tracking
- Performance metrics

## Future Enhancements

1. **Password Policies**: Configurable complexity requirements
2. **Multi-Factor Authentication**: TOTP/SMS support
3. **OAuth Integration**: External provider support
4. **Audit Logging**: Complete activity tracking
5. **Rate Limiting**: API throttling
6. **Email Services**: Registration confirmation, password reset
7. **Advanced Permissions**: Resource-specific permissions
8. **Tenant Customization**: Per-tenant branding and configuration

## Troubleshooting

### Common Issues:

1. **JWT Token Expiry**: Implement automatic refresh
2. **Tenant Context Loss**: Verify middleware ordering
3. **Permission Errors**: Check role assignments and claims
4. **Database Connection**: Verify connection strings and network access

### Debug Logging:

Enable detailed logging in development:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Acm": "Debug"
    }
  }
}
```

This implementation provides a robust, scalable foundation for multi-tenant SaaS applications with comprehensive access control and security features.
