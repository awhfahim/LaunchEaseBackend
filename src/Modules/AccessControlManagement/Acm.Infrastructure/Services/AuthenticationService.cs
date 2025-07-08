using Acm.Application.Services;
using Acm.Application.Repositories;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Acm.Application.Options;
using Acm.Application.Services.Interfaces;
using Common.Application.Providers;
using Common.Application.Services;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Acm.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly LazyService<IUserRepository> _userRepository;
    private readonly LazyService<IUserClaimRepository> _userClaimRepository;
    private readonly LazyService<IRoleClaimRepository> _roleClaimRepository;
    private readonly LazyService<ITenantRepository> _tenantRepository;
    private readonly LazyService<IUserTenantRepository> _userTenantRepository;
    private readonly LazyService<IUserRoleRepository> _userRoleRepository;
    private readonly ICryptographyService _cryptographyService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;

    public AuthenticationService(
        LazyService<IUserRepository> userRepository,
        LazyService<IUserClaimRepository> userClaimRepository,
        LazyService<IRoleClaimRepository> roleClaimRepository,
        LazyService<ITenantRepository> tenantRepository,
        LazyService<IUserTenantRepository> userTenantRepository,
        LazyService<IUserRoleRepository> userRoleRepository,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions, 
        ICryptographyService cryptographyService)
    {
        _userRepository = userRepository;
        _userClaimRepository = userClaimRepository;
        _roleClaimRepository = roleClaimRepository;
        _tenantRepository = tenantRepository;
        _userTenantRepository = userTenantRepository;
        _userRoleRepository = userRoleRepository;
        _dateTimeProvider = dateTimeProvider;
        _cryptographyService = cryptographyService;
        _jwtOptions = jwtOptions.Value;
    }

    private Task<string> GenerateTemporaryJwtTokenAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret ??
                                         throw new InvalidOperationException("JWT key not configured"));
        var issuer = _jwtOptions.Issuer;
        var audience = _jwtOptions.Audience;

        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("tenant_id", Guid.NewGuid().ToString())
        };


        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(tokenClaims),
            Expires = _dateTimeProvider.CurrentUtcTime.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Task.FromResult(tokenHandler.WriteToken(token));
    }

    public async Task<IEnumerable<Claim>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>();

        // Get direct user claims
        var userClaims = _userClaimRepository.Value.GetClaimsForUserAsync(userId, cancellationToken);

        // Get role-based claims
        var roleClaims = _roleClaimRepository.Value.GetClaimsForUserRolesAsync(userId, cancellationToken);

        await Task.WhenAll(userClaims, roleClaims);

        claims.AddRange(userClaims.Result);
        claims.AddRange(roleClaims.Result);

        return claims;
    }
    
    public async Task<InitialAuthenticationResult> AuthenticateUserAsync(string email, string password,
        CancellationToken cancellationToken = default)
    {
        // Find user by email (global lookup)
        var user = await _userRepository.Value.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            return InitialAuthenticationResult.Failed("Invalid credentials");
        }

        // Check if user is locked out
        if (user is { IsGloballyLocked: true, GlobalLockoutEnd: not null } &&
            user.GlobalLockoutEnd > _dateTimeProvider.CurrentUtcTime)
        {
            return InitialAuthenticationResult.LockedOut();
        }

        var isValidPassword = await _cryptographyService.ValidatePasswordAsync(password, user.PasswordHash);
        if (!isValidPassword)
        {
            // Increment failed attempts
            user.GlobalAccessFailedCount++;
            await _userRepository.Value.SetAccessFailedCountAsync(user.Id, user.GlobalAccessFailedCount,
                cancellationToken);

            // Lock account if too many failed attempts
            if (user.GlobalAccessFailedCount >= 7)
            {
                var lockoutEnd = _dateTimeProvider.CurrentUtcTime.AddMinutes(30);
                await _userRepository.Value.SetLockoutEndAsync(user.Id, lockoutEnd, cancellationToken);
            }

            return InitialAuthenticationResult.Failed("Invalid credentials");
        }

        if (!user.IsEmailConfirmed)
        {
            return InitialAuthenticationResult.EmailNotConfirmed();
        }

        // Reset failed attempts on successful login
        if (user.GlobalAccessFailedCount > 0)
        {
            await _userRepository.Value.SetAccessFailedCountAsync(user.Id, 0, cancellationToken);
        }

        // Clear lockout
        if (user.IsGloballyLocked)
        {
            await _userRepository.Value.SetLockoutEndAsync(user.Id, null, cancellationToken);
        }

        // Update last login
        user.LastLoginAt = _dateTimeProvider.CurrentUtcTime;
        await _userRepository.Value.UpdateAsync(user, cancellationToken);

        // Get accessible tenants for this user
        var accessibleTenants =
            await _userTenantRepository.Value.GetUserAccessibleTenantsAsync(user.Id, cancellationToken);

        var tenantInfos = new List<TenantInfo>();
        foreach (var tenant in accessibleTenants)
        {
            // Get user roles for this tenant
            var userRoles =
                await _userRoleRepository.Value.GetRoleNamesForUserAsync(user.Id, tenant.Id, cancellationToken);

            tenantInfos.Add(new TenantInfo
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                LogoUrl = tenant.LogoUrl,
                UserRoles = userRoles
            });
        }

        var token = await GenerateTemporaryJwtTokenAsync(user.Id, cancellationToken);

        return InitialAuthenticationResult.Success(user.Id, tenantInfos, token);
    }

    public async Task<TenantAuthenticationResult> AuthenticateWithTenantAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.Value.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TenantAuthenticationResult.Failed("User not found");
        }

        var tenant = await _tenantRepository.Value.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return TenantAuthenticationResult.Failed("Tenant not found");
        }

        var isMember = await _userTenantRepository.Value.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);
        if (!isMember)
        {
            return TenantAuthenticationResult.Failed("User is not a member of this tenant");
        }

        var claims = await _cryptographyService.GetUserClaimsForTenantAsync(userId, tenantId, cancellationToken);

        var token = await _cryptographyService.GenerateJwtTokenAsync(userId, tenantId, claims, cancellationToken);

        var roles = await _userRoleRepository.Value.GetRoleNamesForUserAsync(userId, tenantId, cancellationToken);

        var permissions = claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        var userRoles = roles as string[] ?? roles.ToArray();
        var tenantInfo = new TenantInfo
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            LogoUrl = tenant.LogoUrl,
            UserRoles = userRoles
        };

        return TenantAuthenticationResult.Success(
            token,
            userId,
            tenantId,
            _jwtOptions.AccessTokenExpiryMinutes,
            tenantInfo,
            userRoles,
            permissions);
    }

    public async Task<TenantAuthenticationResult> SwitchTenantAsync(Guid userId, Guid newTenantId,
        CancellationToken cancellationToken = default)
    {
        return await AuthenticateWithTenantAsync(userId, newTenantId, cancellationToken);
    }
}