using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public sealed class UserTenant : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; set; }
    public required Guid TenantId { get; set; }
    public required bool IsActive { get; set; } = true;
    public required DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    public string? InvitedBy { get; set; } // Email of the user who invited this user
}
