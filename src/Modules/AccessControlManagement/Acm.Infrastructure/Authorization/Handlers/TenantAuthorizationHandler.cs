using Microsoft.AspNetCore.Authorization;

namespace Acm.Infrastructure.Authorization.Handlers;

public class TenantRequirement : IAuthorizationRequirement
{
}

public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TenantRequirement requirement)
    {
        // Check if user has a valid tenant claim
        var tenantIdClaim = context.User.FindFirst("tenant_id");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out _))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
