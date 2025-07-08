using System.Security.Claims;
using Acm.Api.DTOs.Requests;
using Acm.Api.DTOs.Responses;
using Acm.Application;
using Acm.Application.DataTransferObjects;
using Acm.Application.Services.Interfaces;
using Common.HttpApi.Controllers;
using Common.HttpApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Acm.Api.Controllers;

[Route("api/auth")]
public class AuthController : JsonApiControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly ICryptographyService _cryptographyService;

    public AuthController(
        IAuthenticationService authenticationService,
        IUserService userService,
        ICryptographyService cryptographyService)
    {
        _authenticationService = authenticationService;
        _userService = userService;
        _cryptographyService = cryptographyService;
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId) ||
                string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                return Unauthorized(ApiResponse<UserInfoDto>.ErrorResult("Invalid token"));
            }

            var userInfo = await _userService.GetRefreshTokenAsync(userId, tenantId, HttpContext.RequestAborted);

            var claims = await _authenticationService.GetUserClaimsAsync(userId);
            var newToken = await _cryptographyService.GenerateJwtTokenAsync(userId, tenantId, claims);

            HttpContext.Response.Cookies.Append(AcmAppConstants.AccessTokenCookieKey,
                newToken, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTimeOffset.Now.AddMinutes(800),
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });

            return FromResult(userInfo,
                dto => Ok(ApiResponse<UserInfoDto>.SuccessResult(dto,
                    "Token refreshed successfully")));
        }
        catch (Exception)
        {
            return StatusCode(500, ApiResponse<UserInfoDto>.ErrorResult("An error occurred during token refresh"));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
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
                return Unauthorized(ApiResponse<UserInfoDto>.ErrorResult("Invalid token"));
            }

            var userInfo = await _userService.GetUserInfoAsync(userId, tenantId, HttpContext.RequestAborted);

            return FromResult(userInfo, dto => Ok(ApiResponse<UserInfoDto>.SuccessResult(dto)));
        }
        catch (Exception)
        {
            return StatusCode(500,
                ApiResponse<UserInfoDto>.ErrorResult("An error occurred while fetching user information"));
        }
    }

    [HttpPost("login")]
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

            var user = await _userService.GetByIdAsync(result.UserId!.Value, HttpContext.RequestAborted);
            if (user == null)
            {
                return BadRequest(ApiResponse<InitialLoginResponse>.ErrorResult("User not found"));
            }

            var response = new InitialLoginResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                AccessibleTenants = result.AccessibleTenants.Select(t => new TenantInfoResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    LogoUrl = t.LogoUrl,
                    UserRoles = t.UserRoles.ToList()
                }).ToList()
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

            var user = await _userService.GetByIdAsync(result.UserId!.Value, HttpContext.RequestAborted);
            if (user == null)
            {
                return BadRequest(ApiResponse<TenantLoginResponse>.ErrorResult("User not found"));
            }

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
                AccessToken = "Token stored in secure cookie",
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

            var user = await _userService.GetByIdAsync(result.UserId!.Value, HttpContext.RequestAborted);
            if (user == null)
            {
                return BadRequest(ApiResponse<TenantLoginResponse>.ErrorResult("User not found"));
            }

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
                AccessToken = "Token stored in secure cookie",
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