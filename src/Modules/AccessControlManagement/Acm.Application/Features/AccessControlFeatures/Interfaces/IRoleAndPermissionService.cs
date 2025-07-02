using Common.Domain.DataTransferObjects.Request;
using Common.Domain.DataTransferObjects.Response;
using Common.Domain.Enums;
using SharpOutcome;
using SharpOutcome.Helpers;

namespace Acm.Application.Features.AccessControlFeatures.Interfaces;

public interface IRoleAndPermissionService
{
    Task ClearPermissionCacheOfRoleAsync(string roleLabel);
    Task WarmCacheAfterPermissionUpdateAsync(int roleId, CancellationToken ct);

    // Task<ValueOutcome<Successful, AuthorizableBadOutcome>> DeletePermissionAndUpdateCacheAsync(long id,
    //     CancellationToken ct = default);
    //
    // Task<Dictionary<string, string[]>> GetPermissionsByRolesAndUpdateCacheAsync(ICollection<string> roles,
    //     CancellationToken ct = default);
    //
    // Task<PagedData<AuthorizablePermissionResponse>> GetAllPermissionsAsync(DynamicQueryDto dto,
    //     CancellationToken ct = default);
    //
    // Task<(bool Exists, Dictionary<string, string[]> Data)> GetPermissionsByRolesFromCacheAsync(
    //     ICollection<string> roles);
    //
    // Task<Dictionary<string, string[]>> GetPermissionsByRolesAsync(ICollection<string> roles,
    //     CancellationToken ct = default);
    //
    // Task<AuthorizablePermission?> GetPermissionByLabelAsync(string permission,
    //     CancellationToken ct = default);
    //
    // Task<AuthorizablePermission?> GetPermissionByIdAsync(long id, CancellationToken ct = default);
    //
    // Task<ValueOutcome<Successful, Failed>> UpdatePermissionsInRoleAsync(long userId, int roleId,
    //     HashSet<MatchablePermission> permissions, CancellationToken ct);
    //
    // Task<IReadOnlyCollection<MatchablePermissionTreeNode>> GetTreeDataAsync(int roleId,
    //     MatchablePermission? locator, CancellationToken ct);
    //
    // Task<PagedData<AuthorizableRoleResponse>> GetAllRolesOfUserAsync(long id, DynamicQueryDto dto,
    //     CancellationToken ct);
    //
    // Task<PagedData<AuthorizableRoleResponse>> GetAllRolesAsync(DynamicQueryDto dto, CancellationToken ct);
    //
    // public Task<AuthorizableRole?> GetRoleByLabelAsync(string role, CancellationToken ct);
    //
    // public Task<PagedData<AuthorizableRoleResponse>> GetAssignableRolesAsync(DynamicQueryDto dto,
    //     long userId, CancellationToken ct);
    //
    // public Task<bool> IsRoleExistsAsync(string label, CancellationToken ct);
    // public Task<bool> IsRoleExistsAsync(long id, CancellationToken ct);
    //
    // public Task<ValueOutcome<AuthorizableRole, AuthorizableBadOutcome>> CreateRoleAsync(string role,
    //     long createdByUserId, CancellationToken ct);
    //
    // public Task<ValueOutcome<AuthorizableRole, AuthorizableBadOutcome>> UpdateRoleAsync(int roleId,
    //     string updatedRole, long updatedByUserId, CancellationToken ct);
    //
    // public Task<ValueOutcome<Successful, AuthorizableBadOutcome>> DeleteRoleAsync(int id,
    //     CancellationToken ct);
}
