using System.Security.Claims;
using System.Text;
using Auth.Application.AuthContracts;
using Auth.Application.Services;
using Auth.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthInfrastructure(builder.Configuration);

var authSection = builder.Configuration.GetSection("Auth");
var jwtKey = authSection.GetValue<string>("JwtKey") 
            ?? throw new InvalidOperationException("Auth:JwtKey missing");
var issuer = authSection.GetValue<string>("JwtIssuer");
var audience = authSection.GetValue<string>("JwtAudience");

// JWT
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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RollOut Auth API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT токен в формате: Bearer {token}"
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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RollOut Auth API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapPost("/auth/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
{
    var tokens = await authService.RegisterAsync(request, ct);
    return Results.Ok(tokens);
});

app.MapPost("/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
{
    var tokens = await authService.LoginAsync(request, ct);
    return Results.Ok(tokens);
});

app.MapPost("/auth/refresh", async (RefreshTokenRequest request, IAuthService authService, CancellationToken ct) =>
{
    var tokens = await authService.RefreshAsync(request, ct);
    return Results.Ok(tokens);
});

app.MapGet("/auth/me", async (ClaimsPrincipal user, IAuthService authService, CancellationToken ct) =>
{
    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? user.FindFirstValue("sub");

    if (!Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();

    var me = await authService.GetMeAsync(userId, ct);
    if (me is null)
        return Results.NotFound();

    return Results.Ok(me);
}).RequireAuthorization();

app.Run();
