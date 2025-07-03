using System.Security.Claims;
using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application;
using Acm.Application.Services;
using Acm.Application.Repositories;
using Common.HttpApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Acm.Api.Controllers;

/// <summary>
/// Authentication controller implementing secure multi-tenant authentication flow.
/// JWT tokens are stored in HTTP-only cookies for enhanced security against XSS attacks.
/// </summary>
[Route("api/auth")]
public class AuthController : JsonApiControllerBase
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
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Redirect users to use the new multi-tenant authentication flow
        return BadRequest(ApiResponse<object>.ErrorResult(
            "This endpoint is deprecated. Please use /api/auth/initial-login to authenticate, then /api/auth/tenant-login to select a tenant."));
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

            HttpContext.Response.Cookies.Append(AcmAppConstants.AccessTokenCookieKey,
                newToken, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddMinutes(800),
                    Secure = true, 
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            var response = new LoginResponse
            {
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    TenantId = tenantId, // Use the tenant ID from the token
                    Roles = roles.ToList(),
                    Permissions = permissions
                }
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Token refreshed successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("An error occurred during token refresh"));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Clear the HTTP-only cookie containing the JWT token
        HttpContext.Response.Cookies.Delete(AcmAppConstants.AccessTokenCookieKey, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        return Ok(ApiResponse<object>.SuccessResult(new { }, "Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) ||
                string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return Unauthorized(ApiResponse<UserInfo>.ErrorResult("Invalid token"));
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserInfo>.ErrorResult("User not found"));
            }

            // Get user roles and permissions for this tenant
            var roles = await _userRoleRepository.GetRoleNamesForUserAsync(user.Id, tenantId);
            var userClaims = await _userClaimRepository.GetClaimsForUserAsync(user.Id, tenantId);
            var roleClaims = await _roleClaimRepository.GetClaimsForUserRolesAsync(user.Id, tenantId);

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
                TenantId = tenantId, // Use tenant ID from token
                Roles = roles.ToList(),
                Permissions = permissions
            };

            return Ok(ApiResponse<UserInfo>.SuccessResult(userInfo));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<UserInfo>.ErrorResult("An error occurred while fetching user information"));
        }
    }

    [HttpPost("initial-login")]
    public async Task<IActionResult> InitialLogin([FromBody] InitialLoginRequest request)
    {
        try
        {
            var result = await _authenticationService.AuthenticateUserAsync(
                request.Email,
                request.Password);

            if (!result.IsSuccess)
            {
                return result.IsLockedOut
                    ? BadRequest(ApiResponse<InitialLoginResponse>.ErrorResult(
                        "Account is temporarily locked due to multiple failed login attempts"))
                    : BadRequest(result.RequiresEmailConfirmation
                        ? ApiResponse<InitialLoginResponse>.ErrorResult("Email confirmation required")
                        : ApiResponse<InitialLoginResponse>.ErrorResult(result.ErrorMessage ??
                                                                        "Authentication failed"));
            }

            // Get user details
            var user = await _userRepository.GetByIdAsync(result.UserId!.Value);
            if (user == null)
            {
                return BadRequest(ApiResponse<InitialLoginResponse>.ErrorResult("User not found"));
            }

            // Map accessible tenants
            var tenantResponses = result.AccessibleTenants.Select(t => new TenantInfoResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                LogoUrl = t.LogoUrl,
                UserRoles = t.UserRoles.ToList()
            }).ToList();

            var response = new InitialLoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                AccessibleTenants = tenantResponses
            };
            
            HttpContext.Response.Cookies.Append(AcmAppConstants.AccessTokenCookieKey,
                result.TempToken, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddMinutes(5),
                    Secure = true, 
                    SameSite = SameSiteMode.Strict
                });

            return Ok(ApiResponse<InitialLoginResponse>.SuccessResult(response, "Please select a tenant to continue"));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<InitialLoginResponse>.ErrorResult("An error occurred during authentication"));
        }
    }

    [HttpPost("tenant-login")]
    [Authorize]
    public async Task<IActionResult> TenantLogin([FromBody] TenantLoginRequest request)
    {
        try
        {
            var result = await _authenticationService.AuthenticateWithTenantAsync(
                request.UserId,
                request.TenantId);

            if (!result.IsSuccess)
            {
                return BadRequest(
                    ApiResponse<TenantLoginResponse>.ErrorResult(result.ErrorMessage ??
                                                                 "Tenant authentication failed"));
            }

            // Get user details
            var user = await _userRepository.GetByIdAsync(result.UserId!.Value);
            if (user == null)
            {
                return BadRequest(ApiResponse<TenantLoginResponse>.ErrorResult("User not found"));
            }

            // Store JWT token in HTTP-only cookie for security
            HttpContext.Response.Cookies.Append(AcmAppConstants.AccessTokenCookieKey,
                result.Token!, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddMinutes(result.ExpiresIn),
                    Secure = true, 
                    SameSite = SameSiteMode.Strict
                });

            var response = new TenantLoginResponse
            {
                AccessToken = "Token stored in secure cookie", // Don't expose the actual token
                TokenType = "Bearer",
                ExpiresIn = (int)result.ExpiresIn,
                TokenStoredInCookie = true,
                User = new UserTenantInfo
                {
                    Id = result.UserId!.Value,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    TenantId = result.TenantId!.Value,
                    Roles = result.Roles.ToList(),
                    Permissions = result.Permissions.ToList()
                },
                Tenant = new TenantInfoResponse
                {
                    Id = result.TenantInfo!.Id,
                    Name = result.TenantInfo.Name,
                    Slug = result.TenantInfo.Slug,
                    LogoUrl = result.TenantInfo.LogoUrl,
                    UserRoles = result.TenantInfo.UserRoles.ToList()
                }
            };

            return Ok(ApiResponse<TenantLoginResponse>.SuccessResult(response, "Login successful"));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<TenantLoginResponse>.ErrorResult("An error occurred during tenant authentication"));
        }
    }

    [HttpPost("switch-tenant")]
    [Authorize]
    public async Task<IActionResult> SwitchTenant([FromBody] TenantSwitchRequest request)
    {
        try
        {
            // Get user ID from current token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<TenantLoginResponse>.ErrorResult("Invalid token"));
            }

            var result = await _authenticationService.SwitchTenantAsync(userId, request.TenantId);

            if (!result.IsSuccess)
            {
                return BadRequest(
                    ApiResponse<TenantLoginResponse>.ErrorResult(result.ErrorMessage ?? "Tenant switch failed"));
            }

            // Get user details
            var user = await _userRepository.GetByIdAsync(result.UserId!.Value);
            if (user == null)
            {
                return BadRequest(ApiResponse<TenantLoginResponse>.ErrorResult("User not found"));
            }

            // Store JWT token in HTTP-only cookie for security
            HttpContext.Response.Cookies.Append(AcmAppConstants.AccessTokenCookieKey,
                result.Token!, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddMinutes(result.ExpiresIn),
                    Secure = true, 
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            var response = new TenantLoginResponse
            {
                AccessToken = "Token stored in secure cookie", // Don't expose the actual token
                TokenType = "Bearer",
                ExpiresIn = (int)result.ExpiresIn,
                TokenStoredInCookie = true,
                User = new UserTenantInfo
                {
                    Id = result.UserId!.Value,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    TenantId = result.TenantId!.Value,
                    Roles = result.Roles.ToList(),
                    Permissions = result.Permissions.ToList()
                },
                Tenant = new TenantInfoResponse
                {
                    Id = result.TenantInfo!.Id,
                    Name = result.TenantInfo.Name,
                    Slug = result.TenantInfo.Slug,
                    LogoUrl = result.TenantInfo.LogoUrl,
                    UserRoles = result.TenantInfo.UserRoles.ToList()
                }
            };

            return Ok(ApiResponse<TenantLoginResponse>.SuccessResult(response, "Tenant switched successfully"));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<TenantLoginResponse>.ErrorResult("An error occurred during tenant switch"));
        }
    }
}