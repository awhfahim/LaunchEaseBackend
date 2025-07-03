using System.ComponentModel.DataAnnotations;

namespace Acm.Application.DataTransferObjects;

public record RegisterTenantRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [Required]
    [StringLength(50)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug can only contain lowercase letters, numbers, and hyphens")]
    public required string Slug { get; set; }

    [EmailAddress]
    public string? ContactEmail { get; set; }

    [Required]
    [EmailAddress]
    public required string AdminEmail { get; set; }

    [Required]
    [StringLength(50)]
    public required string AdminFirstName { get; set; }

    [Required]
    [StringLength(50)]
    public required string AdminLastName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string AdminPassword { get; set; }
}