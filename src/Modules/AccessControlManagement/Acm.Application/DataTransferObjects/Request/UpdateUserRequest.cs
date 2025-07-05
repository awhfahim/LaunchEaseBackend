using System.ComponentModel.DataAnnotations;

namespace Acm.Application.DataTransferObjects.Request;

public record UpdateUserRequest
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