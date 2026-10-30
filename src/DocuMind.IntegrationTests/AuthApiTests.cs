using System.Net;
using System.Net.Http.Json;
using DocuMind.Application.Auth;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class AuthApiTests
{
    private readonly DocuMindApiFactory _factory;

    public AuthApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ThenMe_ReturnsTheRegisteredProfile()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            displayName = "Ada Lovelace",
        });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();

        Assert.Equal(email, me!.Email);
        Assert.Equal("Ada Lovelace", me.DisplayName);
    }

    [Fact]
    public async Task Register_WithAnAlreadyUsedEmail_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var first = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", displayName = "First" });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", displayName = "Second" });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsGenericInvalidCredentialsError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            displayName = "Test User",
        });
        registerResponse.EnsureSuccessStatusCode();

        var anonymousClient = _factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword!1",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            displayName = "Test User",
        });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        // A real browser would honor the Set-Cookie here, which clears the auth cookie. This
        // HttpClient manages cookies manually (see AttachAuthCookie), so do the same explicitly.
        Assert.True(logoutResponse.Headers.TryGetValues("Set-Cookie", out _), "Logout should clear the auth cookie.");
        client.DefaultRequestHeaders.Remove("Cookie");

        var meAfterLogout = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);
    }

    [Fact]
    public async Task UpdateDisplayName_ThenMe_ReturnsTheUpdatedProfile()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            displayName = "Original Name",
        });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var patchResponse = await client.PatchAsJsonAsync("/api/auth/me", new { displayName = "Updated Name" });
        patchResponse.EnsureSuccessStatusCode();
        var patched = await patchResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal("Updated Name", patched!.DisplayName);

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal("Updated Name", me!.DisplayName);
    }

    [Fact]
    public async Task UpdateDisplayName_WithoutAuthentication_IsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var patchResponse = await client.PatchAsJsonAsync("/api/auth/me", new { displayName = "Nobody" });
        Assert.Equal(HttpStatusCode.Unauthorized, patchResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateDisplayName_WithAnEmptyName_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            displayName = "Original Name",
        });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var patchResponse = await client.PatchAsJsonAsync("/api/auth/me", new { displayName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    private static void AttachAuthCookie(HttpClient client, HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            throw new InvalidOperationException("Response did not return an auth cookie.");
        }

        client.DefaultRequestHeaders.Add("Cookie", cookies.First().Split(';')[0]);
    }
}
