using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DocuMind.Application.Auth;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class TwoFactorApiTests
{
    private readonly DocuMindApiFactory _factory;

    public TwoFactorApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Setup_ThenEnableWithAValidCode_TurnsOnTwoFactorAndReturnsRecoveryCodes()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "TwoFactor", lastName = "Tester", country = "US", phoneNumber = "+1 5550100000", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        ApplyCookies(client, registerResponse);

        var setupResponse = await client.PostAsync("/api/auth/2fa/setup", content: null);
        setupResponse.EnsureSuccessStatusCode();
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupResult>();
        Assert.False(string.IsNullOrWhiteSpace(setup!.SharedKey));
        Assert.StartsWith("otpauth://totp/", setup.AuthenticatorUri);

        var code = TotpTestHelper.GenerateCode(setup.SharedKey);
        var enableResponse = await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code });
        enableResponse.EnsureSuccessStatusCode();
        var enableBody = await enableResponse.Content.ReadFromJsonAsync<JsonElement>();
        var recoveryCodes = enableBody.GetProperty("recoveryCodes").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.Equal(10, recoveryCodes.Count);

        var meResponse = await client.GetAsync("/api/auth/me");
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.True(me!.TwoFactorEnabled);
    }

    [Fact]
    public async Task Enable_WithAnInvalidCode_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "TwoFactor", lastName = "Tester", country = "US", phoneNumber = "+1 5550100000", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        ApplyCookies(client, registerResponse);

        await client.PostAsync("/api/auth/2fa/setup", content: null);

        var response = await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithTwoFactorEnabled_RequiresASecondStep_ThenSucceedsWithAValidCode()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "TwoFactor", lastName = "Tester", country = "US", phoneNumber = "+1 5550100000", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        ApplyCookies(client, registerResponse);

        var sharedKey = await EnableTwoFactorAsync(client);

        // Fresh client: logging back in from scratch, as a real browser session would.
        var freshClient = _factory.CreateClient();
        freshClient.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var loginResponse = await freshClient.PostAsJsonAsync("/api/auth/login", new { email, password = "Sup3rSecret!1" });
        loginResponse.EnsureSuccessStatusCode();
        ApplyCookies(freshClient, loginResponse);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(loginBody.GetProperty("requiresTwoFactor").GetBoolean());

        // Not signed in yet — /me should still be unauthorized at this point.
        var meBeforeCode = await freshClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meBeforeCode.StatusCode);

        var code = TotpTestHelper.GenerateCode(sharedKey);
        var completeResponse = await freshClient.PostAsJsonAsync("/api/auth/login/2fa", new { code });
        completeResponse.EnsureSuccessStatusCode();
        ApplyCookies(freshClient, completeResponse);

        var meAfterCode = await freshClient.GetAsync("/api/auth/me");
        meAfterCode.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_WithTwoFactorEnabled_RejectsAnInvalidCode()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "TwoFactor", lastName = "Tester", country = "US", phoneNumber = "+1 5550100000", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        ApplyCookies(client, registerResponse);

        await EnableTwoFactorAsync(client);

        var freshClient = _factory.CreateClient();
        freshClient.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var loginResponse = await freshClient.PostAsJsonAsync("/api/auth/login", new { email, password = "Sup3rSecret!1" });
        ApplyCookies(freshClient, loginResponse);

        var completeResponse = await freshClient.PostAsJsonAsync("/api/auth/login/2fa", new { code = "000000" });
        Assert.Equal(HttpStatusCode.Unauthorized, completeResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithARecoveryCode_SucceedsOnceThenRejectsReuse()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "TwoFactor", lastName = "Tester", country = "US", phoneNumber = "+1 5550100000", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        ApplyCookies(client, registerResponse);

        var setupResponse = await client.PostAsync("/api/auth/2fa/setup", content: null);
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupResult>();
        var code = TotpTestHelper.GenerateCode(setup!.SharedKey);
        var enableResponse = await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code });
        var enableBody = await enableResponse.Content.ReadFromJsonAsync<JsonElement>();
        var recoveryCode = enableBody.GetProperty("recoveryCodes").EnumerateArray().First().GetString()!;

        var freshClient = _factory.CreateClient();
        freshClient.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var loginResponse = await freshClient.PostAsJsonAsync("/api/auth/login", new { email, password = "Sup3rSecret!1" });
        ApplyCookies(freshClient, loginResponse);

        var firstUse = await freshClient.PostAsJsonAsync("/api/auth/login/2fa-recovery", new { recoveryCode });
        firstUse.EnsureSuccessStatusCode();
        ApplyCookies(freshClient, firstUse);
        var meAfterRecovery = await freshClient.GetAsync("/api/auth/me");
        meAfterRecovery.EnsureSuccessStatusCode();

        // Second login attempt with the SAME recovery code should fail — it's single-use.
        var secondClient = _factory.CreateClient();
        secondClient.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var secondLoginResponse = await secondClient.PostAsJsonAsync("/api/auth/login", new { email, password = "Sup3rSecret!1" });
        ApplyCookies(secondClient, secondLoginResponse);
        var secondUse = await secondClient.PostAsJsonAsync("/api/auth/login/2fa-recovery", new { recoveryCode });
        Assert.Equal(HttpStatusCode.Unauthorized, secondUse.StatusCode);
    }

    [Fact]
    public async Task Disable_WithTheCorrectPassword_TurnsOffTwoFactorSoLoginNoLongerRequiresIt()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "TwoFactor", lastName = "Tester", country = "US", phoneNumber = "+1 5550100000", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        ApplyCookies(client, registerResponse);

        await EnableTwoFactorAsync(client);

        var disableResponse = await client.PostAsJsonAsync("/api/auth/2fa/disable", new { password = "Sup3rSecret!1" });
        disableResponse.EnsureSuccessStatusCode();
        var disabled = await disableResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.False(disabled!.TwoFactorEnabled);

        var freshClient = _factory.CreateClient();
        freshClient.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var loginResponse = await freshClient.PostAsJsonAsync("/api/auth/login", new { email, password = "Sup3rSecret!1" });
        loginResponse.EnsureSuccessStatusCode();
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(loginBody.TryGetProperty("requiresTwoFactor", out var requiresTwoFactor) && requiresTwoFactor.GetBoolean());
    }

    [Fact]
    public async Task Disable_WithTheWrongPassword_IsRejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Sup3rSecret!1", firstName = "TwoFactor", lastName = "Tester", country = "US", phoneNumber = "+1 5550100000", preferredLanguage = "en" });
        registerResponse.EnsureSuccessStatusCode();
        ApplyCookies(client, registerResponse);

        await EnableTwoFactorAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/2fa/disable", new { password = "WrongPassword!9" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<string> EnableTwoFactorAsync(HttpClient client)
    {
        var setupResponse = await client.PostAsync("/api/auth/2fa/setup", content: null);
        var setup = await setupResponse.Content.ReadFromJsonAsync<TwoFactorSetupResult>();
        var code = TotpTestHelper.GenerateCode(setup!.SharedKey);
        var enableResponse = await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code });
        enableResponse.EnsureSuccessStatusCode();
        return setup.SharedKey;
    }

    /// <summary>
    /// Merges a response's Set-Cookie headers into the client's outgoing Cookie header, the way a
    /// real browser's cookie jar would. Necessary here (unlike the simpler single-cookie helper in
    /// AuthApiTests) because completing a two-factor sign-in returns multiple Set-Cookie headers at
    /// once — the new auth cookie plus expiring ones for the two-factor-pending state — so blindly
    /// taking "the first" Set-Cookie can pick a cleared cookie instead of the real session.
    /// </summary>
    private static void ApplyCookies(HttpClient client, HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            return;
        }

        var jar = new Dictionary<string, string>();
        if (client.DefaultRequestHeaders.TryGetValues("Cookie", out var existing))
        {
            foreach (var pair in existing.SelectMany(h => h.Split(';')))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2)
                {
                    jar[kv[0].Trim()] = kv[1].Trim();
                }
            }
        }

        foreach (var setCookie in setCookieHeaders)
        {
            var firstPart = setCookie.Split(';')[0];
            var kv = firstPart.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            var name = kv[0].Trim();
            var value = kv[1].Trim();
            var isExpired = setCookie.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase);
            if (isExpired || string.IsNullOrEmpty(value))
            {
                jar.Remove(name);
            }
            else
            {
                jar[name] = value;
            }
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        if (jar.Count > 0)
        {
            client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", jar.Select(kv => $"{kv.Key}={kv.Value}")));
        }
    }
}
