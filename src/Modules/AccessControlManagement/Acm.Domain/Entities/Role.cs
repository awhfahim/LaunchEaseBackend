using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public class Role : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; set; }
    public required string Name { get; set; }              // e.g., Admin, Editor
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

