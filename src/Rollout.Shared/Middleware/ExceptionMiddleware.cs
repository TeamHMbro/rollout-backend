using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rollout.Shared.Exceptions;

namespace Rollout.Shared.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (AppException exception)
        {
            await WriteProblemAsync(httpContext, exception.StatusCode, exception.ErrorCode, exception.ErrorDetail);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception. Path={Path}", httpContext.Request.Path);
            await WriteProblemAsync(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "internal_server_error",
                "An unexpected error occurred.");
        }
    }

    private static Task WriteProblemAsync(HttpContext httpContext, int statusCode, string errorCode, string? detail)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = errorCode,
            Detail = detail
        };

        problem.Extensions["code"] = errorCode;

        return httpContext.Response.WriteAsJsonAsync(problem);
    }
}