using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Acm.Application.Options;
using Acm.Application.Repositories;
using Acm.Application.Services.Interfaces;
using Common.Application.Providers;
using Common.Application.Services;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Acm.Infrastructure.Services;

public class CryptographyService : ICryptographyService
{
    private readonly LazyService<IUserRepository> _userRepository;
    private readonly LazyService<IUserClaimRepository> _userClaimRepository;
    private readonly LazyService<IRoleClaimRepository> _roleClaimRepository;
    private readonly LazyService<ITenantRepository> _tenantRepository;
    private readonly LazyService<IUserTenantRepository> _userTenantRepository;
    private readonly LazyService<IUserRoleRepository> _userRoleRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtOptions _jwtOptions;

    public CryptographyService(
        LazyService<IUserRepository> userRepository,
        LazyService<IUserClaimRepository> userClaimRepository,
        LazyService<IRoleClaimRepository> roleClaimRepository,
        LazyService<ITenantRepository> tenantRepository,
        LazyService<IUserTenantRepository> userTenantRepository,
        LazyService<IUserRoleRepository> userRoleRepository,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _userClaimRepository = userClaimRepository;
        _roleClaimRepository = roleClaimRepository;
        _tenantRepository = tenantRepository;
        _userTenantRepository = userTenantRepository;
        _userRoleRepository = userRoleRepository;
        _dateTimeProvider = dateTimeProvider;
        _jwtOptions = jwtOptions.Value;
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

    public async Task<List<Claim>> GetUserClaimsForTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>();

        // Get direct user claims for this tenant
        var userClaims = _userClaimRepository.Value.GetClaimsForUserAsync(userId, tenantId, cancellationToken);

        // Get role-based claims for this tenant
        var roleClaims = _roleClaimRepository.Value.GetClaimsForUserRolesAsync(userId, tenantId, cancellationToken);
        
        await Task.WhenAll(userClaims, roleClaims);

        claims.AddRange(userClaims.Result);
        claims.AddRange(roleClaims.Result);

        return claims;
    }

    public Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
    {
        try
        {
            var parts = hashedPassword.Split('$');
            if (parts.Length != 2)
                return Task.FromResult(false);

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);
            
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
}