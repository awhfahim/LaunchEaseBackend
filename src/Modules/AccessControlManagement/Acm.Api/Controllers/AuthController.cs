using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application.Services;
using Acm.Application.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Acm.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserClaimRepository _userClaimRepository;
    private readonly IRoleClaimRepository _roleClaimRepository;

    public AuthController(
        IAuthenticationService authenticationService,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IUserClaimRepository userClaimRepository,
        IRoleClaimRepository roleClaimRepository)
    {
        _authenticationService = authenticationService;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _userClaimRepository = userClaimRepository;
        _roleClaimRepository = roleClaimRepository;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authenticationService.AuthenticateAsync(
                request.Email, 
                request.Password, 
                request.TenantId);

            if (!result.IsSuccess)
            {
                if (result.IsLockedOut)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Account is temporarily locked due to multiple failed login attempts"));
                }

                if (result.RequiresEmailConfirmation)
                {
                    return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Email confirmation required"));
                }

                return BadRequest(ApiResponse<LoginResponse>.ErrorResult(result.ErrorMessage ?? "Authentication failed"));
            }

            // Get user details for response
            var user = await _userRepository.GetByIdAsync(result.UserId!.Value);
            if (user == null)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("User not found"));
            }

            // Get user roles and permissions
            var roles = await _userRoleRepository.GetRoleNamesForUserAsync(user.Id);
            var userClaims = await _userClaimRepository.GetClaimsForUserAsync(user.Id);
            var roleClaims = await _roleClaimRepository.GetClaimsForUserRolesAsync(user.Id);
            
            var permissions = userClaims.Where(c => c.Type == "permission")
                .Union(roleClaims.Where(c => c.Type == "permission"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            var response = new LoginResponse
            {
                AccessToken = result.Token!,
                TokenType = "Bearer",
                ExpiresIn = 8 * 60 * 60, // 8 hours in seconds
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    TenantId = user.TenantId,
                    Roles = roles.ToList(),
                    Permissions = permissions
                }
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("An error occurred during authentication"));
        }
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken()
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst("user_id")?.Value;
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) ||
                string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("Invalid token"));
            }

            // Get user details
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("User not found"));
            }

            // Generate new token with fresh claims
            var claims = await _authenticationService.GetUserClaimsAsync(userId);
            var newToken = await _authenticationService.GenerateJwtTokenAsync(userId, tenantId, claims);

            // Get user roles and permissions
            var roles = await _userRoleRepository.GetRoleNamesForUserAsync(user.Id);
            var userClaims = await _userClaimRepository.GetClaimsForUserAsync(user.Id);
            var roleClaims = await _roleClaimRepository.GetClaimsForUserRolesAsync(user.Id);
            
            var permissions = userClaims.Where(c => c.Type == "permission")
                .Union(roleClaims.Where(c => c.Type == "permission"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            var response = new LoginResponse
            {
                AccessToken = newToken,
                TokenType = "Bearer",
                ExpiresIn = 8 * 60 * 60, // 8 hours in seconds
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    TenantId = user.TenantId,
                    Roles = roles.ToList(),
                    Permissions = permissions
                }
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("An error occurred during token refresh"));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult<ApiResponse<object>> Logout()
    {
        // In a stateless JWT implementation, logout is handled client-side
        // However, you could implement token blacklisting here if needed
        return Ok(ApiResponse<object>.SuccessResult(null, "Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<UserInfo>.ErrorResult("Invalid token"));
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserInfo>.ErrorResult("User not found"));
            }

            // Get user roles and permissions
            var roles = await _userRoleRepository.GetRoleNamesForUserAsync(user.Id);
            var userClaims = await _userClaimRepository.GetClaimsForUserAsync(user.Id);
            var roleClaims = await _roleClaimRepository.GetClaimsForUserRolesAsync(user.Id);
            
            var permissions = userClaims.Where(c => c.Type == "permission")
                .Union(roleClaims.Where(c => c.Type == "permission"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            var userInfo = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                TenantId = user.TenantId,
                Roles = roles.ToList(),
                Permissions = permissions
            };

            return Ok(ApiResponse<UserInfo>.SuccessResult(userInfo));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<UserInfo>.ErrorResult("An error occurred while fetching user information"));
        }
    }
}
