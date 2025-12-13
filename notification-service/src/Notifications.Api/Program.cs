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

var jwtIssuer = builder.Configuration["Auth:JwtIssuer"] ?? throw new InvalidOperationException("Auth:JwtIssuer is not configured.");
var jwtAudience = builder.Configuration["Auth:JwtAudience"] ?? throw new InvalidOperationException("Auth:JwtAudience is not configured.");
var jwtKey = builder.Configuration["Auth:JwtKey"] ?? throw new InvalidOperationException("Auth:JwtKey is not configured.");

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
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
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Token"
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { { securityScheme, Array.Empty<string>() } });
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