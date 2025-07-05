using Acm.Application.Repositories;
using Common.Domain.Interfaces;

namespace Acm.Application.Interfaces;

/// <summary>
/// Access Control Management Unit of Work interface
/// Exposes all ACM repositories and manages transactions
/// </summary>
public interface IAcmUnitOfWork : IDapperUnitOfWork
{
    /// <summary>
    /// User repository for user management operations
    /// </summary>
    IUserRepository Users { get; }
    
    /// <summary>
    /// Role repository for role management operations
    /// </summary>
    IRoleRepository Roles { get; }
    
    /// <summary>
    /// User-Role repository for user-role relationship operations
    /// </summary>
    IUserRoleRepository UserRoles { get; }
    
    /// <summary>
    /// Tenant repository for tenant management operations
    /// </summary>
    ITenantRepository Tenants { get; }
    
    /// <summary>
    /// User-Tenant repository for user-tenant relationship operations
    /// </summary>
    IUserTenantRepository UserTenants { get; }
    
    /// <summary>
    /// User claim repository for user claims management
    /// </summary>
    IUserClaimRepository UserClaims { get; }
    
    /// <summary>
    /// Role claim repository for role claims management
    /// </summary>
    IRoleClaimRepository RoleClaims { get; }
    
    /// <summary>
    /// Role management repository for complex role operations
    /// </summary>
    IRoleManagementRepository RoleManagement { get; }
    
    /// <summary>
    /// Permission management repository for permission operations
    /// </summary>
    IPermissionManagementRepository PermissionManagement { get; }
}
