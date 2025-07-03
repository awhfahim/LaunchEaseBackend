using System.ComponentModel.DataAnnotations;

namespace Acm.Api.DTOs.Requests;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
    
    public bool RememberMe { get; set; }
}

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(50)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    public required string LastName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; set; }

    public string? PhoneNumber { get; set; }

    public ICollection<string> Roles { get; set; } = new List<string>();
}

public class UpdateUserRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(50)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    public required string LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsEmailConfirmed { get; set; }
    public bool IsLocked { get; set; }
}

public class CreateRoleRequest
{
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public ICollection<string> Permissions { get; set; } = new List<string>();
}

public class UpdateRoleRequest
{
    [Required]
    [StringLength(50)]
    public required string Name { get; set; }

    public string? Description { get; set; }
}

public class AssignPermissionsRequest
{
    public required ICollection<string> Permissions { get; set; }
}

public class InitialLoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}

public class TenantLoginRequest
{
    [Required]
    public required Guid UserId { get; set; }

    [Required]
    public required Guid TenantId { get; set; }
}

public class TenantSwitchRequest
{
    [Required]
    public required Guid TenantId { get; set; }
}

public class InviteUserToTenantRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(50)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(50)]
    public required string LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public ICollection<string> Roles { get; set; } = new List<string>();

    public bool SendInvitationEmail { get; set; } = true;
}

public class InviteUserRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    public ICollection<string> Roles { get; set; } = new List<string>();
}
