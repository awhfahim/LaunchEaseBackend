using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Common.Application.Data;
using Common.Application.Providers;
using Common.Application.Services;
using Microsoft.Extensions.Logging;

namespace Acm.Application.Services.RoleServices;

public class RoleService : IRoleService
{
    private readonly LazyService<IRoleRepository> _roleRepository;
    private readonly LazyService<IRoleClaimRepository> _roleClaimRepository;
    private readonly LazyService<IUserRoleRepository> _userRoleRepository;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<RoleService> _logger;
    private readonly IGuidProvider _guidProvider;

    public RoleService(LazyService<IRoleRepository> roleRepository,
        LazyService<IRoleClaimRepository> roleClaimRepository, LazyService<IUserRoleRepository> userRoleRepository,
        IDbConnectionFactory dbConnectionFactory, ILogger<RoleService> logger, IGuidProvider guidProvider)
    {
        _roleRepository = roleRepository;
        _roleClaimRepository = roleClaimRepository;
        _userRoleRepository = userRoleRepository;
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
        _guidProvider = guidProvider;
    }

    public async Task<(bool result, string message)> DeleteRoleAsync(Guid roleId, Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var role = await _roleRepository.Value.GetByIdAsync(roleId);
            if (role == null || role.TenantId != tenantId)
            {
                return (false, "Role not found.");
            }

            await _roleClaimRepository.Value.DeleteByRoleIdAsync(roleId, connection, transaction);
            await _userRoleRepository.Value.DeleteByRoleIdAsync(roleId, connection, transaction);
            await _roleRepository.Value.DeleteAsync(roleId, connection, transaction);

            await transaction.CommitAsync(cancellationToken);
            return (true, "Role deleted successfully.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting role with ID {RoleId} for tenant {TenantId}", roleId, tenantId);
            throw;
        }
    }

    public async Task<(bool result, string message)> AssignPermissionsAsync(Guid roleId, Guid tenantId,
        IEnumerable<Guid> claimIds, CancellationToken cancellationToken)
    {
        await using var connection = await _dbConnectionFactory.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var role = await _roleRepository.Value.GetByIdAsync(roleId);
            if (role == null || role.TenantId != tenantId)
            {
                return (false, "Role not found");
            }

            await _roleClaimRepository.Value.DeleteAsync(roleId, connection, transaction);
            
            List<RoleClaim> claims = [];
            foreach (var claimId in claimIds)
            {
                var roleClaim = new RoleClaim
                {
                    Id = _guidProvider.SortableGuid(),
                    RoleId = roleId,
                    MasterClaimId = claimId
                };
                claims.Add(roleClaim);
            }

            await _roleClaimRepository.Value.AddRangeAsync(claims, connection, transaction);

            await transaction.CommitAsync();

            return (true, "Permissions assigned successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error assigning permissions to role with ID {RoleId} for tenant {TenantId}", roleId,
                tenantId);
            return (false, "An error occurred while assigning permissions.");
        }
    }

    public Task<IEnumerable<Role>> GetRolesByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return _roleRepository.Value.GetByTenantIdAsync(tenantId, cancellationToken);
    }
}