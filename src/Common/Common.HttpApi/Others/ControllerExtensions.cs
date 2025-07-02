using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Common.HttpApi.Others;

public static class ControllerExtensions
{
    public static IActionResult MakeResponse(this ActionContext context, int code, object? data = null)
    {
        if (data is null)
        {
            return new StatusCodeResult(code);
        }

        return new JsonResult(data) { StatusCode = code };
    }
    public static IActionResult MakeValidationErrorResponse(this ActionContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => new
            {
                Field = JsonNamingPolicy.CamelCase.ConvertName(e.Key),
                Errors = e.Value?.Errors.Select(er => er.ErrorMessage)
            });

        var problemDetails = new
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Message = "One or more validation errors occurred.",
            Errors = errors
        };

        return context.MakeResponse(StatusCodes.Status400BadRequest, problemDetails);
    }
}
