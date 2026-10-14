using System.Security.Claims;

namespace DocuMind.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User id claim was not present on the authenticated principal.");
        return Guid.Parse(value);
    }
}
