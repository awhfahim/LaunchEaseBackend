using System.Security.Claims;
using System.Text.Json;
using Common.HttpApi.Others;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharpOutcome.Helpers.Contracts;
using SharpOutcome.Helpers.Enums;

namespace Common.HttpApi.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class JsonApiControllerBase : ControllerBase
{
    protected IActionResult SendJsonResponse(int code, object? data = null)
    {
        return ControllerContext.MakeResponse(code, data);
    }
    protected IActionResult HttpBadOutcomeResponse(IBadOutcome<HttpBadOutcomeTag> error)
    {
        return error.Tag switch
        {
            HttpBadOutcomeTag.Conflict => ControllerContext.MakeResponse(StatusCodes.Status409Conflict,
                error.Reason),
            HttpBadOutcomeTag.BadRequest => ControllerContext.MakeResponse(StatusCodes.Status400BadRequest,
                error.Reason),
            HttpBadOutcomeTag.NotFound => ControllerContext.MakeResponse(StatusCodes.Status404NotFound,
                error.Reason),
            HttpBadOutcomeTag.Unauthorized => ControllerContext.MakeResponse(
                StatusCodes.Status401Unauthorized, error.Reason),
            HttpBadOutcomeTag.Forbidden => ControllerContext.MakeResponse(StatusCodes.Status403Forbidden,
                error.Reason),
            HttpBadOutcomeTag.NotImplemented => ControllerContext.MakeResponse(
                StatusCodes.Status501NotImplemented, error.Reason),
            HttpBadOutcomeTag.RequestTimeout => ControllerContext.MakeResponse(
                StatusCodes.Status408RequestTimeout, error.Reason),
            HttpBadOutcomeTag.InternalServerError => ControllerContext.MakeResponse(
                StatusCodes.Status500InternalServerError, error.Reason),
            _ => ControllerContext.MakeResponse(StatusCodes.Status400BadRequest, error.Reason)
        };
    }

    protected IActionResult BadOutcomeResponse(IBadOutcome error)
    {
        return error.Tag switch
        {
            BadOutcomeTag.Conflict => ControllerContext.MakeResponse(StatusCodes.Status409Conflict,
                error.Reason),
            BadOutcomeTag.BadRequest => ControllerContext.MakeResponse(StatusCodes.Status400BadRequest,
                error.Reason),
            BadOutcomeTag.NotFound => ControllerContext.MakeResponse(StatusCodes.Status404NotFound,
                error.Reason),
            BadOutcomeTag.Unauthorized => ControllerContext.MakeResponse(
                StatusCodes.Status401Unauthorized, error.Reason),
            BadOutcomeTag.Forbidden => ControllerContext.MakeResponse(StatusCodes.Status403Forbidden,
                error.Reason),
            BadOutcomeTag.Timeout => ControllerContext.MakeResponse(
                StatusCodes.Status408RequestTimeout, error.Reason),
            BadOutcomeTag.Duplicate => ControllerContext.MakeResponse(StatusCodes.Status409Conflict,
                error.Reason),
            BadOutcomeTag.Unknown => ControllerContext.MakeResponse(
                StatusCodes.Status500InternalServerError, error.Reason),
            _ => ControllerContext.MakeResponse(StatusCodes.Status400BadRequest, error.Reason)
        };
    }

    protected static IActionResult DynamicQueryResponse<T>(IEnumerable<T> dataFromDb, long count, int pageSize,
        JsonSerializerOptions? opts = null)
    {
        if (pageSize <= 0)
        {
            pageSize = 1;
        }

        var totalPages = (long)Math.Ceiling(count / (decimal)pageSize);
        return new JsonResult(new { data = dataFromDb, last_row = count, last_page = totalPages }, opts)
        {
            StatusCode = StatusCodes.Status200OK
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
