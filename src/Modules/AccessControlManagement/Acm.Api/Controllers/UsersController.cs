using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.Repositories;
using Acm.Application.Services;
using Acm.Domain.Entities;
using Acm.Infrastructure.Authorization.Attributes;
using Acm.Infrastructure.Authorization;
using Common.Application.Providers;
using Common.HttpApi.Controllers;
using Common.HttpApi.Others;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Acm.Api.Controllers;

[Route("api/users")]
[Authorize]
[RequireTenant]
public class UsersController : JsonApiControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserClaimRepository _userClaimRepository;
    private readonly IUserTenantRepository _userTenantRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGuidProvider _guidProvider;

    public UsersController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IUserClaimRepository userClaimRepository,
        IUserTenantRepository userTenantRepository,
        IAuthenticationService authenticationService, 
        IDateTimeProvider dateTimeProvider, 
        IGuidProvider guidProvider)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _userClaimRepository = userClaimRepository;
        _userTenantRepository = userTenantRepository;
        _authenticationService = authenticationService;
        _dateTimeProvider = dateTimeProvider;
        _guidProvider = guidProvider;
    }

    [HttpGet]
    [RequirePermission(PermissionConstants.UsersView)]
    public async Task<IActionResult> GetUsers(
        [FromQuery, BindRequired] PaginationQueryParameter pagination)
    {
        try
        {
            var users = await _userRepository.GetByTenantIdAsync(GetTenantId(), pagination.Page, pagination.Limit);

            var responses = new List<UserResponse>();
            foreach (var user in users)
            {
                responses.Add(new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    IsEmailConfirmed = user.IsEmailConfirmed,
                    IsLocked = user.IsGloballyLocked,
                    LockoutEnd = user.GlobalLockoutEnd,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt
                });
            }

            return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResult(responses));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<IEnumerable<UserResponse>>.ErrorResult("An error occurred while fetching users"));
        }
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionConstants.UsersView)]
    public async Task<IActionResult> GetUser([FromRoute] Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            
            var user = await _userRepository.GetByIdAsync(id, HttpContext.RequestAborted);
            if (user == null)
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found"));
            }

            // Check if user is a member of current tenant
            var isMember = await _userTenantRepository.IsUserMemberOfTenantAsync(id, tenantId, HttpContext.RequestAborted);
            if (!isMember)
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found in current tenant"));
            }

            var response = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsLocked = user.IsGloballyLocked,
                LockoutEnd = user.GlobalLockoutEnd,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<UserResponse>.SuccessResult(response));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<UserResponse>.ErrorResult("An error occurred while fetching user"));
        }
    }

    [HttpPost]
    [RequirePermission(PermissionConstants.UsersCreate)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var tenantId = GetTenantId();
            
            // Check if email already exists globally
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, HttpContext.RequestAborted);
            if (existingUser != null)
            {
                // Check if user is already a member of this tenant
                var isMember = await _userTenantRepository.IsUserMemberOfTenantAsync(existingUser.Id, tenantId, HttpContext.RequestAborted);
                if (isMember)
                {
                    return BadRequest(ApiResponse<UserResponse>.ErrorResult("User already exists in this tenant"));
                }
                else
                {
                    return BadRequest(ApiResponse<UserResponse>.ErrorResult("Email already exists. Use invite functionality to add existing user to this tenant."));
                }
            }

            // Hash password
            var hashedPassword = await _authenticationService.HashPasswordAsync(request.Password);

            // Create global user
            var user = new User
            {
                Id = _guidProvider.SortableGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = hashedPassword,
                SecurityStamp = Guid.NewGuid().ToString(),
                PhoneNumber = request.PhoneNumber,
                IsEmailConfirmed = false, // Require email confirmation
                CreatedAt = _dateTimeProvider.CurrentUtcTime
            };

            await _userRepository.CreateAsync(user);

            // Add user to current tenant
            var userTenant = new UserTenant
            {
                Id = _guidProvider.SortableGuid(),
                UserId = user.Id,
                TenantId = tenantId,
                IsActive = true,
                JoinedAt = _dateTimeProvider.CurrentUtcTime,
                InvitedBy = User.Identity?.Name ?? "System" // Current user's email or system
            };

            await _userTenantRepository.AddUserToTenantAsync(userTenant);

            // Assign tenant-specific roles
            foreach (var roleName in request.Roles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName, tenantId, HttpContext.RequestAborted);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        Id = _guidProvider.SortableGuid(),
                        UserId = user.Id,
                        RoleId = role.Id,
                        TenantId = tenantId
                    };
                    await _userRoleRepository.CreateAsync(userRole);
                }
            }

            var response = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsLocked = user.IsGloballyLocked,
                LockoutEnd = user.GlobalLockoutEnd,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                Roles = request.Roles.ToList()
            };

            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                ApiResponse<UserResponse>.SuccessResult(response, "User created successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<UserResponse>.ErrorResult("An error occurred while creating user"));
        }
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionConstants.UsersEdit)]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id,
        [FromBody] UpdateUserRequest request)
    {
        try
        {
            var tenantId = GetTenantId();
            
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found"));
            }

            // Check if user is a member of current tenant
            var isMember = await _userTenantRepository.IsUserMemberOfTenantAsync(id, tenantId, HttpContext.RequestAborted);
            if (!isMember)
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found in current tenant"));
            }
            
            // Check if email already exists (excluding current user)
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, HttpContext.RequestAborted);
            if (existingUser != null && existingUser.Id != id)
            {
                return BadRequest(ApiResponse<UserResponse>.ErrorResult("Email already exists"));
            }

            // Update user
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.IsEmailConfirmed = request.IsEmailConfirmed;
            user.IsGloballyLocked = request.IsLocked;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            var response = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.IsEmailConfirmed,
                IsLocked = user.IsGloballyLocked,
                LockoutEnd = user.GlobalLockoutEnd,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<UserResponse>.SuccessResult(response, "User updated successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<UserResponse>.ErrorResult("An error occurred while updating user"));
        }
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionConstants.UsersDelete)]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            // Check if user is a member of current tenant
            var isMember = await _userTenantRepository.IsUserMemberOfTenantAsync(id, tenantId, HttpContext.RequestAborted);
            if (!isMember)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found in current tenant"));
            }

            // Get tenant-specific user claims and delete them
            var userClaims = await _userClaimRepository.GetByUserIdAsync(id, tenantId, HttpContext.RequestAborted);
            foreach (var claim in userClaims)
            {
                await _userClaimRepository.DeleteAsync(claim.UserId, claim.ClaimType, claim.ClaimValue, tenantId, HttpContext.RequestAborted);
            }

            // Get tenant-specific user roles and delete them
            var userRoles = await _userRoleRepository.GetByUserIdAsync(id, tenantId, HttpContext.RequestAborted);
            foreach (var userRole in userRoles)
            {
                await _userRoleRepository.DeleteAsync(userRole.UserId, userRole.RoleId, tenantId, HttpContext.RequestAborted);
            }

            // Remove user from current tenant
            await _userTenantRepository.RemoveUserFromTenantAsync(id, tenantId);

            // Check if user is member of any other tenants
            var otherTenantMemberships = await _userTenantRepository.GetUserTenantsAsync(id, HttpContext.RequestAborted);
            
            // If user is not a member of any tenants, delete the user globally
            if (!otherTenantMemberships.Any())
            {
                await _userRepository.DeleteAsync(id);
                return Ok(ApiResponse<object>.SuccessResult(new object(), "User removed from tenant and deleted globally (no other tenant memberships)"));
            }

            return Ok(ApiResponse<object>.SuccessResult(new object(), "User removed from current tenant"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while deleting user"));
        }
    }

    [HttpPost("{id:guid}/roles")]
    [RequirePermission(PermissionConstants.UsersEdit)]
    public async Task<IActionResult> AssignRoles([FromRoute] Guid id,
        [FromBody] ICollection<string> roleNames)
    {
        try
        {
            var tenantId = GetTenantId();
            
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            // Check if user is a member of current tenant
            var isMember = await _userTenantRepository.IsUserMemberOfTenantAsync(id, tenantId, HttpContext.RequestAborted);
            if (!isMember)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found in current tenant"));
            }

            // Remove existing tenant-specific roles
            var existingRoles = await _userRoleRepository.GetByUserIdAsync(id, tenantId, HttpContext.RequestAborted);
            foreach (var existingRole in existingRoles)
            {
                await _userRoleRepository.DeleteAsync(existingRole.UserId, existingRole.RoleId, tenantId, HttpContext.RequestAborted);
            }

            // Assign new tenant-specific roles
            foreach (var roleName in roleNames)
            {
                var role = await _roleRepository.GetByNameAsync(roleName, tenantId);
                if (role == null) continue;
                var userRole = new UserRole
                {
                    Id = _guidProvider.SortableGuid(),
                    UserId = id,
                    RoleId = role.Id,
                    TenantId = tenantId
                };
                await _userRoleRepository.CreateAsync(userRole);
            }

            return Ok(ApiResponse<object>.SuccessResult(new object(), "Roles assigned successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while assigning roles"));
        }
    }

    [HttpPost("invite")]
    [RequirePermission(PermissionConstants.UsersCreate)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        try
        {
            var tenantId = GetTenantId();
            
            // Check if user exists globally
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, HttpContext.RequestAborted);
            if (existingUser == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("User with this email does not exist. Use CreateUser to create a new user."));
            }

            // Check if user is already a member of this tenant
            var isMember = await _userTenantRepository.IsUserMemberOfTenantAsync(existingUser.Id, tenantId, HttpContext.RequestAborted);
            if (isMember)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("User is already a member of this tenant"));
            }

            // Add user to current tenant
            var userTenant = new UserTenant
            {
                Id = _guidProvider.SortableGuid(),
                UserId = existingUser.Id,
                TenantId = tenantId,
                IsActive = true,
                JoinedAt = _dateTimeProvider.CurrentUtcTime,
                InvitedBy = User.Identity?.Name ?? "System" // Current user's email or system
            };

            await _userTenantRepository.AddUserToTenantAsync(userTenant);

            // Assign tenant-specific roles if specified
            foreach (var roleName in request.Roles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName, tenantId, HttpContext.RequestAborted);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        Id = _guidProvider.SortableGuid(),
                        UserId = existingUser.Id,
                        RoleId = role.Id,
                        TenantId = tenantId
                    };
                    await _userRoleRepository.CreateAsync(userRole);
                }
            }

            var response = new UserResponse
            {
                Id = existingUser.Id,
                Email = existingUser.Email,
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                FullName = existingUser.FullName,
                PhoneNumber = existingUser.PhoneNumber,
                IsEmailConfirmed = existingUser.IsEmailConfirmed,
                IsLocked = existingUser.IsGloballyLocked,
                LockoutEnd = existingUser.GlobalLockoutEnd,
                LastLoginAt = existingUser.LastLoginAt,
                CreatedAt = existingUser.CreatedAt,
                Roles = request.Roles.ToList()
            };

            return Ok(ApiResponse<UserResponse>.SuccessResult(response, "User invited to tenant successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while inviting user"));
        }
    }
}