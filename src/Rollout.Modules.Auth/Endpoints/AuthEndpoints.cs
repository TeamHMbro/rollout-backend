using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Rollout.Modules.Auth.Dtos;
using Rollout.Modules.Auth.Services;
using Rollout.Shared.Auth;

namespace Rollout.Modules.Auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.RegisterAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/login", async (LoginRequest request, AuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.LoginAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/refresh", async (RefreshRequest request, AuthService authService, CancellationToken cancellationToken) =>
        {
            var response = await authService.RefreshAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/logout", async (LogoutRequest request, AuthService authService, CancellationToken cancellationToken) =>
        {
            await authService.LogoutAsync(request, cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, AuthService authService, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await authService.GetMeAsync(userId, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        return builder;
    }
}