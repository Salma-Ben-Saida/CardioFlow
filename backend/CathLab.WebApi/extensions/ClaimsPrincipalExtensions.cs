using System.Security.Claims;

namespace CathLab.WebApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// MapInboundClaims=false → literal JWT claim key "nameid", not
    /// ClaimTypes.NameIdentifier. Shared here so ProceduresController and
    /// ReportsController (and any future controller) don't each carry
    /// their own copy of this logic.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("nameid");
        if (Guid.TryParse(claim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("No authenticated user found.");
    }
}