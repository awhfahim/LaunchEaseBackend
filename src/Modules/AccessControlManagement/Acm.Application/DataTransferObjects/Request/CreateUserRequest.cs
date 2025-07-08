using System.ComponentModel.DataAnnotations;

namespace Acm.Application.DataTransferObjects.Request;

public record CreateUserRequest
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

    public Guid RoleId { get; set; }
}