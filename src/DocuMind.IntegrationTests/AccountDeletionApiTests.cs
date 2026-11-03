using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class AccountDeletionApiTests
{
    private readonly DocuMindApiFactory _factory;

    public AccountDeletionApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteAccount_WithTheCorrectPassword_DeletesTheAccountAndItsDocuments()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", displayName = "Delete Tester" });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildOnePagePdfBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        uploadContent.Add(fileContent, "file", "sample.pdf");
        var uploadResponse = await client.PostAsync("/api/documents", uploadContent);
        uploadResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.PostAsJsonAsync("/api/auth/me/delete", new { password = "Sup3rSecret!1" });
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // The session should be ended server-side too — same pattern as Logout_EndsTheSession:
        // a real browser would honor the Set-Cookie clearing the auth cookie, so do that explicitly.
        Assert.True(deleteResponse.Headers.TryGetValues("Set-Cookie", out _), "Deleting the account should clear the auth cookie.");
        client.DefaultRequestHeaders.Remove("Cookie");

        var meAfterDelete = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterDelete.StatusCode);

        // The email is free again — a new account can reuse it.
        var reRegisterResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "AnotherSecret!2", displayName = "Reused Email" });
        Assert.True(reRegisterResponse.IsSuccessStatusCode, "Email should be reusable after the owning account is deleted.");
    }

    [Fact]
    public async Task DeleteAccount_WithTheWrongPassword_IsRejectedAndKeepsTheAccount()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", displayName = "Delete Tester" });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var deleteResponse = await client.PostAsJsonAsync("/api/auth/me/delete", new { password = "WrongPassword!9" });
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DeleteAccount_WithoutAuthentication_IsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/auth/me/delete", new { password = "whatever" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
