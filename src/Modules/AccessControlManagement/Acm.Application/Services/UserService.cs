using System.Data;
using Acm.Application.DataTransferObjects.Request;
using Acm.Application.DataTransferObjects.Response;
using Acm.Application.Interfaces;
using Acm.Domain.Entities;
using Common.Application.Misc;
using Common.Application.Providers;
using Common.Application.Services;
using Microsoft.Extensions.Logging;

namespace Acm.Application.Services;

public class UserService : IUserService
{
    private readonly IAcmUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IGuidProvider _guidProvider;
    private readonly LazyService<IDateTimeProvider> _dateTimeProvider;

    public UserService(IAcmUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IGuidProvider guidProvider,
        LazyService<IDateTimeProvider> dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _guidProvider = guidProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<User>> CreateUserWithTenantAsync(User user, Guid tenantId, Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var emailExists =
                await _unitOfWork.Users.EmailExistsAsync(user.Email, connection, transaction,
                    cancellationToken);
            if (emailExists)
            {
                return Result<User>.Failure("Email already exists", ErrorType.Conflict);
            }

            var userId = await _unitOfWork.Users.InsertAsync(user, connection, transaction,
                cancellationToken);

            var userTenant = new UserTenant
            {
                Id = _guidProvider.SortableGuid(),
                UserId = userId,
                TenantId = tenantId,
                IsActive = true,
                JoinedAt = _dateTimeProvider.Value.CurrentUtcTime,
                InvitedBy = null
            };
            await _unitOfWork.UserTenants.AddUserToTenantAsync(userTenant, connection, transaction,
                cancellationToken);

            var userRole = new UserRole
            {
                Id = _guidProvider.SortableGuid(),
                UserId = userId,
                RoleId = roleId,
                TenantId = tenantId
            };

            await _unitOfWork.UserRoles.CreateAsync(userRole, connection, transaction);
            return user;
        }, cancellationToken: cancellationToken);
    }

    public async Task UpdateUserSecurityInfoAsync(Guid userId, string newPasswordHash, string newSecurityStamp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                _logger.LogInformation("Updating security information for user {UserId}", userId);

                // 1. Check if user exists
                var user = await _unitOfWork.Users.GetByIdAsync(userId, connection, transaction,
                    cancellationToken);
                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {userId} not found");
                }

                // 2. Update password hash
                await _unitOfWork.Users.SetPasswordHashAsync(userId, newPasswordHash, connection, transaction,
                    cancellationToken);

                // 3. Update security stamp
                await _unitOfWork.Users.SetSecurityStampAsync(userId, newSecurityStamp, connection, transaction,
                    cancellationToken);

                // 4. Reset access failed count
                await _unitOfWork.Users.SetAccessFailedCountAsync(userId, 0, connection, transaction,
                    cancellationToken);

                _logger.LogInformation("Security information updated successfully for user {UserId}", userId);
            }, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update security information for user {UserId}", userId);
            throw;
        }
    }


    public async Task<UserWithTenantsDto> GetUserWithTenantsAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            var tenants = await _unitOfWork.UserTenants.GetUserAccessibleTenantsAsync(userId, cancellationToken);
            var userTenants = await _unitOfWork.UserTenants.GetUserTenantsAsync(userId, cancellationToken);

            return new UserWithTenantsDto
            {
                User = user,
                Tenants = tenants.ToList(),
                UserTenantRelationships = userTenants.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user {UserId} with tenants", userId);
            throw;
        }
    }


    public async Task BulkCreateUsersAsync(IEnumerable<User> users, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var userList = users.ToList();
        _logger.LogInformation("Starting bulk creation of {UserCount} users", userList.Count);

        // Manual transaction control for more complex scenarios
        await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            var createdUserIds = new List<Guid>();

            foreach (var user in userList)
            {
                // Check email doesn't exist
                var emailExists = await _unitOfWork.Users.EmailExistsAsync(user.Email,
                    _unitOfWork.Connection,
                    _unitOfWork.Transaction, cancellationToken);
                if (emailExists)
                {
                    _logger.LogWarning("Skipping user {Email} - already exists", user.Email);
                    continue;
                }

                // Create user
                var userId = await _unitOfWork.Users.InsertAsync(user, _unitOfWork.Connection,
                    _unitOfWork.Transaction,
                    cancellationToken);
                createdUserIds.Add(userId);

                // Add to tenant
                var userTenant = new UserTenant
                {
                    Id = _guidProvider.SortableGuid(),
                    UserId = userId,
                    TenantId = tenantId,
                    IsActive = true,
                    JoinedAt = _dateTimeProvider.Value.CurrentUtcTime
                };
                await _unitOfWork.UserTenants.AddUserToTenantAsync(userTenant, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            _logger.LogInformation("Successfully created {CreatedCount} users out of {TotalCount}",
                createdUserIds.Count, userList.Count);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to bulk create users. Transaction rolled back.");
            throw;
        }
    }

    public Task<(int, IEnumerable<User>)> GetUsersByTenantIdAsync(Guid tenantId, int page, int limit,
        string? searchString, CancellationToken cancellationToken)
    {
        return _unitOfWork.Users.GetByTenantIdAsync(tenantId, page, limit, searchString, cancellationToken);
    }

    public async Task<Result<UserResponse>> GetUserAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync<Result<UserResponse>>(async (connection, transaction) =>
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, connection, transaction, cancellationToken);

            if (user == null)
            {
                return Result<UserResponse>.Failure("User not found", ErrorType.NotFound);
            }

            var isMember = await _unitOfWork.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId);

            if (!isMember)
            {
                return Result<UserResponse>.Failure("User is not a member of the specified tenant",
                    ErrorType.Forbidden);
            }

            var userRoles = await _unitOfWork.UserRoles.GetByUserIdAsync(userId, tenantId, connection, transaction,
                cancellationToken);

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.IsEmailConfirmed,
                CreatedAt = user.CreatedAt,
                FullName = user.FullName,
                IsLocked = user.IsGloballyLocked,
                Roles = userRoles.Select(x => new UserRoleResponse
                {
                    RoleId = x.roleId,
                    RoleName = x.roleName
                })
            };

            return userResponse;
        });
    }

    public async Task<Result<User>> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid tenantId,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return Result<User>.Failure("User not found", ErrorType.NotFound);
        }

        var isMember = await _unitOfWork.UserTenants.IsUserMemberOfTenantAsync(id, tenantId, cancellationToken);

        if (!isMember)
        {
            return Result<User>.Failure("User not found in current tenant", ErrorType.NotFound);
        }

        var existingUser = await _unitOfWork.Users.GetByEmailAsync(user.Email, cancellationToken);
        if (existingUser != null && existingUser.Id != id)
        {
            return Result<User>.Failure("Email already exists", ErrorType.Conflict);
        }

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.IsEmailConfirmed = request.IsEmailConfirmed;
        user.IsGloballyLocked = request.IsLocked;
        user.UpdatedAt = _dateTimeProvider.Value.CurrentUtcTime;
        var isUpdated = await _unitOfWork.Users.UpdateAsync(user);

        return !isUpdated ? Result<User>.Failure("Failed to update user", ErrorType.InternalServerError) : user;
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        return _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        return _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        return _unitOfWork.Users.EmailExistsAsync(email, cancellationToken);
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var result =
                await UserExistsAndIsMemberOfTenantAsync(userId, tenantId, cancellationToken: cancellationToken);

            if (!result.IsSuccess)
            {
                return result;
            }

            await _unitOfWork.UserClaims.DeleteByUserIdAsync(userId, tenantId, connection, transaction,
                cancellationToken);
            await _unitOfWork.UserRoles.DeleteByUserIdAsync(userId, tenantId, connection, transaction,
                cancellationToken);
            await _unitOfWork.UserTenants.RemoveUserFromTenantAsync(userId, tenantId, connection, transaction,
                cancellationToken);

            return true;
        }, cancellationToken: cancellationToken);
    }

    public async Task<Result<bool>> AssignRoleToUserAsync(Guid userId, ICollection<Guid> roleIds, Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await UserExistsAndIsMemberOfTenantAsync(userId, tenantId, cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return result;
        }

        ICollection<UserRole> roles = [];
        foreach (var roleId in roleIds)
        {
            var userRole = new UserRole
            {
                Id = _guidProvider.SortableGuid(),
                UserId = userId,
                RoleId = roleId,
                TenantId = tenantId
            };

            var exists = await _unitOfWork.UserRoles.ExistsAsync(userId, roleId, tenantId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("User {UserId} already has role {RoleId} in tenant {TenantId}", userId, roleId,
                    tenantId);
                continue;
            }

            roles.Add(userRole);
        }

        await _unitOfWork.UserRoles.CreateRangeAsync(roles);
        return true;
    }

    public Task<Result<bool>> RemoveRoleFromUserAsync(Guid userId, Guid tenantId, ICollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var result = await UserExistsAndIsMemberOfTenantAsync(userId, tenantId, connection, transaction,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return result;
            }

            await _unitOfWork.UserRoles.DeleteRangeAsync(userId, tenantId, roleIds);

            return true;
        }, cancellationToken: cancellationToken);
    }

    public async Task<Result<bool>> InviteUserAsync(string email, Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure("User not found", ErrorType.NotFound);
        }

        var isMember =
            await _unitOfWork.UserTenants.IsUserMemberOfTenantAsync(user.Id, tenantId, cancellationToken);
        if (isMember)
        {
            return Result<bool>.Failure("User is already a member of the specified tenant", ErrorType.Conflict);
        }

        var userTenant = new UserTenant
        {
            Id = _guidProvider.SortableGuid(),
            UserId = user.Id,
            TenantId = tenantId,
            IsActive = false, // Initially inactive until accepted
            JoinedAt = _dateTimeProvider.Value.CurrentUtcTime,
            InvitedBy = null // Set this to the user who sent the invite if needed
        };

        await _unitOfWork.UserTenants.AddUserToTenantAsync(userTenant, cancellationToken);
        return true;
    }

    public Task<bool> IsUserMemberOfTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return _unitOfWork.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);
    }

    private async Task<Result<bool>> UserExistsAndIsMemberOfTenantAsync(Guid userId, Guid tenantId,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var userExists = await _unitOfWork.Users.ExistsAsync(userId, connection, transaction, cancellationToken);
        if (!userExists)
        {
            return Result<bool>.Failure("User does not exist", ErrorType.NotFound);
        }

        var isMember =
            await _unitOfWork.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);

        return !isMember
            ? Result<bool>.Failure("User is not a member of the specified tenant", ErrorType.Forbidden)
            : true;
    }
}

/// <summary>
/// DTO for user with tenants data
/// </summary>
public class UserWithTenantsDto
{
    public User User { get; set; } = null!;
    public List<Tenant> Tenants { get; set; } = new();
    public List<UserTenant> UserTenantRelationships { get; set; } = new();
}