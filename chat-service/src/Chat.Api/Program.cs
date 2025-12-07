using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Chat.Api;
using Chat.Application.Abstractions;
using Chat.Application.EventMessages;
using Chat.Application.Services;
using Chat.Domain;
using Chat.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddChatInfrastructure(builder.Configuration);
builder.Services.AddSignalR();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var jwtIssuer = builder.Configuration["Auth:JwtIssuer"] ?? throw new InvalidOperationException("Auth:JwtIssuer is not configured");
var jwtAudience = builder.Configuration["Auth:JwtAudience"] ?? throw new InvalidOperationException("Auth:JwtAudience is not configured");
var jwtKey = builder.Configuration["Auth:JwtKey"] ?? throw new InvalidOperationException("Auth:JwtKey is not configured");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rollout Chat API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            Array.Empty<string>()
        }
    };

    c.AddSecurityRequirement(securityRequirement);
});

var app = builder.Build();

app.UseGlobalErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

Guid GetUserId(ClaimsPrincipal user)
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        throw new DomainException("Chat.InvalidUserId");
    return userId;
}

string GetBearerToken(HttpContext httpContext)
{
    var header = httpContext.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(header))
        throw new DomainException("Chat.Unauthorized");
    if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        header = header["Bearer ".Length..];
    if (string.IsNullOrWhiteSpace(header))
        throw new DomainException("Chat.Unauthorized");
    return header.Trim();
}

app.MapGet("/events/{eventId:long}/messages", async (long eventId, int page, int pageSize, IChatService chatService, CancellationToken ct) =>
{
    var result = await chatService.GetEventMessagesAsync(eventId, page, pageSize, ct);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapGet("/events/{eventId:long}/messages/recent", async (long eventId, int limit, IChatService chatService, CancellationToken ct) =>
{
    var result = await chatService.GetRecentEventMessagesAsync(eventId, limit, ct);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapPost("/events/{eventId:long}/messages", async (long eventId, SendEventMessageRequest request, ClaimsPrincipal user, HttpContext httpContext, IEventAccessService eventAccessService, IChatService chatService, CancellationToken ct) =>
{
    var userId = GetUserId(user);
    var token = GetBearerToken(httpContext);
    await eventAccessService.EnsureUserCanWriteAsync(eventId, userId, token, ct);
    var result = await chatService.SendEventMessageAsync(eventId, userId, request, ct);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapPatch("/messages/{messageId:long}", async (long messageId, SendEventMessageRequest request, ClaimsPrincipal user, IChatService chatService, CancellationToken ct) =>
{
    var userId = GetUserId(user);
    var result = await chatService.UpdateEventMessageAsync(messageId, userId, request.Content, ct);
    return Results.Ok(result);
}).RequireAuthorization();

app.MapDelete("/messages/{messageId:long}", async (long messageId, ClaimsPrincipal user, IChatService chatService, CancellationToken ct) =>
{
    var userId = GetUserId(user);
    await chatService.DeleteEventMessageAsync(messageId, userId, ct);
    return Results.NoContent();
}).RequireAuthorization();

app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

app.Run();
