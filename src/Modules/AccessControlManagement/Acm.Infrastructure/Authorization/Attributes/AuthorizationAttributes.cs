using Microsoft.AspNetCore.Authorization;

namespace Acm.Infrastructure.Authorization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(params string[] permissions)
        : base($"Permission.{string.Join(",", permissions)}")
    {
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireTenantAttribute : AuthorizeAttribute
{
    public RequireTenantAttribute()
        : base("TenantPolicy")
    {
    }
}
