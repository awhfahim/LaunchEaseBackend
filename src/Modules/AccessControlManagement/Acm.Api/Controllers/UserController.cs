using System.Globalization;
using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.DataTransferObjects.Request;
using Acm.Application.DataTransferObjects.Response;
using Acm.Application.Services;
using Acm.Application.Services.Interfaces;
using Acm.Domain.Entities;
using Acm.Infrastructure.Authorization.Attributes;
using Acm.Infrastructure.Authorization;
using Common.Application.Providers;
using Common.Application.Services;
using Common.HttpApi.Controllers;
using Common.HttpApi.DTOs;
using Common.HttpApi.Others;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Acm.Api.Controllers;

[Route("api/users")]
[Authorize]
[RequireTenant]
public class UserController : JsonApiControllerBase
{
    private readonly IUserService _userService;
    private readonly LazyService<ILogger<UserController>> _logger;

    public UserController(
        IUserService userService, LazyService<ILogger<UserController>> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission(PermissionConstants.UsersView)]
    public async Task<IActionResult> GetUsers([FromQuery, BindRequired] PaginationQueryParameter pagination,
        [FromQuery] string? searchString = null)
    {
        try
        {
            var (totalCount, usersList) = await _userService.GetUsersByTenantIdAsync(
                GetTenantId(), pagination.Page, pagination.Limit, searchString, HttpContext.RequestAborted);

            var responses = usersList.Select(user => new UserResponse
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
                })
                .ToList();

            var totalPages = checked((int)Math.Ceiling((double)totalCount / pagination.Limit));

            var result = new
            {
                TotalCount = totalCount,
                CurrentPage = pagination.Page,
                TotalPages = totalPages,
                Users = responses
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
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
        var tenantId = GetTenantId();

        var result = await _userService.GetUserAsync(id, tenantId, HttpContext.RequestAborted);

        return FromResult(result, userResponse => Ok(
            ApiResponse<UserResponse>.SuccessResult(userResponse)));
    }

    [HttpPost]
    [RequirePermission(PermissionConstants.UsersCreate)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var tenantId = GetTenantId();
        var result = await _userService.CreateUserWithTenantAsync(request, tenantId, request.RoleId,
            HttpContext.RequestAborted);

        return FromResult(result, createdUser => CreatedAtAction(nameof(GetUser), new { id = createdUser.Id },
            ApiResponse<UserResponse>.SuccessResult(new UserResponse()
            {
                Id = createdUser.Id,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                FullName = createdUser.FullName,
                PhoneNumber = createdUser.PhoneNumber,
                IsEmailConfirmed = createdUser.IsEmailConfirmed,
                IsLocked = createdUser.IsGloballyLocked,
                LockoutEnd = createdUser.GlobalLockoutEnd,
                LastLoginAt = createdUser.LastLoginAt,
                CreatedAt = createdUser.CreatedAt
            })));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionConstants.UsersEdit)]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id,
        [FromBody] UpdateUserRequest request)
    {
        var tenantId = GetTenantId();

        var result = await _userService.UpdateUserAsync(id, request, tenantId, HttpContext.RequestAborted);

        return FromResult(result, user => Ok(ApiResponse<UserResponse>.SuccessResult(new UserResponse
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
        }, "User updated successfully")));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionConstants.UsersDelete)]
    public async Task<IActionResult> DeleteUser([FromRoute, BindRequired] Guid id)
    {
        var tenantId = GetTenantId();
        var result = await _userService.DeleteUserAsync(id, tenantId, HttpContext.RequestAborted);
        return FromResult(result,
            _ => Ok(ApiResponse<object>.SuccessResult(new object(), "User deleted successfully")));
    }

    [HttpPost("{id:guid}/assign-roles")]
    [RequirePermission(PermissionConstants.UsersEdit)]
    public async Task<IActionResult> AssignRoles([FromRoute, BindRequired] Guid id,
        [FromBody] ICollection<Guid> roleIds)
    {
        try
        {
            var tenantId = GetTenantId();
            if (roleIds.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Role IDs are required"));
            }

            var result = await _userService.AssignRoleToUserAsync(id, roleIds, tenantId, HttpContext.RequestAborted);

            return FromResult(result,
                _ => Ok(ApiResponse<object>.SuccessResult(new object(), "Roles assigned successfully")));
        }
        catch (Exception ex)
        {
            _logger.Value.LogError(ex, ex.Message);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while assigning roles"));
        }
    }

    [HttpPost("{id:guid}/unassign-roles")]
    [RequirePermission(PermissionConstants.UsersEdit)]
    public async Task<IActionResult> UnassignRoles([FromRoute, BindRequired] Guid id,
        [FromBody] ICollection<Guid> roleIds)
    {
        try
        {
            var tenantId = GetTenantId();
            if (roleIds.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Role IDs are required"));
            }

            await _userService.RemoveRoleFromUserAsync(id, tenantId, roleIds, HttpContext.RequestAborted);

            return Ok(ApiResponse<object>.SuccessResult(new object(), "Roles unassigned successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while unassigning roles"));
        }
    }


    [HttpPost("invite")]
    [RequirePermission(PermissionConstants.UsersCreate)]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        try
        {
            var tenantId = GetTenantId();
            var isAdded = await _userService.InviteUserAsync(request.Email, tenantId, HttpContext.RequestAborted);
            return FromResult(isAdded,
                _ => Ok(ApiResponse<object>.SuccessResult(new object(), "User invited to tenant successfully")));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while inviting user"));
        }
    }

    [HttpGet("email-exists")]
    [RequirePermission(PermissionConstants.UsersCreate)]
    public async Task<IActionResult> CheckEmailExists([FromQuery] string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Email is required"));
            }

            var isExist = await _userService.EmailExistsAsync(email, HttpContext.RequestAborted);
            return Ok(isExist
                ? ApiResponse<bool>.SuccessResult(true, "Email exists")
                : ApiResponse<bool>.SuccessResult(false, "Email does not exist"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while checking email"));
        }
    }

    [HttpGet("user-tenants")]
    [RequirePermission(PermissionConstants.GlobalUsersView)]
    public async Task<IActionResult> GetUserTenants([FromQuery, BindRequired] Guid userId)
    {
        var result = await _userService.GetUserWithTenantsAsync(userId, HttpContext.RequestAborted);
        return Ok(ApiResponse<object>.SuccessResult(result, "User tenants fetched successfully"));
    }

    [HttpGet("search-existing-emails")]
    [RequirePermission(PermissionConstants.UsersView)]
    public async Task<IActionResult> GetExistingEmails([FromQuery, BindRequired] string email)
    {
        var results = await _userService.GetExistingEmailsAsync(email, HttpContext.RequestAborted);
        return Ok(ApiResponse<IEnumerable<string>>.SuccessResult(results, "Existing emails fetched successfully"));
    }
}