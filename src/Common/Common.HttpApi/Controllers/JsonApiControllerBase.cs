using System.Security.Claims;
using Common.Application.Misc;
using Common.HttpApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Common.HttpApi.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class JsonApiControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        var errorResponse = ApiResponse<T>.ErrorResult(result.Error ?? "An error occurred");
        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(errorResponse),
            ErrorType.BadRequest => BadRequest(errorResponse),
            ErrorType.Conflict => Conflict(errorResponse),
            ErrorType.Forbidden => Forbid(),
            ErrorType.Unauthorized => Unauthorized(errorResponse),
            ErrorType.Validation => UnprocessableEntity(errorResponse),
            _ => StatusCode(500, errorResponse)
        };
    }

    protected long? GetCurrentLoggedInUserId(string? locator = null)
    {
        var key = locator ?? ClaimTypes.NameIdentifier;

        var id = User.FindFirst(x => x.Type == key)?.Value;

        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        return long.TryParse(id, out var result) ? result : null;
    }
    
    protected Guid GetTenantId()
    {
        return (Guid)HttpContext.Items["TenantId"]!;
    }
}
