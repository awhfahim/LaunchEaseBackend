using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public class RoleClaim : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required Guid RoleId { get; set; }
    public required string ClaimType { get; set; }
    public required string ClaimValue { get; set; }
}

