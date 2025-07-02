# LaunchEase Backend - Access Control Management Implementation

This document outlines the complete implementation of a multi-tenant access control system using Dapper, ASP.NET Core Identity, and JWT authentication.

## Architecture Overview

The system implements a multi-tenant SaaS architecture with the following key components:

- **Data Access Layer**: Dapper-based repositories for high-performance database operations
- **Identity System**: Custom ASP.NET Core Identity stores backed by Dapper
- **Authentication**: JWT-based authentication with tenant isolation
- **Authorization**: Policy-based authorization with permission claims
- **Multi-tenancy**: Complete tenant isolation with middleware enforcement

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
