# Dapper Unit of Work and Generic Repository Pattern Implementation

This document explains the implementation and usage of the Unit of Work and Generic Repository pattern using Dapper for high-performance, multitenant SaaS applications.

## Architecture Overview

The implementation provides:

1. **Generic Repository Pattern** - Base CRUD operations for all entities
2. **Unit of Work Pattern** - Transaction management across multiple repositories  
3. **Custom Repository Extensions** - Entity-specific methods
4. **Transaction Support** - Both automatic and manual transaction control
5. **Dependency Injection** - Full DI container integration

## Core Components

### 1. IGenericRepository<TEntity, TKey>

Base interface providing standard CRUD operations:

```csharp
public interface IGenericRepository<TEntity, TKey> where TEntity : class
{
    // Basic operations without transaction
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(int? limit = null, int? offset = null, CancellationToken cancellationToken = default);
    Task<TKey> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);
    
    // Operations with transaction support
    Task<TEntity?> GetByIdAsync(TKey id, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    Task<TKey> InsertAsync(TEntity entity, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);
    // ... etc
}
```

### 2. IDapperUnitOfWork

Main Unit of Work interface for transaction management:

```csharp
public interface IDapperUnitOfWork : IAsyncDisposable, IDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
    bool HasActiveTransaction { get; }
    
    Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    
    Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> work, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> work, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    
    TRepository GetRepository<TRepository>() where TRepository : class;
}
```

### 3. IAcmUnitOfWork

Access Control Management specific Unit of Work exposing all repositories:

```csharp
public interface IAcmUnitOfWork : IDapperUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    IUserRoleRepository UserRoles { get; }
    ITenantRepository Tenants { get; }
    IUserTenantRepository UserTenants { get; }
    // ... other repositories
}
```

## Implementation Details

### 1. DapperGenericRepository<TEntity, TKey>

Abstract base class providing common CRUD operations:

```csharp
public abstract class DapperGenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey> 
    where TEntity : class
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly string TableName;
    protected readonly string PrimaryKeyColumn;
    
    // Abstract methods for SQL generation
    protected abstract string GetInsertSql();
    protected abstract string GetUpdateSql();
    protected abstract string GetSelectSql();
    
    // Helper methods for SQL building
    protected virtual string BuildInsertSql(IEnumerable<string> columns, string returningColumn = "id");
    protected virtual string BuildUpdateSql(IEnumerable<string> columns, string whereClause = null);
    protected virtual string BuildSelectSql(IEnumerable<string> columns);
}
```

### 2. Enhanced Repository Example

The `EnhancedUserRepository` shows how to extend the generic repository:

```csharp
public class EnhancedUserRepository : DapperGenericRepository<User, Guid>, IUserRepository
{
    protected override string GetInsertSql()
    {
        return BuildInsertSql(new[]
        {
            "id", "email", "first_name", "last_name", "password_hash", "security_stamp",
            "is_email_confirmed", "is_globally_locked", // ... other columns
        });
    }
    
    // Custom methods
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);
        return await GetByEmailAsync(email, connection, null, cancellationToken);
    }
    
    // Custom methods with transaction support
    public async Task<User?> GetByEmailAsync(string email, IDbConnection connection, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT * FROM users WHERE email = @Email";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email }, transaction);
    }
}
```

## Usage Examples

### 1. Basic Repository Usage

```csharp
public class UserService
{
    private readonly IUserRepository _userRepository;
    
    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User?> GetUserAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
    
    public async Task<Guid> CreateUserAsync(User user)
    {
        return await _userRepository.InsertAsync(user);
    }
}
```

### 2. Unit of Work with Automatic Transaction

```csharp
public class UserManagementService
{
    private readonly IAcmUnitOfWork _unitOfWork;
    
    public async Task<Guid> CreateUserWithTenantAsync(User user, Guid tenantId, Guid roleId)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            // 1. Create user
            var userId = await _unitOfWork.Users.InsertAsync(user, connection, transaction);
            
            // 2. Add to tenant
            var userTenant = new UserTenant
            {
                UserId = userId,
                TenantId = tenantId,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            await _unitOfWork.UserTenants.AddUserToTenantAsync(userTenant);
            
            // 3. Assign role
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                TenantId = tenantId
            };
            await _unitOfWork.UserRoles.CreateAsync(userRole);
            
            return userId;
        });
    }
}
```

