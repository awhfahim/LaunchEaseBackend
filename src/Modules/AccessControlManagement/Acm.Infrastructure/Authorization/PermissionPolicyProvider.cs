using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Acm.Infrastructure.Authorization;


public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith("Permission.", StringComparison.OrdinalIgnoreCase))
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        var permissionString = policyName["Permission.".Length..];
        
        if (!permissionString.Contains(',')) return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permissionString))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}