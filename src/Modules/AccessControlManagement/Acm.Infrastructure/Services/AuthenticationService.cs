using Acm.Application.Services;
using Acm.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Acm.Application.Options;
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
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    private JwtOptions _jwtOptions;

    public AuthenticationService(
        LazyService<IUserRepository> userRepository,
        LazyService<IUserClaimRepository> userClaimRepository,
        LazyService<IRoleClaimRepository> roleClaimRepository,
        LazyService<ITenantRepository> tenantRepository,
        LazyService<IUserTenantRepository> userTenantRepository,
        LazyService<IUserRoleRepository> userRoleRepository,
        IConfiguration configuration, 
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _userClaimRepository = userClaimRepository;
        _roleClaimRepository = roleClaimRepository;
        _tenantRepository = tenantRepository;
        _userTenantRepository = userTenantRepository;
        _userRoleRepository = userRoleRepository;
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Validate tenant exists
        var tenant = await _tenantRepository.Value.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return AuthenticationResult.Failed("Invalid tenant");
        }

        // Find user by email (global lookup)
        var user = await _userRepository.Value.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            return AuthenticationResult.Failed("Invalid credentials");
        }

        // Verify user is a member of this tenant
        var isMember = await _userTenantRepository.Value.IsUserMemberOfTenantAsync(user.Id, tenantId, cancellationToken);
        if (!isMember)
        {
            return AuthenticationResult.Failed("Invalid credentials");
        }

        // Check if user is locked out
        if (user is { IsGloballyLocked: true, GlobalLockoutEnd: not null } && user.GlobalLockoutEnd > _dateTimeProvider.CurrentUtcTime)
        {
            return AuthenticationResult.LockedOut();
        }

        // Validate password
        var isValidPassword = await ValidatePasswordAsync(password, user.PasswordHash);
        if (!isValidPassword)
        {
            // Increment failed attempts
            user.GlobalAccessFailedCount++;
            await _userRepository.Value.SetAccessFailedCountAsync(user.Id, user.GlobalAccessFailedCount, cancellationToken);

            // Lock account if too many failed attempts
            if (user.GlobalAccessFailedCount < 5)
                return AuthenticationResult.Failed("Invalid credentials"); // Configure this value
            var lockoutEnd = _dateTimeProvider.CurrentUtcTime.AddMinutes(30); // Configure lockout duration
            await _userRepository.Value.SetLockoutEndAsync(user.Id, lockoutEnd, cancellationToken);

            return AuthenticationResult.Failed("Invalid credentials");
        }

        // Check email confirmation
        if (!user.IsEmailConfirmed)
        {
            return AuthenticationResult.EmailNotConfirmed();
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

        // Get user claims for this tenant
        var claims = await GetUserClaimsForTenantAsync(user.Id, tenantId, cancellationToken);

        // Generate JWT token
        var token = await GenerateJwtTokenAsync(user.Id, tenantId, claims, cancellationToken);

        return AuthenticationResult.Success(token, user.Id, _jwtOptions.AccessTokenExpiryMinutes);
    }

    public Task<string> GenerateJwtTokenAsync(Guid userId, Guid tenantId, IEnumerable<Claim> claims,
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
            new("tenant_id", tenantId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        tokenClaims.AddRange(claims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(tokenClaims),
            Expires = _dateTimeProvider.CurrentUtcTime.AddHours(_jwtOptions.AccessTokenExpiryMinutes),
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

    public Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
    {
        try
        {
            // Parse the stored hash (assuming format: salt$hash)
            var parts = hashedPassword.Split('$');
            if (parts.Length != 2)
                return Task.FromResult(false);

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            // Hash the provided password with the same salt
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 8,
                Iterations = 4,
                MemorySize = 1024 * 1024 // 1 GB
            };

            var computedHash = argon2.GetBytes(32);
            return Task.FromResult(computedHash.SequenceEqual(hash));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> HashPasswordAsync(string password)
    {
        // Generate a random salt
        var salt = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        // Hash the password
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 8,
            Iterations = 4,
            MemorySize = 1024 * 1024 // 1 GB
        };

        var hash = argon2.GetBytes(32);

        // Combine salt and hash
        var combined = Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        return Task.FromResult(combined);
    }

    // New multi-tenant authentication methods
    
    public async Task<InitialAuthenticationResult> AuthenticateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        // Find user by email (global lookup)
        var user = await _userRepository.Value.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            return InitialAuthenticationResult.Failed("Invalid credentials");
        }

        // Check if user is locked out
        if (user is { IsGloballyLocked: true, GlobalLockoutEnd: not null } && user.GlobalLockoutEnd > _dateTimeProvider.CurrentUtcTime)
        {
            return InitialAuthenticationResult.LockedOut();
        }

        // Validate password
        var isValidPassword = await ValidatePasswordAsync(password, user.PasswordHash);
        if (!isValidPassword)
        {
            // Increment failed attempts
            user.GlobalAccessFailedCount++;
            await _userRepository.Value.SetAccessFailedCountAsync(user.Id, user.GlobalAccessFailedCount, cancellationToken);

            // Lock account if too many failed attempts
            if (user.GlobalAccessFailedCount >= 5) // Configure this value
            {
                var lockoutEnd = _dateTimeProvider.CurrentUtcTime.AddMinutes(30); // Configure lockout duration
                await _userRepository.Value.SetLockoutEndAsync(user.Id, lockoutEnd, cancellationToken);
            }

            return InitialAuthenticationResult.Failed("Invalid credentials");
        }

        // Check email confirmation
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
        var accessibleTenants = await _userTenantRepository.Value.GetUserAccessibleTenantsAsync(user.Id, cancellationToken);
        
        var tenantInfos = new List<TenantInfo>();
        foreach (var tenant in accessibleTenants)
        {
            // Get user roles for this tenant
            var userRoles = await _userRoleRepository.Value.GetRoleNamesForUserAsync(user.Id, tenant.Id, cancellationToken);
            
            tenantInfos.Add(new TenantInfo
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                LogoUrl = tenant.LogoUrl,
                UserRoles = userRoles
            });
        }

        return InitialAuthenticationResult.Success(user.Id, tenantInfos);
    }

    public async Task<TenantAuthenticationResult> AuthenticateWithTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Verify user exists
        var user = await _userRepository.Value.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return TenantAuthenticationResult.Failed("User not found");
        }

        // Verify tenant exists
        var tenant = await _tenantRepository.Value.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return TenantAuthenticationResult.Failed("Tenant not found");
        }

        // Verify user is a member of this tenant
        var isMember = await _userTenantRepository.Value.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);
        if (!isMember)
        {
            return TenantAuthenticationResult.Failed("User is not a member of this tenant");
        }

        // Get user claims for this tenant
        var claims = await GetUserClaimsForTenantAsync(userId, tenantId, cancellationToken);

        // Generate JWT token
        var token = await GenerateJwtTokenAsync(userId, tenantId, claims, cancellationToken);

        // Get user roles for this tenant
        var roles = await _userRoleRepository.Value.GetRoleNamesForUserAsync(userId, tenantId, cancellationToken);

        // Get permissions from claims
        var permissions = claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        var tenantInfo = new TenantInfo
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            LogoUrl = tenant.LogoUrl,
            UserRoles = roles
        };

        return TenantAuthenticationResult.Success(
            token, 
            userId, 
            tenantId, 
            _jwtOptions.AccessTokenExpiryMinutes, 
            tenantInfo, 
            roles, 
            permissions);
    }

    public async Task<TenantAuthenticationResult> SwitchTenantAsync(Guid userId, Guid newTenantId, CancellationToken cancellationToken = default)
    {
        // Use the same logic as AuthenticateWithTenantAsync since switching is essentially re-authenticating with a new tenant
        return await AuthenticateWithTenantAsync(userId, newTenantId, cancellationToken);
    }

    public async Task<IEnumerable<Claim>> GetUserClaimsForTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>();

        // Get direct user claims for this tenant
        var userClaims = await _userClaimRepository.Value.GetClaimsForUserAsync(userId, tenantId, cancellationToken);

        // Get role-based claims for this tenant
        var roleClaims = await _roleClaimRepository.Value.GetClaimsForUserRolesAsync(userId, tenantId, cancellationToken);

        claims.AddRange(userClaims);
        claims.AddRange(roleClaims);

        return claims;
    }
}