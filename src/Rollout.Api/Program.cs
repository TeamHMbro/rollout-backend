using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Rollout.Api.HealthChecks;
using Rollout.Api.Seeding;
using Rollout.Modules.Auth;
using Rollout.Modules.Auth.Data;
using Rollout.Modules.Events;
using Rollout.Modules.Events.Data;
using Rollout.Modules.Users;
using Rollout.Modules.Users.Data;
using Rollout.Shared.Auth;
using Rollout.Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.Configure<DevSeedOptions>(builder.Configuration.GetSection("Seeding"));
builder.Services.AddScoped<DevDataSeeder>();

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsOrigins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            return;
        }

        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddEventsModule(builder.Configuration);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var authSection = builder.Configuration.GetSection("Auth");
var jwtOptions = authSection.Get<JwtOptions>() ?? throw new InvalidOperationException("Auth section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.IncludeErrorDetails = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.JwtKey)),
            ValidateLifetime = true,
            NameClaimType = JwtRegisteredClaimNames.Sub,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rollout API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT bearer token"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseForwardedHeaders();
app.UseCors("Frontend");

using (var scope = app.Services.CreateScope())
{
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    var eventsDb = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

    if (authDb.Database.IsRelational())
    {
        authDb.Database.Migrate();
    }

    if (usersDb.Database.IsRelational())
    {
        usersDb.Database.Migrate();
    }

    if (eventsDb.Database.IsRelational())
    {
        eventsDb.Database.Migrate();
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DevDataSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "Rollout API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "rollout-api",
    version = "v1"
}));

app.MapHealthChecks("/health");
app.MapAuthModule();
app.MapUsersModule();
app.MapEventsModule();

app.Run();

public partial class Program;