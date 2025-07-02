using Acm.Application.Services;
using Acm.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Konscious.Security.Cryptography;

namespace Acm.Infrastructure.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserClaimRepository _userClaimRepository;
    private readonly IRoleClaimRepository _roleClaimRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IConfiguration _configuration;

    public AuthenticationService(
        IUserRepository userRepository,
        IUserClaimRepository userClaimRepository,
        IRoleClaimRepository roleClaimRepository,
        ITenantRepository tenantRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _userClaimRepository = userClaimRepository;
        _roleClaimRepository = roleClaimRepository;
        _tenantRepository = tenantRepository;
        _configuration = configuration;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password, Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Validate tenant exists
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            return AuthenticationResult.Failed("Invalid tenant");
        }

        // Find user
        var user = await _userRepository.GetByEmailAsync(email, tenantId, cancellationToken);
        if (user == null)
        {
            return AuthenticationResult.Failed("Invalid credentials");
        }

        // Check if user is locked out
        if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
        {
            return AuthenticationResult.LockedOut();
        }

        // Validate password
        var isValidPassword = await ValidatePasswordAsync(password, user.PasswordHash);
        if (!isValidPassword)
        {
            // Increment failed attempts
            user.AccessFailedCount++;
            await _userRepository.SetAccessFailedCountAsync(user.Id, user.AccessFailedCount, cancellationToken);

            // Lock account if too many failed attempts
            if (user.AccessFailedCount >= 5) // Configure this value
            {
                var lockoutEnd = DateTime.UtcNow.AddMinutes(30); // Configure lockout duration
                await _userRepository.SetLockoutEndAsync(user.Id, lockoutEnd, cancellationToken);
            }

            return AuthenticationResult.Failed("Invalid credentials");
        }

        // Check email confirmation
        if (!user.IsEmailConfirmed)
        {
            return AuthenticationResult.EmailNotConfirmed();
        }

        // Reset failed attempts on successful login
        if (user.AccessFailedCount > 0)
        {
            await _userRepository.SetAccessFailedCountAsync(user.Id, 0, cancellationToken);
        }

        // Clear lockout
        if (user.IsLocked)
        {
            await _userRepository.SetLockoutEndAsync(user.Id, null, cancellationToken);
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Get user claims
        var claims = await GetUserClaimsAsync(user.Id, cancellationToken);

        // Generate JWT token
        var token = await GenerateJwtTokenAsync(user.Id, tenantId, claims, cancellationToken);

        return AuthenticationResult.Success(token, user.Id);
    }

    public async Task<string> GenerateJwtTokenAsync(Guid userId, Guid tenantId, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT key not configured"));
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("tenant_id", tenantId.ToString()),
            new("user_id", userId.ToString())
        };

        tokenClaims.AddRange(claims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(tokenClaims),
            Expires = DateTime.UtcNow.AddHours(8), // Configure token expiration
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<IEnumerable<Claim>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>();

        // Get direct user claims
        var userClaims = await _userClaimRepository.GetClaimsForUserAsync(userId, cancellationToken);
        claims.AddRange(userClaims);

        // Get role-based claims
        var roleClaims = await _roleClaimRepository.GetClaimsForUserRolesAsync(userId, cancellationToken);
        claims.AddRange(roleClaims);

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
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

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
