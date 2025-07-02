using Common.Domain.Interfaces;

namespace Acm.Domain.Entities;

public class UserRole : IBaseEntity<Guid>
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; set; }
    public required Guid RoleId { get; set; }
}

