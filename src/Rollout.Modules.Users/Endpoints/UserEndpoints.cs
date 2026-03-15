using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Rollout.Modules.Users.Dtos;
using Rollout.Modules.Users.Services;
using Rollout.Shared.Auth;

namespace Rollout.Modules.Users.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, UserProfileService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await service.GetMyProfileAsync(userId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPatch("/me", async (ClaimsPrincipal principal, UpdateMyProfileRequest request, UserProfileService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await service.UpdateMyProfileAsync(userId, request, cancellationToken);
            return Results.Ok(response);
        });

        return builder;
    }
}