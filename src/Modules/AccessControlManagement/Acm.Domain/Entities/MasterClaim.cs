namespace Acm.Domain.Entities;

public sealed class MasterClaim
{
    public Guid Id { get; set; }
    public string ClaimType { get; set; } = "permission";
    public string ClaimValue { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public bool IsTenantScoped { get; set; } = true;
    public bool IsSystemPermission { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}