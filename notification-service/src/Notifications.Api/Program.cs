using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Notifications.Api;
using Notifications.Application.Models;
using Notifications.Application.Services;
using Notifications.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

var jwtKey = builder.Configuration["Auth:JwtKey"] ?? throw new InvalidOperationException("Auth:JwtKey missing");
var issuer = builder.Configuration["Auth:JwtIssuer"];
var audience = builder.Configuration["Auth:JwtAudience"];
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Jwt");

                var authHeader = ctx.Request.Headers.Authorization.ToString();
                logger.LogInformation(
                    "JWT OnMessageReceived: path={Path}, hasAuthHeader={Has}, startsWithBearer={IsBearer}, authLen={Len}",
                    ctx.HttpContext.Request.Path.Value,
                    !string.IsNullOrWhiteSpace(authHeader),
                    authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase),
                    authHeader?.Length ?? 0
                );

                return Task.CompletedTask;
            },

            OnTokenValidated = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Jwt");

                var sub = ctx.Principal?.FindFirst("sub")?.Value;
                logger.LogInformation("JWT validated OK. sub={Sub}", sub);

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Jwt");

                logger.LogError(ctx.Exception, "JWT auth failed");
                return Task.CompletedTask;
            },

            OnChallenge = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Jwt");

                logger.LogWarning("JWT challenge: error={Error}, desc={Desc}", ctx.Error, ctx.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Notifications API", Version = "v1" });
     var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Token"
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});

var app = builder.Build();

app.UseGlobalErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notifications API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "Notifications API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

Guid GetUserId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
    {
        throw new InvalidOperationException("Invalid user id in token");
    }

    return userId;
}

app.MapGet("/notifications", async (HttpContext context, bool? onlyUnread, int page, int pageSize, INotificationService service, CancellationToken ct) =>
{
    var userId = GetUserId(context.User);
    var notifications = await service.GetForUserAsync(userId, onlyUnread ?? false, page, pageSize, ct);
    return Results.Ok(notifications);
}).RequireAuthorization();

app.MapPost("/notifications/mark-read", async (HttpContext context, MarkReadRequest request, INotificationService service, CancellationToken ct) =>
{
    var userId = GetUserId(context.User);
    if (request.Ids == null || request.Ids.Count == 0)
    {
        return Results.NoContent();
    }

    await service.MarkReadAsync(userId, request.Ids, ct);
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/notifications/internal/notifications", async (CreateNotificationRequest request, INotificationService service, CancellationToken ct) =>
{
    await service.CreateAsync(request, ct);
    return Results.Accepted();
}).RequireAuthorization();

app.Run();