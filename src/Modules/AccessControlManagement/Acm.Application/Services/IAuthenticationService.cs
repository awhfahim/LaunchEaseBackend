using System.Security.Claims;

namespace Acm.Application.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(string email, string password, Guid tenantId, CancellationToken cancellationToken = default);
    Task<string> GenerateJwtTokenAsync(Guid userId, Guid tenantId, IEnumerable<Claim> claims, CancellationToken cancellationToken = default);
    Task<IEnumerable<Claim>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidatePasswordAsync(string password, string hashedPassword);
    Task<string> HashPasswordAsync(string password);
}

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
