using Common.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Common.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(MatchablePermission permission)
        : base(policy: permission.ToString())
    {
    }
}
