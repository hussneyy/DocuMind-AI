using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
            firstName = "Ada",
            lastName = "Lovelace",
            country = "GB",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();

        Assert.Equal(email, me!.Email);
        Assert.Equal("Ada", me.FirstName);
        Assert.Equal("Lovelace", me.LastName);
        Assert.Equal("Ada Lovelace", me.DisplayName);
        Assert.Equal("GB", me.Country);
        Assert.Equal("en", me.PreferredLanguage);
    }

    [Fact]
    public async Task Register_WithAnUnsupportedLanguage_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            firstName = "Ada",
            lastName = "Lovelace",
            country = "GB",
            preferredLanguage = "xx",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithoutACountry_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            firstName = "Ada",
            lastName = "Lovelace",
            preferredLanguage = "en",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAnAlreadyUsedEmail_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var first = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "First", lastName = "User", country = "US", preferredLanguage = "en" });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "Second", lastName = "User", country = "US", preferredLanguage = "en" });
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
            firstName = "Test",
            lastName = "User",
            country = "US",
            preferredLanguage = "en",
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
            firstName = "Test",
            lastName = "User",
            country = "US",
            preferredLanguage = "en",
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
            firstName = "Original",
            lastName = "Name",
            country = "US",
            preferredLanguage = "en",
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
            firstName = "Original",
            lastName = "Name",
            country = "US",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var patchResponse = await client.PatchAsJsonAsync("/api/auth/me", new { displayName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithBioLocationAndPhoneNumber_PersistsAllFields()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "Original", lastName = "Name", country = "US", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        var patchResponse = await client.PatchAsJsonAsync("/api/auth/me", new
        {
            displayName = "Original Name",
            bio = "Building things with PDFs.",
            location = "Berlin",
            phoneNumber = "+49 123 4567",
        });
        patchResponse.EnsureSuccessStatusCode();
        var patched = await patchResponse.Content.ReadFromJsonAsync<CurrentUserDto>();

        Assert.Equal("Building things with PDFs.", patched!.Bio);
        Assert.Equal("Berlin", patched.Location);
        Assert.Equal("+49 123 4567", patched.PhoneNumber);

        var meResponse = await client.GetAsync("/api/auth/me");
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal("Building things with PDFs.", me!.Bio);
    }

    [Fact]
    public async Task UploadAvatar_ThenGetAndDelete_RoundTripsCorrectly()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "Avatar", lastName = "Tester", country = "US", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(OnePixelPngBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "avatar.png");

        var uploadResponse = await client.PostAsync("/api/auth/me/avatar", content);
        uploadResponse.EnsureSuccessStatusCode();
        var afterUpload = await uploadResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.True(afterUpload!.HasAvatar);

        var getResponse = await client.GetAsync("/api/auth/me/avatar");
        getResponse.EnsureSuccessStatusCode();
        Assert.Equal("image/png", getResponse.Content.Headers.ContentType?.MediaType);

        var deleteResponse = await client.DeleteAsync("/api/auth/me/avatar");
        deleteResponse.EnsureSuccessStatusCode();
        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.False(afterDelete!.HasAvatar);

        var getAfterDelete = await client.GetAsync("/api/auth/me/avatar");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_RejectsAFileThatIsNotActuallyAnImage()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "Avatar", lastName = "Tester", country = "US", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        AttachAuthCookie(client, registerResponse);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("this is definitely not an image"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "fake.png");

        var response = await client.PostAsync("/api/auth/me/avatar", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ThenResetPassword_AllowsLoggingInWithTheNewPassword()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "Reset", lastName = "Tester", country = "US", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();

        var forgotResponse = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        forgotResponse.EnsureSuccessStatusCode();
        var forgotBody = await forgotResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(forgotBody.GetProperty("accountExists").GetBoolean());
        var resetLink = forgotBody.GetProperty("resetLink").GetString();
        Assert.NotNull(resetLink);

        var uri = new Uri(resetLink!);
        var queryParams = uri.Query.TrimStart('?').Split('&')
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
        var token = queryParams["token"];
        Assert.NotNull(token);

        var resetResponse = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token,
            newPassword = "BrandNewSecret!2",
        });
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "BrandNewSecret!2" });
        loginResponse.EnsureSuccessStatusCode();

        var oldPasswordLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Sup3rSecret!1" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithAnUnknownEmail_StillReturns200ButWithNoResetLink()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = $"nobody-{Guid.NewGuid():N}@example.com" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(body.GetProperty("accountExists").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("resetLink").ValueKind);
    }

    [Fact]
    public async Task ResetPassword_WithAnInvalidToken_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "Reset", lastName = "Tester", country = "US", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = "not-a-real-token",
            newPassword = "BrandNewSecret!2",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static byte[] OnePixelPngBytes() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    private static void AttachAuthCookie(HttpClient client, HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            throw new InvalidOperationException("Response did not return an auth cookie.");
        }

        client.DefaultRequestHeaders.Add("Cookie", cookies.First().Split(';')[0]);
    }
}
