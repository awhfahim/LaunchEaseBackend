using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public class Tenant : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }                // Business name
    public required string Slug { get; set; }                // Used for subdomain (e.g., acme.yoursaas.com)
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
