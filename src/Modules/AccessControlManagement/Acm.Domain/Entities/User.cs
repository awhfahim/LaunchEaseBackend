using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public class User : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required string Email { get; set; } // Global email - unique across system
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string PasswordHash { get; set; }
    public required string SecurityStamp { get; set; }

    public bool IsEmailConfirmed { get; set; }
    public bool IsGloballyLocked { get; set; } // Global lockout across all tenants
    public DateTime? GlobalLockoutEnd { get; set; }
    public int GlobalAccessFailedCount { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public string? PhoneNumber { get; set; }
    public bool IsPhoneNumberConfirmed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

