using Acm.Application.Interfaces;
using Acm.Application.Repositories;
using Common.Application.Data;
using Common.Application.Services;
using Common.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Acm.Infrastructure.Persistence;

/// <summary>
/// Access Control Management Unit of Work implementation
/// </summary>
public class AcmUnitOfWork : DapperUnitOfWork, IAcmUnitOfWork
{
    private readonly LazyService<IUserRepository> _users;
    private readonly LazyService<IRoleRepository> _roles;
    private readonly LazyService<IUserRoleRepository> _userRoles;
    private readonly LazyService<ITenantRepository> _tenants;
    private readonly LazyService<IUserTenantRepository> _userTenants;
    private readonly LazyService<IUserClaimRepository> _userClaims;
    private readonly LazyService<IRoleClaimRepository> _roleClaims;
    private readonly LazyService<IRoleManagementRepository> _roleManagement;
    private readonly LazyService<IPermissionManagementRepository> _permissionManagement;

    public AcmUnitOfWork(
        IDbConnectionFactory connectionFactory,
        IServiceProvider serviceProvider)
        : base(connectionFactory, serviceProvider)
    {
        _users = new LazyService<IUserRepository>(serviceProvider);
        _roles = new LazyService<IRoleRepository>(serviceProvider);
        _userRoles = new LazyService<IUserRoleRepository>(serviceProvider);
        _tenants = new LazyService<ITenantRepository>(serviceProvider);
        _userTenants = new LazyService<IUserTenantRepository>(serviceProvider);
        _userClaims = new LazyService<IUserClaimRepository>(serviceProvider);
        _roleClaims = new LazyService<IRoleClaimRepository>(serviceProvider);
        _roleManagement = new LazyService<IRoleManagementRepository>(serviceProvider);
        _permissionManagement = new LazyService<IPermissionManagementRepository>(serviceProvider);
    }

    public IUserRepository Users => _users.Value;
    public IRoleRepository Roles => _roles.Value;
    public IUserRoleRepository UserRoles => _userRoles.Value;
    public ITenantRepository Tenants => _tenants.Value;
    public IUserTenantRepository UserTenants => _userTenants.Value;
    public IUserClaimRepository UserClaims => _userClaims.Value;
    public IRoleClaimRepository RoleClaims => _roleClaims.Value;
    public IRoleManagementRepository RoleManagement => _roleManagement.Value;
    public IPermissionManagementRepository PermissionManagement => _permissionManagement.Value;
}
