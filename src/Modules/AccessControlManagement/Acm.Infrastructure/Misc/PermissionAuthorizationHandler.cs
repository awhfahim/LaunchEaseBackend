using System.Security.Claims;
using Acm.Application.Features.AccessControlFeatures.Interfaces;
using Common.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Acm.Infrastructure.Misc;

public class PermissionAuthorizationHandler 
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public PermissionAuthorizationHandler(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    // protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
    //     PermissionRequirement requirement)
    // {
    //     var roles = context.User.FindAll(c => c.Type == ClaimTypes.Role).Select(x => x.Value).ToList();
    //
    //     if (roles.Count == 0)
    //     {
    //         return;
    //     }
    //
    //     await using var scope = _serviceScopeFactory.CreateAsyncScope();
    //
    //     var authPermissionService = scope.ServiceProvider.GetRequiredService<IRoleAndPermissionService>();
    //
    //     var cacheResult = await authPermissionService.GetPermissionsByRolesFromCacheAsync(roles);
    //
    //     if (CheckData(cacheResult.Data, requirement.Permission, MatchablePermission.SuperAdmin.ToString()))
    //     {
    //         context.Succeed(requirement);
    //     }
    //     else
    //     {
    //         var dbResult = await authPermissionService.GetPermissionsByRolesAndUpdateCacheAsync(roles);
    //         if (CheckData(dbResult, requirement.Permission, MatchablePermission.SuperAdmin.ToString()))
    //         {
    //             context.Succeed(requirement);
    //         }
    //     }
    // }

    private static bool CheckData(Dictionary<string, string[]> toCheck, string target, string fallback)
    {
        return toCheck.Values.Any(elem => elem.Contains(target) || elem.Contains(fallback));
    }
}
