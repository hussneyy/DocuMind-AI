using System.Security.Claims;
using DocuMind.Api.Extensions;

namespace DocuMind.Tests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ParsesTheNameIdentifierClaim()
    {
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())]));

        Assert.Equal(userId, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ThrowsWhenClaimIsMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<InvalidOperationException>(() => principal.GetUserId());
    }
}
