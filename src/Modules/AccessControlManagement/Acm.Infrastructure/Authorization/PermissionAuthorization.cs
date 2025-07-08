using Microsoft.AspNetCore.Authorization;

namespace Acm.Infrastructure.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string[] Permissions { get; }

    public PermissionRequirement(string permissionString)
    {
        Permissions = permissionString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userPermissions = context.User.FindAll("permission")
            .Select(c => c.Value)
            .ToList();
        
        foreach (var requiredPermission in requirement.Permissions)
        {
            if (userPermissions.Contains(requiredPermission) || 
                HasHierarchicalPermission(userPermissions, requiredPermission))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    private static bool HasHierarchicalPermission(IList<string> userPermissions, string requiredPermission)
    {
        if (userPermissions.Contains(PermissionConstants.BusinessOwner))
        {
            return true;
        }
        
        if (userPermissions.Contains(PermissionConstants.SystemAdmin))
        {
            if (IsSystemOrGlobalPermission(requiredPermission) || IsTenantScopedPermission(requiredPermission))
            {
                return true;
            }
        }

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

public static class PermissionConstants
{
    // ===========================================
    // TENANT-SCOPED PERMISSIONS
    // (Users can only access data within their tenant)
    // ===========================================
    
    // User Management (Tenant-scoped)
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string UsersManageRoles = "users.manage.roles";

    // Role Management (Tenant-scoped)
    public const string RolesAndPermissionView = "roles.view";
    public const string RolesAndPermissionCreate = "roles.create";
    public const string RolesAndPermissionEdit = "roles.edit";
    public const string RolesAndPermissionDelete = "roles.delete";
    public const string RolesManagePermissions = "roles.manage.permissions";
    

    // Tenant Settings (Own tenant only)
    public const string TenantSettingsView = "tenant.settings.view";
    public const string TenantSettingsEdit = "tenant.settings.edit";

    // Dashboard (Tenant-scoped)
    public const string DashboardView = "dashboard.view";

    // Authentication & Authorization (Tenant-scoped)
    public const string AuthenticationView = "authentication.view";
    public const string AuthenticationEdit = "authentication.edit";
    public const string AuthorizationView = "authorization.view";
    public const string AuthorizationEdit = "authorization.edit";

    // ===========================================
    // SYSTEM-WIDE PERMISSIONS
    // (Business owner/system admin can access all tenants)
    // ===========================================
    
    // Global Tenant Management
    public const string GlobalTenantsView = "global.tenants.view";
    public const string GlobalTenantsCreate = "global.tenants.create";
    public const string GlobalTenantsEdit = "global.tenants.edit";
    public const string GlobalTenantsDelete = "global.tenants.delete";
    
    // Global User Management (Cross-tenant)
    public const string GlobalUsersView = "global.users.view";
    public const string GlobalUsersCreate = "global.users.create";
    public const string GlobalUsersEdit = "global.users.edit";
    public const string GlobalUsersDelete = "global.users.delete";
    
    // Global Role Management (Cross-tenant)
    public const string GlobalRolesView = "global.roles.view";
    public const string GlobalRolesCreate = "global.roles.create";
    public const string GlobalRolesEdit = "global.roles.edit";
    public const string GlobalRolesDelete = "global.roles.delete";

    // System Administration
    public const string SystemAdmin = "system.admin";
    public const string SystemDashboard = "system.dashboard.view";
    public const string SystemLogs = "system.logs.view";
    public const string SystemConfiguration = "system.configuration.edit";

    // ===========================================
    // BUSINESS OWNER PERMISSIONS
    // (Highest level - can access everything)
    // ===========================================
    
    public const string BusinessOwner = "business.owner";
    public const string CrossTenantAccess = "cross.tenant.access";
}
