using System.Security.Claims;
using System.Text;
using Events.Application.EventsContracts;
using Events.Application.Services;
using Events.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddEventInfrastructure(builder.Configuration);

var authSection = builder.Configuration.GetSection("Auth");
var jwtKey = authSection.GetValue<string>("JwtKey")
            ?? throw new InvalidOperationException("Auth:JwtKey missing");
var issuer = authSection.GetValue<string>("JwtIssuer");
var audience = authSection.GetValue<string>("JwtAudience");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RollOut Event API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Token"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    };

    c.AddSecurityRequirement(securityRequirement);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Events API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "Events API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/events", async (
    CreateEventRequest request,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var ev = await service.CreateAsync(request, userId, ct);
    return Results.Ok(ev);
}).RequireAuthorization();

app.MapGet("/events/{id:long}", async (
    long id,
    IEventService service,
    CancellationToken ct) =>
{
    var ev = await service.GetAsync(id, ct);
    if (ev is null)
        return Results.NotFound();

    return Results.Ok(ev);
});

app.MapGet("/events/feed", async (
    string? city,
    DateTimeOffset? from,
    DateTimeOffset? to,
    int page,
    int pageSize,
    IEventService service,
    CancellationToken ct) =>
{
    var query = new FeedQuery(
        city,
        from,
        to,
        page <= 0 ? 1 : page,
        pageSize <= 0 ? 20 : pageSize);

    var items = await service.GetFeedAsync(query, ct);
    return Results.Ok(items);
});

app.MapPost("/events/{id:long}/join", async (
    long id,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.JoinAsync(id, userId, ct);
    return Results.Ok();
}).RequireAuthorization();

app.MapDelete("/events/{id:long}/join", async (
    long id,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.LeaveAsync(id, userId, ct);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/events/{id:long}/like", async (
    long id,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.LikeAsync(id, userId, ct);
    return Results.Ok();
}).RequireAuthorization();

app.MapDelete("/events/{id:long}/like", async (
    long id,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.UnlikeAsync(id, userId, ct);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/events/{id:long}/save", async (
    long id,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.SaveAsync(id, userId, ct);
    return Results.Ok();
}).RequireAuthorization();

app.MapDelete("/events/{id:long}/save", async (
    long id,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.UnsaveAsync(id, userId, ct);
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/events/me/events/created", async (
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var events = await service.GetMyCreatedEventsAsync(userId, ct);
    return Results.Ok(events);
}).RequireAuthorization();

app.MapGet("/events/me/events/going", async (
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var events = await service.GetMyGoingEventsAsync(userId, ct);
    return Results.Ok(events);
}).RequireAuthorization();

app.MapGet("/events/me/events/saved", async (
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var events = await service.GetMySavedEventsAsync(userId, ct);
    return Results.Ok(events);
}).RequireAuthorization();

app.MapGet("/events/me/events/liked", async (
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var events = await service.GetMyLikedEventsAsync(userId, ct);
    return Results.Ok(events);
}).RequireAuthorization();

app.MapGet("/events/{id:long}/members", async (
    long id,
    IEventService service,
    CancellationToken ct) =>
{
    var members = await service.GetMembersAsync(id, ct);
    return Results.Ok(members);
}).RequireAuthorization();

app.MapMethods("/events/{id:long}", new[] { "PATCH" }, async (
    long id,
    UpdateEventRequest request,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.UpdateAsync(id, userId, request, ct);
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/events/{id:long}/cancel", async (
    long id,
    ClaimsPrincipal user,
    IEventService service,
    CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    await service.CancelAsync(id, userId, ct);
    return Results.Ok();
}).RequireAuthorization();

app.Run();