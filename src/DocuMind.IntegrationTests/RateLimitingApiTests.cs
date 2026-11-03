using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DocuMind.IntegrationTests;

/// <summary>
/// Exercises the "upload" and "ai" rate limiters against a dedicated host with very low limits
/// (see <see cref="RateLimitedApiFactory"/>) — the main shared collection deliberately raises
/// these limits so its own many requests don't 429, so proving the limiters actually engage needs
/// this separate, tightly-limited instance instead.
///
/// Deliberately NOT <see cref="IClassFixture{T}"/>: that would share one factory — and therefore
/// one rate limiter's already-consumed permit count — across every test method in this class.
/// Implementing <see cref="IAsyncLifetime"/> directly means xUnit creates a fresh instance of this
/// class, and so a fresh factory with a zeroed-out limiter, per test method.
///
/// Placed in <see cref="DocuMindApiCollection"/> — not because it shares that collection's host,
/// but so xUnit runs it sequentially relative to it. Both this factory and <see cref="DocuMindApiFactory"/>
/// set process-wide environment variables in their constructors (the only way confirmed to
/// actually override this app's configuration — see DocuMindApiFactory's own doc comment); outside
/// this collection, xUnit could run this class's tests in parallel with the shared collection's,
/// and if this factory's low-limit env vars happened to be set while the shared factory's host was
/// still lazily building, the shared host would be permanently stuck with a 2-request limit for
/// its entire (otherwise sequential, intentionally high-limit) test run. Collection fixtures are
/// guaranteed built before any test in the collection runs, which forces the shared host to exist
/// — immune to later environment variable changes — before this class's tests ever execute.
/// </summary>
[Collection(DocuMindApiCollection.Name)]
public class RateLimitingApiTests : IAsyncLifetime
{
    private RateLimitedApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _factory = new RateLimitedApiFactory();
        await ((IAsyncLifetime)_factory).InitializeAsync();
    }

    public async Task DisposeAsync() => await ((IAsyncLifetime)_factory).DisposeAsync();

    [Fact]
    public async Task Ask_ExceedingTheAiRateLimit_Returns429()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        // RateLimiting__Ai__PermitLimit is 2 on this factory.
        var first = await client.PostAsJsonAsync("/api/chat/ask", new { question = "first question" });
        var second = await client.PostAsJsonAsync("/api/chat/ask", new { question = "second question" });
        var third = await client.PostAsJsonAsync("/api/chat/ask", new { question = "third question" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Upload_ExceedingTheUploadRateLimit_Returns429()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        // RateLimiting__Upload__PermitLimit is 2 on this factory.
        var first = await UploadAsync(client);
        var second = await UploadAsync(client);
        var third = await UploadAsync(client);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Summary_ExceedingTheAiRateLimit_Returns429()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        var uploadResponse = await UploadAsync(client);
        var document = await uploadResponse.Content.ReadFromJsonAsync<DocuMind.Application.Documents.DocumentDto>();

        // The same "ai" limiter as chat; this test gets its own fresh factory (see InitializeAsync
        // above), so the limit of 2 applies from scratch here regardless of other tests.
        var first = await client.GetAsync($"/api/documents/{document!.Id}/summary?type=Short");
        var second = await client.GetAsync($"/api/documents/{document.Id}/summary?type=Detailed");
        var third = await client.GetAsync($"/api/documents/{document.Id}/summary?type=KeyPoints");

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.True(first.StatusCode != HttpStatusCode.TooManyRequests);
        Assert.True(second.StatusCode != HttpStatusCode.TooManyRequests);
    }

    private static string NewEmail() => $"ratelimit-{Guid.NewGuid():N}@example.com";

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            firstName = "RateLimit",
            lastName = "TestUser",
            country = "US",
            phoneNumber = "+1 5550100000",
            preferredLanguage = "en",
        });
        response.EnsureSuccessStatusCode();

        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            client.DefaultRequestHeaders.Add("Cookie", cookies.First().Split(';')[0]);
        }
        else
        {
            throw new InvalidOperationException("Register did not return an auth cookie.");
        }
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildOnePagePdfBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", $"{Guid.NewGuid():N}.pdf");
        return client.PostAsync("/api/documents", content);
    }
}
