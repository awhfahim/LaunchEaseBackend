using System.Security.Claims;

namespace Acm.Infrastructure.Authorization;

/// <summary>
/// Helper class for checking permissions in controllers and services
/// Supports hierarchical permission checking for multi-tenant system
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// Checks if the user has the required permission, considering hierarchical permissions
    /// </summary>
    public static bool HasPermission(ClaimsPrincipal user, string requiredPermission)
    {
        var userPermissions = user.FindAll("permission")
            .Select(c => c.Value)
            .ToList();

        // Direct permission check
        if (userPermissions.Contains(requiredPermission))
        {
            return true;
        }

        // Hierarchical permission check
        return HasHierarchicalPermission(userPermissions, requiredPermission);
    }

    /// <summary>
    /// Checks if user is a business owner (highest level access)
    /// </summary>
    public static bool IsBusinessOwner(ClaimsPrincipal user)
    {
        return user.FindAll("permission")
            .Any(c => c.Value == PermissionConstants.BusinessOwner);
    }

    /// <summary>
    /// Checks if user is a system administrator
    /// </summary>
    public static bool IsSystemAdmin(ClaimsPrincipal user)
    {
        var permissions = user.FindAll("permission").Select(c => c.Value).ToList();
        return permissions.Contains(PermissionConstants.SystemAdmin) || 
               permissions.Contains(PermissionConstants.BusinessOwner);
    }

    /// <summary>
    /// Checks if user can access global/cross-tenant operations
    /// </summary>
    public static bool CanAccessGlobalOperations(ClaimsPrincipal user)
    {
        var permissions = user.FindAll("permission").Select(c => c.Value).ToList();
        return permissions.Contains(PermissionConstants.BusinessOwner) ||
               permissions.Contains(PermissionConstants.SystemAdmin) ||
               permissions.Contains(PermissionConstants.CrossTenantAccess);
    }

    /// <summary>
    /// Gets the tenant ID from the user's claims
    /// </summary>
    public static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var tenantClaim = user.FindFirst("tenant_id");
        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }

    /// <summary>
    /// Checks if user can perform action on specific tenant
    /// </summary>
    public static bool CanAccessTenant(ClaimsPrincipal user, Guid targetTenantId)
    {
        // Business owner and system admin can access any tenant
        if (IsBusinessOwner(user) || IsSystemAdmin(user))
        {
            return true;
        }

        // Cross-tenant access permission
        if (CanAccessGlobalOperations(user))
        {
            return true;
        }

        // Check if user belongs to the target tenant
        var userTenantId = GetTenantId(user);
        return userTenantId.HasValue && userTenantId.Value == targetTenantId;
    }

    private static bool HasHierarchicalPermission(IList<string> userPermissions, string requiredPermission)
    {
        // Business Owner has access to everything
        if (userPermissions.Contains(PermissionConstants.BusinessOwner))
        {
            return true;
        }

        // System Admin has access to all system and tenant operations
        if (userPermissions.Contains(PermissionConstants.SystemAdmin))
        {
            if (IsSystemOrGlobalPermission(requiredPermission) || IsTenantScopedPermission(requiredPermission))
            {
                return true;
            }
        }

        // Cross-tenant access allows global operations
        if (userPermissions.Contains(PermissionConstants.CrossTenantAccess))
        {
            if (IsGlobalPermission(requiredPermission))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSystemOrGlobalPermission(string permission)
    {
        return permission.StartsWith("system.") || permission.StartsWith("global.");
    }

    private static bool IsGlobalPermission(string permission)
    {
        return permission.StartsWith("global.");
    }

    private static bool IsTenantScopedPermission(string permission)
    {
        return !permission.StartsWith("global.") && 
               !permission.StartsWith("system.") && 
               !permission.StartsWith("business.") &&
               !permission.StartsWith("cross.");
    }
}