### 3. Manual Transaction Control

```csharp
public async Task BulkOperationAsync(IEnumerable<User> users)
{
    await _unitOfWork.BeginTransactionAsync();
    
    try
    {
        foreach (var user in users)
        {
            await _unitOfWork.Users.InsertAsync(user, _unitOfWork.Connection, _unitOfWork.Transaction);
        }
        
        await _unitOfWork.CommitAsync();
    }
    catch
    {
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

### 4. Read-Only Operations (No Transaction)

```csharp
public async Task<UserWithTenantsDto> GetUserWithTenantsAsync(Guid userId)
{
    var user = await _unitOfWork.Users.GetByIdAsync(userId);
    var tenants = await _unitOfWork.UserTenants.GetUserAccessibleTenantsAsync(userId);
    
    return new UserWithTenantsDto
    {
        User = user,
        Tenants = tenants.ToList()
    };
}
```

## Performance Features

### 1. Connection Management
- Connections are created only when needed
- Automatic connection disposal with `using` statements
- Shared connection for transactions

### 2. SQL Optimization
- Raw SQL with Dapper for maximum performance
- Prepared statements through parameterized queries
- Optimized column selection

### 3. Transaction Efficiency
- Minimal transaction scope
- Automatic rollback on exceptions
- Support for different isolation levels

## Testing

### 1. Unit Testing with Mocks

```csharp
[Test]
public async Task CreateUserAsync_ShouldReturnUserId()
{
    // Arrange
    var mockUnitOfWork = new Mock<IAcmUnitOfWork>();
    var expectedUserId = Guid.NewGuid();
    
    mockUnitOfWork.Setup(x => x.Users.InsertAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(expectedUserId);
    
    var service = new UserManagementService(mockUnitOfWork.Object, Mock.Of<ILogger<UserManagementService>>());
    
    // Act
    var result = await service.CreateUserAsync(new User { Email = "test@example.com" });
    
    // Assert
    Assert.AreEqual(expectedUserId, result);
}
```

### 2. Integration Testing

```csharp
[Test]
public async Task CreateUserWithTenant_ShouldCreateUserAndAssignToTenant()
{
    // Arrange
    using var unitOfWork = _serviceProvider.GetRequiredService<IAcmUnitOfWork>();
    var user = new User { Email = "integration@test.com" };
    var tenantId = Guid.NewGuid();
    var roleId = Guid.NewGuid();
    
    // Act
    var userId = await _userManagementService.CreateUserWithTenantAsync(user, tenantId, roleId);
    
    // Assert
    var createdUser = await unitOfWork.Users.GetByIdAsync(userId);
    var userTenant = await unitOfWork.UserTenants.GetUserTenantAsync(userId, tenantId);
    
    Assert.IsNotNull(createdUser);
    Assert.IsNotNull(userTenant);
}
```

## Configuration

### Dependency Injection Setup

```csharp
// In Program.cs or Startup.cs
services.AddScoped<IDbConnectionFactory, PgConnectionFactory>();
services.AddScoped<IDapperUnitOfWork, DapperUnitOfWork>();
services.AddScoped<IAcmUnitOfWork, AcmUnitOfWork>();

// Repositories
services.AddScoped<IUserRepository, EnhancedUserRepository>();
services.AddScoped<IRoleRepository, RoleRepository>();
// ... other repositories

// Services
services.AddScoped<UserManagementService>();
```

## Benefits

1. **Performance** - Direct SQL with Dapper, no ORM overhead
2. **Testability** - Easy to mock interfaces for unit testing
3. **Maintainability** - Clear separation of concerns
4. **Flexibility** - Support for custom SQL and complex operations
5. **Transaction Safety** - Automatic rollback on failures
6. **Scalability** - Efficient connection and transaction management

## Best Practices

1. **Use transactions for multi-operation scenarios**
2. **Prefer automatic transaction management with `ExecuteInTransactionAsync`**
3. **Keep transaction scope minimal**
4. **Use read-only operations without transactions when possible**
5. **Implement proper error handling and logging**
6. **Follow the repository naming conventions**
7. **Use cancellation tokens for async operations**

This implementation provides a robust, high-performance foundation for your multitenant SaaS application with clean architecture principles and excellent testability.
