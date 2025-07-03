using System.Security.Claims;

namespace Acm.Application.Services;

public interface IAuthenticationService
{
    // Step 1: Initial login without tenant context
    Task<InitialAuthenticationResult> AuthenticateUserAsync(string email, string password, CancellationToken cancellationToken = default);
    
    // Step 2: Select tenant and get tenant-specific token
    Task<TenantAuthenticationResult> AuthenticateWithTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    
    // Generate tenant-specific JWT
    Task<string> GenerateJwtTokenAsync(Guid userId, Guid tenantId, IEnumerable<Claim> claims, CancellationToken cancellationToken = default);
    
    // Get user claims for specific tenant
    Task<List<Claim>> GetUserClaimsForTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    
    // Utility methods
    Task<bool> ValidatePasswordAsync(string password, string hashedPassword);
    Task<string> HashPasswordAsync(string password);
    
    // Tenant switching for already authenticated users
    Task<TenantAuthenticationResult> SwitchTenantAsync(Guid userId, Guid newTenantId, CancellationToken cancellationToken = default);
    
    // Legacy methods for backward compatibility
    Task<IEnumerable<Claim>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class InitialAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public bool IsLockedOut { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
    public IEnumerable<TenantInfo> AccessibleTenants { get; set; } = new List<TenantInfo>();

    public string TempToken { get; set; } = string.Empty;
    
    public static InitialAuthenticationResult Success(Guid userId, IEnumerable<TenantInfo> tenants, string token)
    {
        return new InitialAuthenticationResult
        {
            IsSuccess = true,
            UserId = userId,
            AccessibleTenants = tenants,
            TempToken = token
        };
    }
    
    public static InitialAuthenticationResult Failed(string errorMessage)
    {
        return new InitialAuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
    
    public static InitialAuthenticationResult LockedOut()
    {
        return new InitialAuthenticationResult
        {
            IsSuccess = false,
            IsLockedOut = true,
            ErrorMessage = "Account is locked out"
        };
    }
    
    public static InitialAuthenticationResult EmailNotConfirmed()
    {
        return new InitialAuthenticationResult
        {
            IsSuccess = false,
            RequiresEmailConfirmation = true,
            ErrorMessage = "Email confirmation required"
        };
    }
}

public class TenantAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public uint ExpiresIn { get; set; }
    public TenantInfo? TenantInfo { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public IEnumerable<string> Permissions { get; set; } = new List<string>();
    
    public static TenantAuthenticationResult Success(string token, Guid userId, Guid tenantId, uint expiresIn, TenantInfo tenantInfo, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        return new TenantAuthenticationResult
        {
            IsSuccess = true,
            Token = token,
            UserId = userId,
            TenantId = tenantId,
            ExpiresIn = expiresIn,
            TenantInfo = tenantInfo,
            Roles = roles,
            Permissions = permissions
        };
    }
    
    public static TenantAuthenticationResult Failed(string errorMessage)
    {
        return new TenantAuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}

public class TenantInfo
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoUrl { get; set; }
    public IEnumerable<string> UserRoles { get; set; } = new List<string>();
}

// Keep the old classes for backward compatibility if needed
public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? Token { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public bool IsLockedOut { get; set; }
    public bool RequiresEmailConfirmation { get; set; }
    public uint ExpiresIn { get; set; }
    
    public static AuthenticationResult Success(string token, Guid userId, uint expiresIn)
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            Token = token,
            UserId = userId,
            ExpiresIn = expiresIn
        };
    }
    
    public static AuthenticationResult Failed(string errorMessage)
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
    
    public static AuthenticationResult LockedOut()
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            IsLockedOut = true,
            ErrorMessage = "Account is locked out"
        };
    }
    
    public static AuthenticationResult EmailNotConfirmed()
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            RequiresEmailConfirmation = true,
            ErrorMessage = "Email confirmation required"
        };
    }
}
