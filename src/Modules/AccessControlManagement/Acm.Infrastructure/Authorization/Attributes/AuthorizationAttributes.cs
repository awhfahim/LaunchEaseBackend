using Microsoft.AspNetCore.Authorization;

namespace Acm.Infrastructure.Authorization.Attributes;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base($"Permission.{permission}")
    {
    }
}

public class RequireTenantAttribute : AuthorizeAttribute
{
    public RequireTenantAttribute()
        : base("TenantPolicy")
    {
    }
}
