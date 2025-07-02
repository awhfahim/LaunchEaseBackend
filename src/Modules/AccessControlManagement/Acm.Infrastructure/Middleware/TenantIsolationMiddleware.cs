using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Acm.Infrastructure.Middleware;

public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply tenant isolation to authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = GetTenantIdFromUser(context.User);
            if (tenantId.HasValue)
            {
                // Store tenant ID in HTTP context for use in controllers/services
                context.Items["TenantId"] = tenantId.Value;
                
                // Add tenant ID to response headers for debugging (optional)
                context.Response.Headers.Add("X-Tenant-Id", tenantId.Value.ToString());
            }
            else
            {
                _logger.LogWarning("Authenticated user without valid tenant ID");
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Invalid tenant context");
                return;
            }
        }

        await _next(context);
    }

    private static Guid? GetTenantIdFromUser(System.Security.Claims.ClaimsPrincipal user)
    {
        var tenantIdClaim = user.FindFirst("tenant_id");
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }
}

public static class TenantIsolationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantIsolation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantIsolationMiddleware>();
    }
}
