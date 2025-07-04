using Microsoft.AspNetCore.Authorization;

namespace Common.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base($"Permission.{permission}")
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
