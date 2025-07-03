using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public class UserClaim : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; set; }
    public required Guid TenantId { get; set; } // Tenant-specific claims
    public required string ClaimType { get; set; }         // e.g., "permission"
    public required string ClaimValue { get; set; }        // e.g., "Dashboard.View"
}

