using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rollout.Shared.Exceptions;

namespace Rollout.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? TryGetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");

        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    public static Guid GetUserIdOrThrow(this ClaimsPrincipal principal)
    {
        var userId = principal.TryGetUserId();

        if (userId.HasValue)
        {
            return userId.Value;
        }

        throw new AppException(StatusCodes.Status401Unauthorized, "unauthorized", "Invalid user identifier.");
    }
}