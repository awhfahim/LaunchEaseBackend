using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security;
using System.Security.Authentication;
using Acm.Api.DTOs.Responses;
using Acm.Infrastructure.Authorization.Exceptions;
using Common.HttpApi.DTOs;

namespace LaunchEase.Api.Others;

/// <summary>
/// Enhanced global exception handler for LaunchEase multi-tenant application.
/// Provides consistent error responses using ApiResponse pattern for API endpoints
/// and comprehensive logging for different exception types.
/// Features:
/// - Tenant-aware error messages
/// - Security-focused error handling
/// - Database constraint violation handling
/// - Validation error formatting
/// - Consistent API response format
/// </summary>
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
        var problemDetails = CreateProblemDetails(exception, httpContext);

        // Log different levels based on exception type
        LogException(exception);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        
        // Return consistent ApiResponse format for API endpoints
        if (IsApiRequest(httpContext))
        {
            var apiResponse = CreateApiErrorResponse(problemDetails);
            await httpContext.Response.WriteAsJsonAsync(apiResponse, ct);
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
        }
        
        return true;
    }

    private static ProblemDetails CreateProblemDetails(Exception exception, HttpContext? httpContext = null)
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

            case TenantIsolationViolationException tenantViolation:
                status = StatusCodes.Status403Forbidden;
                title = "Tenant Boundary Violation";
                details = $"You cannot access data from tenant '{tenantViolation.RequestedTenantId}' while authenticated to tenant '{tenantViolation.UserTenantId}'. Operation: {tenantViolation.Operation}";
                break;

            case InsufficientPermissionException permissionEx:
                status = StatusCodes.Status403Forbidden;
                title = "Insufficient Permissions";
                details = $"Operation '{permissionEx.Operation}' requires '{permissionEx.RequiredPermission}' permission. Contact your administrator to request access.";
                break;

            case GlobalOperationDeniedException globalEx:
                status = StatusCodes.Status403Forbidden;
                title = "Global Operation Denied";
                details = $"Operation '{globalEx.Operation}' requires {globalEx.RequiredLevel} privileges. Contact your system administrator.";
                break;

            case UnauthorizedAccessException when exception.Message.Contains("tenant"):
                status = StatusCodes.Status403Forbidden;
                title = "Tenant Access Denied";
                details = "You do not have permission to access resources in this tenant. Please verify your tenant membership.";
                break;

            case UnauthorizedAccessException when exception.Message.Contains("global"):
                status = StatusCodes.Status403Forbidden;
                title = "Global Access Denied";
                details = "You do not have system-wide permissions to perform this action. Contact your system administrator.";
                break;

            case UnauthorizedAccessException when exception.Message.Contains("cross-tenant"):
                status = StatusCodes.Status403Forbidden;
                title = "Cross-Tenant Access Denied";
                details = "You do not have permission to access resources across multiple tenants.";
                break;

            case UnauthorizedAccessException when exception.Message.Contains("business.owner"):
                status = StatusCodes.Status403Forbidden;
                title = "Business Owner Access Required";
                details = "This operation requires business owner privileges. Contact the business owner for access.";
                break;

            case UnauthorizedAccessException when exception.Message.Contains("system.admin"):
                status = StatusCodes.Status403Forbidden;
                title = "System Administrator Access Required";
                details = "This operation requires system administrator privileges. Contact your system administrator.";
                break;

            case UnauthorizedAccessException when exception.Message.Contains("global."):
                status = StatusCodes.Status403Forbidden;
                title = "Global Permission Required";
                details = "You do not have permission to perform global operations across tenants.";
                break;

            case UnauthorizedAccessException:
                status = StatusCodes.Status401Unauthorized;
                title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized);
                details = "You do not have permission to perform this action in the current context.";
                break;

            case AuthenticationException:
                status = StatusCodes.Status401Unauthorized;
                title = "Authentication Failed";
                details = "Invalid credentials or authentication token. Please log in again.";
                break;

            case SecurityException:
                status = StatusCodes.Status403Forbidden;
                title = "Access Forbidden";
                details = "You do not have sufficient permissions to access this resource.";
                break;

            case InvalidOperationException when exception.Message.Contains("tenant"):
                status = StatusCodes.Status400BadRequest;
                title = "Invalid Tenant Operation";
                details = "The requested operation is not valid for the current tenant context. Please verify your tenant access.";
                break;

            case InvalidOperationException when exception.Message.Contains("user"):
                status = StatusCodes.Status400BadRequest;
                title = "Invalid User Operation";
                details = "The requested user operation could not be completed. Please check your request.";
                break;

            case InvalidOperationException when exception.Message.Contains("tenant isolation"):
                status = StatusCodes.Status403Forbidden;
                title = "Tenant Isolation Violation";
                details = "You cannot access data outside your tenant boundary. Contact your administrator if you need cross-tenant access.";
                break;

            case InvalidOperationException when exception.Message.Contains("permission denied"):
                status = StatusCodes.Status403Forbidden;
                title = "Permission Denied";
                details = "You do not have the required permission to perform this action in the current tenant context.";
                break;

            case ArgumentException when exception.Message.Contains("email"):
                status = StatusCodes.Status400BadRequest;
                title = "Invalid Email";
                details = "Please provide a valid email address format.";
                break;

            case ArgumentException when exception.Message.Contains("password"):
                status = StatusCodes.Status400BadRequest;
                title = "Invalid Password";
                details = "Password does not meet the required criteria.";
                break;

            case ArgumentNullException:
                status = StatusCodes.Status400BadRequest;
                title = "Missing Required Data";
                details = "A required field was not provided. Please check your request.";
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

            case TimeoutException:
                status = StatusCodes.Status408RequestTimeout;
                title = "Request Timeout";
                details = "The request took too long to process. Please try again.";
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
                                
                                // Provide more context-specific messages
                                if (uniqueField.Contains("email"))
                                {
                                    details = "This email address is already registered. Please use a different email or try logging in.";
                                }
                                else if (uniqueField.Contains("slug"))
                                {
                                    details = "This slug is already taken. Please choose a different one.";
                                }
                                else if (uniqueField.Contains("name"))
                                {
                                    details = "This name is already in use. Please choose a different name.";
                                }
                                else
                                {
                                    details = $"A record with this {uniqueField} already exists.";
                                }
                            }
                            else
                            {
                                details = "This record already exists. Please check your input.";
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

            case KeyNotFoundException:
                status = StatusCodes.Status404NotFound;
                title = "Resource Not Found";
                details = "The requested resource could not be found.";
                break;

            case NotSupportedException when exception.Message.Contains("tenant"):
                status = StatusCodes.Status400BadRequest;
                title = "Operation Not Supported";
                details = "This operation is not supported in the current tenant context.";
                break;

            case NotSupportedException:
                status = StatusCodes.Status501NotImplemented;
                title = "Operation Not Supported";
                details = "The requested operation is not supported.";
                break;

            case TaskCanceledException:
            case OperationCanceledException:
                status = StatusCodes.Status408RequestTimeout;
                title = "Request Cancelled";
                details = "The request was cancelled or timed out. Please try again.";
                break;
            default:
                // For any other unhandled exceptions, log as critical
                status = StatusCodes.Status500InternalServerError;
                title = "Unexpected Error";
                details = "An unexpected error occurred. Please try again later or contact support.";
                break;
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

    private void LogException(Exception exception)
    {
        switch (exception)
        {
            case UnauthorizedAccessException:
            case AuthenticationException:
                _logger.LogWarning(exception, "Authentication/Authorization error: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
            
            case SecurityException:
                _logger.LogWarning(exception, "Security violation: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
                
            case ArgumentException:
                _logger.LogWarning(exception, "Client error - Invalid argument: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
                
            case InvalidOperationException when exception.Message.Contains("tenant"):
                _logger.LogWarning(exception, "Tenant context error: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
                
            case InvalidOperationException:
                _logger.LogWarning(exception, "Invalid operation: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
                
            case TimeoutException:
                _logger.LogError(exception, "Timeout error: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
                
            case DbUpdateException:
            case PostgresException:
                _logger.LogError(exception, "Database error: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
                
            case FluentValidation.ValidationException:
                _logger.LogInformation(exception, "Validation error: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
                
            default:
                _logger.LogCritical(exception, "Unhandled exception: {Message} - Path: {Path}", 
                    exception.Message, GetCurrentPath());
                break;
        }
    }

    private static string GetCurrentPath()
    {
        // Simple fallback since we don't have direct access to HttpContext in this static method
        return "API Request";
    }

    private static bool IsApiRequest(HttpContext httpContext)
    {
        return httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
               httpContext.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true);
    }

    private static object CreateApiErrorResponse(ProblemDetails problemDetails)
    {
        // Use ApiResponse<object> for consistent API error responses
        if (problemDetails is ValidationProblemDetails validationProblem)
        {
            var validationErrors = validationProblem.Errors?
                .SelectMany(kvp => kvp.Value.Select(error => $"{kvp.Key}: {error}"))
                .ToList() ?? new List<string>();
            
            return ApiResponse<object>.ErrorResult(
                problemDetails.Detail ?? problemDetails.Title ?? "Validation failed",
                validationErrors
            );
        }

        return ApiResponse<object>.ErrorResult(
            problemDetails.Detail ?? problemDetails.Title ?? "An error occurred"
        );
    }

    /// <summary>
    /// Determines if additional error details should be included based on environment
    /// </summary>
    private static bool ShouldIncludeDetailedErrorInfo(HttpContext? httpContext)
    {
        // In development, include more details
        // In production, limit sensitive information
        try
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
