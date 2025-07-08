using Acm.Domain.Entities;

namespace Acm.Application.DataTransferObjects;

public class UserWithTenantsDto
{
    public User User { get; set; } = null!;
    public List<Tenant> Tenants { get; set; } = new();
    public List<UserTenant> UserTenantRelationships { get; set; } = new();
}