using Acm.Application.DataTransferObjects.Request;
using Acm.Application.Interfaces;
using Acm.Domain.Entities;
using Common.Application.Misc;
using Common.Application.Providers;
using Common.Application.Services;
using Microsoft.Extensions.Logging;

namespace Acm.Application.Services;

public class UserService : IUserService
{
    private readonly LazyService<IAcmUnitOfWork> _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IGuidProvider _guidProvider;
    private readonly LazyService<IDateTimeProvider> _dateTimeProvider;

    public UserService(LazyService<IAcmUnitOfWork> unitOfWork,
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
        return await _unitOfWork.Value.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var emailExists =
                await _unitOfWork.Value.Users.EmailExistsAsync(user.Email, connection, transaction,
                    cancellationToken);
            if (emailExists)
            {
                return Result<User>.Failure("Email already exists", ErrorType.Conflict);
            }

            var userId = await _unitOfWork.Value.Users.InsertAsync(user, connection, transaction,
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
            await _unitOfWork.Value.UserTenants.AddUserToTenantAsync(userTenant, connection, transaction,
                cancellationToken);

            var userRole = new UserRole
            {
                Id = _guidProvider.SortableGuid(),
                UserId = userId,
                RoleId = roleId,
                TenantId = tenantId
            };

            await _unitOfWork.Value.UserRoles.CreateAsync(userRole, connection, transaction);
            return user;
        }, cancellationToken: cancellationToken);
    }

    public async Task UpdateUserSecurityInfoAsync(Guid userId, string newPasswordHash, string newSecurityStamp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.Value.ExecuteInTransactionAsync(async (connection, transaction) =>
            {
                _logger.LogInformation("Updating security information for user {UserId}", userId);

                // 1. Check if user exists
                var user = await _unitOfWork.Value.Users.GetByIdAsync(userId, connection, transaction,
                    cancellationToken);
                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {userId} not found");
                }

                // 2. Update password hash
                await _unitOfWork.Value.Users.SetPasswordHashAsync(userId, newPasswordHash, connection, transaction,
                    cancellationToken);

                // 3. Update security stamp
                await _unitOfWork.Value.Users.SetSecurityStampAsync(userId, newSecurityStamp, connection, transaction,
                    cancellationToken);

                // 4. Reset access failed count
                await _unitOfWork.Value.Users.SetAccessFailedCountAsync(userId, 0, connection, transaction,
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
            var user = await _unitOfWork.Value.Users.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            var tenants = await _unitOfWork.Value.UserTenants.GetUserAccessibleTenantsAsync(userId, cancellationToken);
            var userTenants = await _unitOfWork.Value.UserTenants.GetUserTenantsAsync(userId, cancellationToken);

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
        await _unitOfWork.Value.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            var createdUserIds = new List<Guid>();

            foreach (var user in userList)
            {
                // Check email doesn't exist
                var emailExists = await _unitOfWork.Value.Users.EmailExistsAsync(user.Email,
                    _unitOfWork.Value.Connection,
                    _unitOfWork.Value.Transaction, cancellationToken);
                if (emailExists)
                {
                    _logger.LogWarning("Skipping user {Email} - already exists", user.Email);
                    continue;
                }

                // Create user
                var userId = await _unitOfWork.Value.Users.InsertAsync(user, _unitOfWork.Value.Connection,
                    _unitOfWork.Value.Transaction,
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
                await _unitOfWork.Value.UserTenants.AddUserToTenantAsync(userTenant, cancellationToken);
            }

            await _unitOfWork.Value.CommitAsync(cancellationToken);
            _logger.LogInformation("Successfully created {CreatedCount} users out of {TotalCount}",
                createdUserIds.Count, userList.Count);
        }
        catch (Exception ex)
        {
            await _unitOfWork.Value.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to bulk create users. Transaction rolled back.");
            throw;
        }
    }

    public Task<IEnumerable<User>> GetUsersByTenantIdAsync(Guid tenantId, int page, int limit,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.Value.Users.GetByTenantIdAsync(tenantId, page, limit, cancellationToken);
    }

    public async Task<Result<User>> GetUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Value.Users.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return Result<User>.Failure("User not found", ErrorType.NotFound);
        }

        var isMember = await _unitOfWork.Value.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId);

        if (!isMember)
        {
            return Result<User>.Failure("User is not a member of the specified tenant", ErrorType.Forbidden);
        }

        return user;
    }

    public async Task<Result<User>> UpdateUserAsync(Guid id, UpdateUserRequest request, Guid tenantId,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Value.Users.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return Result<User>.Failure("User not found", ErrorType.NotFound);
        }

        var isMember = await _unitOfWork.Value.UserTenants.IsUserMemberOfTenantAsync(id, tenantId, cancellationToken);

        if (!isMember)
        {
            return Result<User>.Failure("User not found in current tenant", ErrorType.NotFound);
        }

        var existingUser = await _unitOfWork.Value.Users.GetByEmailAsync(user.Email, cancellationToken);
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
        var isUpdated = await _unitOfWork.Value.Users.UpdateAsync(user);

        return !isUpdated ? Result<User>.Failure("Failed to update user", ErrorType.InternalServerError) : user;
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(userId));
        }

        return _unitOfWork.Value.Users.GetByIdAsync(userId, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        return _unitOfWork.Value.Users.GetByEmailAsync(email, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        }

        return _unitOfWork.Value.Users.EmailExistsAsync(email, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Value.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var userExists =
                await _unitOfWork.Value.Users.ExistsAsync(userId, connection, transaction, cancellationToken);
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {userId} does not exist");
            }

            var isMember =
                await _unitOfWork.Value.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);

            if (!isMember)
            {
                throw new InvalidOperationException($"User with ID {userId} is not a member of tenant {tenantId}");
            }

            await _unitOfWork.Value.UserClaims.DeleteByUserIdAsync(userId, tenantId, connection, transaction,
                cancellationToken);
            await _unitOfWork.Value.UserRoles.DeleteByUserIdAsync(userId, tenantId, connection, transaction,
                cancellationToken);
            await _unitOfWork.Value.UserTenants.RemoveUserFromTenantAsync(userId, tenantId, connection, transaction,
                cancellationToken);

            return true;
        }, cancellationToken: cancellationToken);
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, ICollection<Guid> roleIds, Guid tenantId,
        CancellationToken cancellationToken)
    {
        var userExists = await _unitOfWork.Value.Users.ExistsAsync(userId, cancellationToken);
        if (!userExists)
        {
            throw new InvalidOperationException($"User with ID {userId} does not exist");
        }

        var isMember =
            await _unitOfWork.Value.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);

        if (!isMember)
        {
            throw new InvalidOperationException($"User with ID {userId} is not a member of tenant {tenantId}");
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

            var exists = await _unitOfWork.Value.UserRoles.ExistsAsync(userId, roleId, tenantId, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("User {UserId} already has role {RoleId} in tenant {TenantId}", userId, roleId,
                    tenantId);
                continue;
            }

            roles.Add(userRole);
        }

        await _unitOfWork.Value.UserRoles.CreateRangeAsync(roles);
        _logger.LogInformation("Assigned {RoleCount} roles to user {UserId} in tenant {TenantId}", roles.Count, userId,
            tenantId);
        return true;
    }

    public Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid tenantId, ICollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.Value.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var userExists =
                await _unitOfWork.Value.Users.ExistsAsync(userId, connection, transaction, cancellationToken);
            if (!userExists)
            {
                throw new InvalidOperationException($"User with ID {userId} does not exist");
            }

            var isMember =
                await _unitOfWork.Value.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);

            if (!isMember)
            {
                throw new InvalidOperationException($"User with ID {userId} is not a member of tenant {tenantId}");
            }

            await _unitOfWork.Value.UserRoles.DeleteRangeAsync(userId, tenantId, roleIds);

            return true;
        }, cancellationToken: cancellationToken);
    }

    public async Task<bool> InviteUserAsync(string email, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Value.Users.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException(
                $"User with this email {email} does not exist. Use CreateUser to create a new user.");
        }

        var isMember =
            await _unitOfWork.Value.UserTenants.IsUserMemberOfTenantAsync(user.Id, tenantId, cancellationToken);
        if (isMember)
        {
            throw new InvalidOperationException($"User with email {email} is already a member of tenant {tenantId}");
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

        await _unitOfWork.Value.UserTenants.AddUserToTenantAsync(userTenant, cancellationToken);
        return true;
    }

    public Task<bool> IsUserMemberOfTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return _unitOfWork.Value.UserTenants.IsUserMemberOfTenantAsync(userId, tenantId, cancellationToken);
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