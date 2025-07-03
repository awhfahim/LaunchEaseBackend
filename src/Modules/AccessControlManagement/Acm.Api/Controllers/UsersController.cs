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
    private readonly IAuthenticationService _authenticationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IGuidProvider _guidProvider;

    public UsersController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IUserClaimRepository userClaimRepository,
        IAuthenticationService authenticationService, IDateTimeProvider dateTimeProvider, IGuidProvider guidProvider)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _userClaimRepository = userClaimRepository;
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
                    IsLocked = user.IsLocked,
                    LockoutEnd = user.LockoutEnd,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt
                });
            }

            return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResult(responses));
        }
        catch (Exception ex)
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
            var user = await _userRepository.GetByIdAsync(id, HttpContext.RequestAborted);
            if (user == null || user.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found"));
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
                IsLocked = user.IsLocked,
                LockoutEnd = user.LockoutEnd,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<UserResponse>.SuccessResult(response));
        }
        catch (Exception ex)
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
            // Check if email already exists in this tenant
            var existingUser =
                await _userRepository.GetByEmailAsync(request.Email, tenantId, HttpContext.RequestAborted);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<UserResponse>.ErrorResult("Email already exists"));
            }

            // Hash password
            var hashedPassword = await _authenticationService.HashPasswordAsync(request.Password);

            // Create user
            var user = new User
            {
                Id = _guidProvider.SortableGuid(),
                TenantId = tenantId,
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

            // Assign roles if specified
            foreach (var roleName in request.Roles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName, tenantId, HttpContext.RequestAborted);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        Id = _guidProvider.SortableGuid(),
                        UserId = user.Id,
                        RoleId = role.Id
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
                IsLocked = user.IsLocked,
                LockoutEnd = user.LockoutEnd,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                Roles = request.Roles.ToList()
            };

            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                ApiResponse<UserResponse>.SuccessResult(response, "User created successfully"));
        }
        catch (Exception ex)
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
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<UserResponse>.ErrorResult("User not found"));
            }

            var tenantId = GetTenantId();
            // Check if email already exists (excluding current user)
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, tenantId);
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
            user.IsLocked = request.IsLocked;
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
                IsLocked = user.IsLocked,
                LockoutEnd = user.LockoutEnd,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<UserResponse>.SuccessResult(response, "User updated successfully"));
        }
        catch (Exception ex)
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
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            // Delete user claims
            await _userClaimRepository.DeleteByUserIdAsync(id);

            // Delete user roles
            await _userRoleRepository.DeleteByUserIdAsync(id);

            // Delete user
            await _userRepository.DeleteAsync(id);

            return Ok(ApiResponse<object>.SuccessResult(null, "User deleted successfully"));
        }
        catch (Exception ex)
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
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.TenantId != GetTenantId())
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            // Remove existing roles
            await _userRoleRepository.DeleteByUserIdAsync(id);

            // Assign new roles
            foreach (var roleName in roleNames)
            {
                var role = await _roleRepository.GetByNameAsync(roleName, GetTenantId());
                if (role == null) continue;
                var userRole = new UserRole
                {
                    Id = _guidProvider.SortableGuid(),
                    UserId = id,
                    RoleId = role.Id
                };
                await _userRoleRepository.CreateAsync(userRole);
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Roles assigned successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while assigning roles"));
        }
    }
}