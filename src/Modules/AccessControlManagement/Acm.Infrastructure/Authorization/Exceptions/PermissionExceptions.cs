namespace Acm.Infrastructure.Authorization.Exceptions;

/// <summary>
/// Exception thrown when a user tries to access resources outside their tenant boundary
/// </summary>
public class TenantIsolationViolationException : UnauthorizedAccessException
{
    public Guid UserTenantId { get; }
    public Guid RequestedTenantId { get; }
    public string Operation { get; }

    public TenantIsolationViolationException(Guid userTenantId, Guid requestedTenantId, string operation)
        : base($"User from tenant {userTenantId} attempted to access resources in tenant {requestedTenantId} during operation: {operation}")
    {
        UserTenantId = userTenantId;
        RequestedTenantId = requestedTenantId;
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when a user lacks the required permission for an operation
/// </summary>
public class InsufficientPermissionException : UnauthorizedAccessException
{
    public string RequiredPermission { get; }
    public string[] UserPermissions { get; }
    public string Operation { get; }

    public InsufficientPermissionException(string requiredPermission, string[] userPermissions, string operation)
        : base($"Operation '{operation}' requires permission '{requiredPermission}'. User permissions: [{string.Join(", ", userPermissions)}]")
    {
        RequiredPermission = requiredPermission;
        UserPermissions = userPermissions;
        Operation = operation;
    }
}

/// <summary>
/// Exception thrown when a global operation is attempted without proper authorization
/// </summary>
public class GlobalOperationDeniedException : UnauthorizedAccessException
{
    public string RequiredLevel { get; }
    public string Operation { get; }

    public GlobalOperationDeniedException(string requiredLevel, string operation)
        : base($"Global operation '{operation}' requires {requiredLevel} level access")
    {
        RequiredLevel = requiredLevel;
        Operation = operation;
    }
}
