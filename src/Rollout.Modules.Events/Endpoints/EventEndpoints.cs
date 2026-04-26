using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Rollout.Modules.Events.Dtos;
using Rollout.Modules.Events.Services;
using Rollout.Shared.Auth;

namespace Rollout.Modules.Events.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/events").WithTags("Events");

        group.MapPost("/", async (ClaimsPrincipal principal, CreateEventRequest request, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await service.CreateAsync(userId, request, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapGet("/feed", async (
            ClaimsPrincipal principal,
            string? q,
            string? city,
            string? category,
            DateTime? from,
            DateTime? to,
            bool? onlyAvailable,
            string? sort,
            int page,
            int pageSize,
            EventService service,
            CancellationToken cancellationToken) =>
        {
            var currentUserId = principal.TryGetUserId();
            var response = await service.GetFeedAsync(q, city, category, from, to, onlyAvailable, sort, page, pageSize, currentUserId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/{id:long}", async (long id, ClaimsPrincipal principal, EventService service, CancellationToken cancellationToken) =>
        {
            var currentUserId = principal.TryGetUserId();
            var response = await service.GetByIdAsync(id, currentUserId, cancellationToken);
            return response is null ? Results.NotFound() : Results.Ok(response);
        });

        group.MapPatch("/{id:long}", async (long id, ClaimsPrincipal principal, UpdateEventRequest request, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await service.UpdateAsync(id, userId, request, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapPost("/{id:long}/cancel", async (long id, ClaimsPrincipal principal, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            await service.CancelAsync(id, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/{id:long}/join", async (long id, ClaimsPrincipal principal, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            await service.JoinAsync(id, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapDelete("/{id:long}/join", async (long id, ClaimsPrincipal principal, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            await service.LeaveAsync(id, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/{id:long}/members", async (long id, EventService service, CancellationToken cancellationToken) =>
        {
            var response = await service.GetMembersAsync(id, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/me/created", async (ClaimsPrincipal principal, int page, int pageSize, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await service.GetMyCreatedAsync(userId, page, pageSize, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapGet("/me/joined", async (ClaimsPrincipal principal, int page, int pageSize, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await service.GetMyJoinedAsync(userId, page, pageSize, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        group.MapGet("/me/calendar", async (ClaimsPrincipal principal, string status, int page, int pageSize, EventService service, CancellationToken cancellationToken) =>
        {
            var userId = principal.GetUserIdOrThrow();
            var response = await service.GetMyCalendarAsync(userId, status, page, pageSize, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        return builder;
    }
}