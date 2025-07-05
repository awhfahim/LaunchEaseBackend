using Acm.Application.Interfaces;
using Acm.Application.Repositories;
using Common.Application.Data;
using Common.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Acm.Infrastructure.Persistence;

/// <summary>
/// Access Control Management Unit of Work implementation
/// </summary>
public class AcmUnitOfWork : DapperUnitOfWork, IAcmUnitOfWork
{

    private readonly Lazy<IUserRepository> _users;
    private readonly Lazy<IRoleRepository> _roles;
    private readonly Lazy<IUserRoleRepository> _userRoles;
    private readonly Lazy<ITenantRepository> _tenants;
    private readonly Lazy<IUserTenantRepository> _userTenants;
    private readonly Lazy<IUserClaimRepository> _userClaims;
    private readonly Lazy<IRoleClaimRepository> _roleClaims;
    private readonly Lazy<IRoleManagementRepository> _roleManagement;
    private readonly Lazy<IPermissionManagementRepository> _permissionManagement;

    public AcmUnitOfWork(
        IDbConnectionFactory connectionFactory,
        IServiceProvider serviceProvider)
        : base(connectionFactory, serviceProvider)
    {
        _users = new Lazy<IUserRepository>(serviceProvider.GetRequiredService<IUserRepository>);
        _roles = new Lazy<IRoleRepository>(serviceProvider.GetRequiredService<IRoleRepository>);
        _userRoles = new Lazy<IUserRoleRepository>(serviceProvider.GetRequiredService<IUserRoleRepository>);
        _tenants = new Lazy<ITenantRepository>(serviceProvider.GetRequiredService<ITenantRepository>);
        _userTenants = new Lazy<IUserTenantRepository>(serviceProvider.GetRequiredService<IUserTenantRepository>);
        _userClaims = new Lazy<IUserClaimRepository>(serviceProvider.GetRequiredService<IUserClaimRepository>);
        _roleClaims = new Lazy<IRoleClaimRepository>(serviceProvider.GetRequiredService<IRoleClaimRepository>);
        _roleManagement = new Lazy<IRoleManagementRepository>(serviceProvider.GetRequiredService<IRoleManagementRepository>);
        _permissionManagement = new Lazy<IPermissionManagementRepository>(serviceProvider.GetRequiredService<IPermissionManagementRepository>);
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
