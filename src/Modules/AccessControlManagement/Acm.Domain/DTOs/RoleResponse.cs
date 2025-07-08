

namespace Acm.Domain.DTOs;

public record RoleResponse
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Claims { get; set; }
    public ICollection<RoleClaimResponse> Permissions { get; set; } = [];

}

public record RoleClaimResponse
{
    public Guid? ClaimId { get; init; }
    public string? Claim { get; init; }
}