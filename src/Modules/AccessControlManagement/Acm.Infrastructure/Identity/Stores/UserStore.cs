using Acm.Application.Repositories;
using Acm.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Acm.Infrastructure.Identity.Stores;

public class UserStore : IUserStore<User>, IUserPasswordStore<User>, IUserClaimStore<User>, 
    IUserRoleStore<User>, IUserLockoutStore<User>, IUserSecurityStampStore<User>, IUserEmailStore<User>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserClaimRepository _userClaimRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;

    public UserStore(
        IUserRepository userRepository,
        IUserClaimRepository userClaimRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _userClaimRepository = userClaimRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
    }

    public void Dispose() { }

    // IUserStore<User> implementation
    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await _userRepository.CreateAsync(user, cancellationToken: cancellationToken);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await _userRepository.UpdateAsync(user, cancellationToken);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await _userRepository.DeleteAsync(user.Id, cancellationToken);
            return IdentityResult.Success;
        }
        catch (Exception ex)
        {
            return IdentityResult.Failed(new IdentityError { Description = ex.Message });
        }
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(userId, out var guid))
        {
            return await _userRepository.GetByIdAsync(guid, cancellationToken);
        }
        return null;
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        // In this case, username is email, so we need tenant context
        // This might need to be handled differently based on your tenant resolution strategy
        throw new NotImplementedException("FindByNameAsync requires tenant context. Use FindByEmailAsync instead.");
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email.ToUpperInvariant());
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email);
    }

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        // No action needed as we use email as username
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.Email = userName ?? throw new ArgumentNullException(nameof(userName));
        return Task.CompletedTask;
    }

    // IUserPasswordStore<User> implementation
    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        return Task.CompletedTask;
    }

    // IUserClaimStore<User> implementation
    public async Task<IList<Claim>> GetClaimsAsync(User user, CancellationToken cancellationToken)
    {
        var claims = await _userClaimRepository.GetClaimsForUserAsync(user.Id, cancellationToken);
        return claims.ToList();
    }

    public async Task AddClaimsAsync(User user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var userClaim = new UserClaim
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            };
            await _userClaimRepository.CreateAsync(userClaim, cancellationToken);
        }
    }

    public async Task ReplaceClaimAsync(User user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var existingClaim = await _userClaimRepository.GetAsync(user.Id, claim.Type, claim.Value, cancellationToken);
        if (existingClaim != null)
        {
            existingClaim.ClaimType = newClaim.Type;
            existingClaim.ClaimValue = newClaim.Value;
            await _userClaimRepository.UpdateAsync(existingClaim, cancellationToken);
        }
    }

    public async Task RemoveClaimsAsync(User user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            await _userClaimRepository.DeleteAsync(user.Id, claim.Type, claim.Value, cancellationToken);
        }
    }

    public async Task<IList<User>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        // This would require a more complex query - implement as needed
        throw new NotImplementedException();
    }

    // IUserRoleStore<User> implementation
    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByNameAsync(roleName, user.TenantId, cancellationToken);
        if (role != null)
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id
            };
            await _userRoleRepository.CreateAsync(userRole, cancellationToken: cancellationToken);
        }
    }

    public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByNameAsync(roleName, user.TenantId, cancellationToken);
        if (role != null)
        {
            await _userRoleRepository.DeleteAsync(user.Id, role.Id, cancellationToken);
        }
    }

    public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        var roleNames = await _userRoleRepository.GetRoleNamesForUserAsync(user.Id, cancellationToken);
        return roleNames.ToList();
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync(user, cancellationToken);
        return roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        // This would require tenant context and a more complex query
        throw new NotImplementedException("GetUsersInRoleAsync requires tenant context");
    }

    // IUserLockoutStore<User> implementation
    public async Task<DateTimeOffset?> GetLockoutEndDateAsync(User user, CancellationToken cancellationToken)
    {
        return await _userRepository.GetLockoutEndAsync(user.Id, cancellationToken);
    }

    public async Task SetLockoutEndDateAsync(User user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        await _userRepository.SetLockoutEndAsync(user.Id, lockoutEnd, cancellationToken);
    }

    public async Task<int> IncrementAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        var count = await _userRepository.GetAccessFailedCountAsync(user.Id, cancellationToken);
        count++;
        await _userRepository.SetAccessFailedCountAsync(user.Id, count, cancellationToken);
        user.AccessFailedCount = count;
        return count;
    }

    public async Task ResetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        await _userRepository.SetAccessFailedCountAsync(user.Id, 0, cancellationToken);
        user.AccessFailedCount = 0;
    }

    public async Task<int> GetAccessFailedCountAsync(User user, CancellationToken cancellationToken)
    {
        return await _userRepository.GetAccessFailedCountAsync(user.Id, cancellationToken);
    }

    public Task<bool> GetLockoutEnabledAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(true); // Lockout is always enabled
    }

    public Task SetLockoutEnabledAsync(User user, bool enabled, CancellationToken cancellationToken)
    {
        // No action needed as lockout is always enabled
        return Task.CompletedTask;
    }

    // IUserSecurityStampStore<User> implementation
    public Task<string?> GetSecurityStampAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.SecurityStamp);
    }

    public Task SetSecurityStampAsync(User user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    // IUserEmailStore<User> implementation
    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.IsEmailConfirmed);
    }

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email.ToUpperInvariant());
    }

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email ?? throw new ArgumentNullException(nameof(email));
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.IsEmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        // No action needed as we use the email directly
        return Task.CompletedTask;
    }

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        // This would require tenant context - you might need to implement tenant resolution
        throw new NotImplementedException("FindByEmailAsync requires tenant context");
    }
}
