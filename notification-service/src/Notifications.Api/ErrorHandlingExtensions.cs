using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notifications.Domain;
using System.Text.Json;

namespace Notifications.Api;

public static class ErrorHandlingExtensions
{
    public static IApplicationBuilder UseGlobalErrorHandling(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(builder =>
        {
            builder.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is DomainException domainException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    var payload = JsonSerializer.Serialize(new { code = domainException.Code, message = domainException.Message });
                    await context.Response.WriteAsync(payload);
                    return;
                }

                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
                logger.LogError(exception, "Unhandled exception");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                var response = JsonSerializer.Serialize(new { code = "InternalServerError", message = "An unexpected error has occurred." });
                await context.Response.WriteAsync(response);
            });
        });

        return app;
    }
}
