namespace Acm.Api.DTOs.Responses;
public class RoleResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime CreatedAt { get; set; }
    public ICollection<string> Permissions { get; set; } = [];
}

public class InitialLoginResponse
{
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FullName { get; set; }
    public required ICollection<TenantInfoResponse> AccessibleTenants { get; set; }
}

public class TenantInfoResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoUrl { get; set; }
    public required ICollection<string> UserRoles { get; set; }
}

public class TenantLoginResponse
{
    public required string AccessToken { get; set; } // For backward compatibility, will contain a message about cookie
    public required string TokenType { get; set; } = "Bearer";
    public required int ExpiresIn { get; set; } // in seconds
    public required UserTenantInfo User { get; set; }
    public required TenantInfoResponse Tenant { get; set; }
    public bool TokenStoredInCookie { get; set; } = true; // Indicates token is in HTTP-only cookie
}

public class UserTenantInfo
{
    public required Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FullName { get; set; }
    public required Guid TenantId { get; set; }
    public required ICollection<string> Roles { get; set; }
    public required ICollection<string> Permissions { get; set; }
}
