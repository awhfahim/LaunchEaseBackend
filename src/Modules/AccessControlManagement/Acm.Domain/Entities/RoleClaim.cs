using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public sealed class RoleClaim : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required Guid RoleId { get; set; }
    public required Guid MasterClaimId { get; set; }
}

