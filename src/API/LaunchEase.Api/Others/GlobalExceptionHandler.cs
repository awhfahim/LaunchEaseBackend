using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaunchEase.Api.Others;

internal class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken ct)
    {
        var problemDetails = CreateProblemDetails(exception);

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogCritical(exception, "Exception occurred: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception)
    {
        var status = StatusCodes.Status500InternalServerError;
        var title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError);
        var details = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError);

        switch (exception)
        {
            case MaxLengthExceededException:
            case NumericOverflowException:
            case CannotInsertNullException:
            case ReferenceConstraintException:
            case BadHttpRequestException:
                status = StatusCodes.Status400BadRequest;
                title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest);
                details = exception.Message;
                break;

            case UnauthorizedAccessException:
                status = StatusCodes.Status401Unauthorized;
                title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized);
                details = "You do not have permission to perform this action.";
                break;

            case UniqueConstraintException:
                status = StatusCodes.Status409Conflict;
                title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict);
                details = exception.Message;
                break;

            case NullReferenceException:
                status = StatusCodes.Status500InternalServerError;
                title = "Unexpected Error";
                details = "A required resource was missing. Please contact support if the issue persists.";
                break;

            case DbUpdateException dbUpdateEx:
                // if (dbUpdateEx.InnerException is PostgresException { SqlState: "23514" } pgEx)
                // {
                //     status = StatusCodes.Status400BadRequest;
                //     title = "Check Constraint Violation";
                //     details = pgEx.ConstraintName;
                // }
                
                if (dbUpdateEx.InnerException is PostgresException pgEx)
                {
                    switch (pgEx.SqlState)
                    {
                        case "23503":
                            status = StatusCodes.Status400BadRequest;
                            title = "Reference Error";
                            var constraintDetails = ParseForeignKeyConstraint(pgEx.ConstraintName ?? string.Empty, pgEx.MessageText);
                            details = $"Invalid reference: The {constraintDetails.columnName} you provided does not exist or has been deleted.";
                            return new ValidationProblemDetails
                            {
                                Status = status,
                                Title = title,
                                Detail = details,
                                Errors = new Dictionary<string, string[]>
                                {
                                    { constraintDetails.columnName, new[] { details } }
                                }
                            };

                        case "23514": // Check constraint violation
                            status = StatusCodes.Status400BadRequest;
                            title = "Check Constraint Violation";
                            details = $"Value violates check constraint: {pgEx.ConstraintName}";
                            break;

                        case "23505": // Unique violation
                            status = StatusCodes.Status409Conflict;
                            title = "Duplicate Entry";
                            if (pgEx.ConstraintName != null)
                            {
                                var uniqueField = ExtractUniqueConstraintField(pgEx.ConstraintName);
                                details = $"A record with this {uniqueField} already exists.";
                            }

                            break;

                        default:
                            status = StatusCodes.Status400BadRequest;
                            title = "Database Constraint Violation";
                            details = "The operation violated a database constraint.";
                            break;
                    }
                }

                break;

            case FluentValidation.ValidationException fluentValidationException:
                var errors = fluentValidationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(e => e.ErrorMessage).ToArray()
                    );

                return new ValidationProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Detail = "One or more validation errors occurred.",
                    Errors = errors
                };
        }

        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = details
        };
    }
    
    private static (string tableName, string columnName) ParseForeignKeyConstraint(string constraintName, string messageText)
    {
        // Example constraint name format: "fk_table_column"
        var parts = constraintName.Split('_');
        string columnName = parts.Length > 2 ? parts[2] : "reference";

        // Try to extract more detailed info from the message text
        // Example: 'update or delete on table "users" violates foreign key constraint "fk_posts_user_id" on table "posts"'
        var match = System.Text.RegularExpressions.Regex.Match(messageText, @"table ""(\w+)"".*table ""(\w+)""");
        if (match.Success)
        {
            string referencedTable = match.Groups[1].Value;
            string childTable = match.Groups[2].Value;
            
            // Convert to more user-friendly format (e.g., "user_id" -> "User")
            columnName = System.Text.RegularExpressions.Regex.Replace(columnName, "_id$", "");
            columnName = char.ToUpper(columnName[0]) + columnName[1..];
            
            return (referencedTable, columnName);
        }

        return ("unknown", columnName);
    }

    private static string ExtractUniqueConstraintField(string constraintName)
    {
        // Example constraint name format: "ix_table_column"
        var parts = constraintName.Split('_');
        if (parts.Length > 2)
        {
            // Convert to more user-friendly format (e.g., "email_address" -> "Email Address")
            var field = string.Join(" ", parts.Skip(2));
            return System.Text.RegularExpressions.Regex
                .Replace(field, "(?<!^)([A-Z])", " $1")
                .ToLower();
        }
        return "value";
    }
}
