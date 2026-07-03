using System.Security.Claims;

namespace SIMS.Web;

public static class ClaimsPrincipalExtensions
{
    // The authenticated user's SIMS Id (stored as NameIdentifier on sign-in).
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : 0;
    }
}
