using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Acm.Infrastructure.Identity.Stores;

public class RoleStore : IRoleStore<Role>, IRoleClaimStore<Role>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IRoleClaimRepository _roleClaimRepository;

    public RoleStore(IRoleRepository roleRepository, IRoleClaimRepository roleClaimRepository)
    {
        _roleRepository = roleRepository;
        _roleClaimRepository = roleClaimRepository;
    }

    public void Dispose() { }

    // IRoleStore<Role> implementation
    public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
    {
        try
        {
            await _roleRepository.CreateAsync(role, cancellationToken);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        try
        {
            await _roleRepository.UpdateAsync(role, cancellationToken);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
    {
        try
        {
            await _roleRepository.DeleteAsync(role.Id, cancellationToken);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    public async Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(roleId, out var guid))
        {
            return await _roleRepository.GetByIdAsync(guid, cancellationToken);
        }
        return null;
    }

    public async Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        // This would require tenant context - you might need to implement tenant resolution
        throw new NotImplementedException("FindByNameAsync requires tenant context");
    }

    public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(role.Name.ToUpperInvariant());
    }

    public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id.ToString());
    }

    public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
    {
        // No action needed as we normalize on the fly
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName ?? throw new ArgumentNullException(nameof(roleName));
        return Task.CompletedTask;
    }

    // IRoleClaimStore<Role> implementation
    public async Task<IList<Claim>> GetClaimsAsync(Role role, CancellationToken cancellationToken = default)
    {
        var claims = await _roleClaimRepository.GetClaimsForRoleAsync(role.Id, cancellationToken);
        return claims.ToList();
    }

    public async Task AddClaimAsync(Role role, Claim claim, CancellationToken cancellationToken = default)
    {
        var roleClaim = new RoleClaim
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            ClaimType = claim.Type,
            ClaimValue = claim.Value
        };
        await _roleClaimRepository.CreateAsync(roleClaim, cancellationToken);
    }

    public async Task RemoveClaimAsync(Role role, Claim claim, CancellationToken cancellationToken = default)
    {
        await _roleClaimRepository.DeleteAsync(role.Id, claim.Type, claim.Value, cancellationToken);
    }
}
