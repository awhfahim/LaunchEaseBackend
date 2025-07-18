using System.Data;
using System.Globalization;
using Acm.Application.DataTransferObjects;
using Acm.Application.DataTransferObjects.Request;
using Acm.Application.DataTransferObjects.Response;
using Acm.Application.Interfaces;
using Acm.Application.Repositories;
using Acm.Application.Services.Interfaces;
using Acm.Domain.Entities;
using Common.Application.Misc;
using Common.Application.Providers;
using Common.Application.Services;
using Microsoft.Extensions.Logging;

namespace Acm.Application.Services.Implementations;

public class UserService : IUserService
{
    private readonly IAcmUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IGuidProvider _guidProvider;
    private readonly LazyService<IDateTimeProvider> _dateTimeProvider;
    private readonly ICryptographyService _cryptographyService;
    private readonly LazyService<IUserRoleRepository> _userRoleRepository;
    private readonly LazyService<IUserClaimRepository> _userClaimRepository;
    private readonly LazyService<IRoleClaimRepository> _roleClaimRepository;

    public UserService(IAcmUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IGuidProvider guidProvider,
        LazyService<IDateTimeProvider> dateTimeProvider, ICryptographyService cryptographyService,
        LazyService<IUserRoleRepository> userRoleRepository, LazyService<IUserClaimRepository> userClaimRepository,
        LazyService<IRoleClaimRepository> roleClaimRepository)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _guidProvider = guidProvider;
        _dateTimeProvider = dateTimeProvider;
        _cryptographyService = cryptographyService;
        _userRoleRepository = userRoleRepository;
        _userClaimRepository = userClaimRepository;
        _roleClaimRepository = roleClaimRepository;
    }

    public async Task<Result<User>> CreateUserWithTenantAsync(CreateUserRequest request, Guid tenantId, Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var hashedPassword = await _cryptographyService.HashPasswordAsync(request.Password);

            var user = new User
            {
                Id = _guidProvider.SortableGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = hashedPassword,
                SecurityStamp = _dateTimeProvider.Value.CurrentUtcTime.ToString(CultureInfo.InvariantCulture),
                PhoneNumber = request.PhoneNumber,
                IsEmailConfirmed = false,
                CreatedAt = _dateTimeProvider.Value.CurrentUtcTime
            };

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
        return await _unitOfWork.ExecuteInTransactionAsync<Result<bool>>(async (connection, transaction) =>
        {
            var result =
                await UserExistsAndIsMemberOfTenantAsync(userId, tenantId, cancellationToken: cancellationToken);

            if (!result.IsSuccess)
            {
                return result;
            }

            await _unitOfWork.UserRoles.DeleteByUserIdAsync(userId, tenantId, connection, transaction);

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

                roles.Add(userRole);
            }

            await _unitOfWork.UserRoles.CreateRangeAsync(roles, connection, transaction);
            return true;
        });
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

        var userTenantExists = await _unitOfWork.UserTenants.GetUserTenantAsync(user.Id, tenantId, cancellationToken);

        if (userTenantExists is not null && !userTenantExists.IsActive)
        {
            userTenantExists.IsActive = true;
            userTenantExists.JoinedAt = _dateTimeProvider.Value.CurrentUtcTime;
            return await _unitOfWork.UserTenants.UpdateUserTenantAsync(userTenantExists, cancellationToken);
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

    public Task<IEnumerable<string>> GetExistingEmailsAsync(string email, CancellationToken ct)
    {
        return _unitOfWork.Users.GetExistingEmailsAsync(email, ct);
    }

    public async Task<Result<UserInfoDto>> GetRefreshTokenAsync(Guid userId, Guid tenantId,
        CancellationToken cancellationToken)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result<UserInfoDto>.Failure("User not found", ErrorType.Unauthorized);
        }

        var roles =  _userRoleRepository.Value.GetRoleNamesForUserAsync(user.Id, cancellationToken);
        var userClaims =  _userClaimRepository.Value.GetClaimsForUserAsync(user.Id, cancellationToken);
        var roleClaims =  _roleClaimRepository.Value.GetClaimsForUserRolesAsync(user.Id, cancellationToken);
        
        await Task.WhenAll(roles, userClaims, roleClaims);

        var permissions = userClaims.Result.Where(c => c.Type == "permission")
            .Union(roleClaims.Result.Where(c => c.Type == "permission"))
            .Select(c => c.Value)
            .Distinct()
            .ToList();
        
        return new UserInfoDto
        {
            Id = userId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            TenantId = tenantId,
            Roles = roles.Result.ToList(),
            Permissions = permissions,
        }; 
    }

    public async Task<Result<UserInfoDto>> GetUserInfoAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result<UserInfoDto>.Failure("User not found", ErrorType.Unauthorized);
        }
        
        var roles = _userRoleRepository.Value.GetRoleNamesForUserAsync(userId, tenantId, cancellationToken);
        var userClaims = _userClaimRepository.Value.GetClaimsForUserAsync(userId, tenantId, cancellationToken);
        var roleClaims = _roleClaimRepository.Value.GetClaimsForUserRolesAsync(userId, tenantId, cancellationToken);

        await Task.WhenAll(roles, userClaims, roleClaims);

        var permissions = userClaims.Result.Where(c => c.Type == "permission")
            .Union(roleClaims.Result.Where(c => c.Type == "permission"))
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        var userInfo = new UserInfoDto()
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            TenantId = tenantId,
            Roles = roles.Result.ToList(),
            Permissions = permissions
        };

        return userInfo;
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