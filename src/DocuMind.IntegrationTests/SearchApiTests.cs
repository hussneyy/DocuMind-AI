using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Application.Documents;
using DocuMind.Application.Search;
using DocuMind.Domain.Enums;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class SearchApiTests
{
    private readonly DocuMindApiFactory _factory;

    public SearchApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SemanticSearch_FindsTheChunkThatSharesVocabularyWithTheQuery()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var document = await UploadAndWaitForIndexedAsync(client,
            "Employee onboarding covers desk setup and introductions.",
            "The exam registration deadline is March fifteenth this year.",
            "The appeals process for disputed grades takes two weeks.");

        var response = await client.PostAsJsonAsync("/api/search", new
        {
            query = "exam registration deadline",
            mode = "Semantic",
        });
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SearchResultItem>>();
        Assert.NotNull(results);
        Assert.NotEmpty(results!);
        Assert.Equal(document.Id, results![0].DocumentId);
        Assert.True(results[0].PageStart <= 2 && results[0].PageEnd >= 2);
    }

    [Fact]
    public async Task KeywordSearch_FindsAnExactPhraseMatch()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        await UploadAndWaitForIndexedAsync(client,
            "This page discusses general office policies and conduct.",
            "The appeals process for disputed grades takes two weeks to complete.");

        var response = await client.PostAsJsonAsync("/api/search", new
        {
            query = "appeals process",
            mode = "Keyword",
        });
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SearchResultItem>>();
        Assert.NotNull(results);
        Assert.Contains(results!, r => r.Excerpt.Contains("appeals process", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HybridSearch_CombinesSemanticAndKeywordResults()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        await UploadAndWaitForIndexedAsync(client,
            "Library hours are posted at the main entrance.",
            "The exam registration deadline is March fifteenth this year.");

        var response = await client.PostAsJsonAsync("/api/search", new
        {
            query = "exam registration deadline",
            mode = "Hybrid",
        });
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SearchResultItem>>();
        Assert.NotNull(results);
        Assert.NotEmpty(results!);
    }

    [Fact]
    public async Task Search_ForATotallyUnrelatedQuery_ReturnsNoResultsRatherThanAWeakMatch()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        await UploadAndWaitForIndexedAsync(client, "The exam registration deadline is March fifteenth this year.");

        var response = await client.PostAsJsonAsync("/api/search", new
        {
            query = "zzqx wobflarp nonsense gibberish unrelated",
            mode = "Semantic",
        });
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SearchResultItem>>();
        Assert.NotNull(results);
        Assert.Empty(results!);
    }

    [Fact]
    public async Task Search_ScopedToASpecificDocument_ExcludesOtherDocuments()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var docA = await UploadAndWaitForIndexedAsync(client, "The exam registration deadline is March fifteenth.");
        var docB = await UploadAndWaitForIndexedAsync(client, "The exam registration deadline is also mentioned here.");

        var response = await client.PostAsJsonAsync("/api/search", new
        {
            query = "exam registration deadline",
            mode = "Semantic",
            documentIds = new[] { docA.Id },
        });
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SearchResultItem>>();
        Assert.NotNull(results);
        Assert.NotEmpty(results!);
        Assert.All(results!, r => Assert.Equal(docA.Id, r.DocumentId));
        Assert.DoesNotContain(results, r => r.DocumentId == docB.Id);
    }

    [Fact]
    public async Task Search_NeverReturnsAnotherUsersChunks()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        await UploadAndWaitForIndexedAsync(ownerClient, "This confidential policy document mentions a secret budget figure.");

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var response = await otherClient.PostAsJsonAsync("/api/search", new
        {
            query = "confidential policy secret budget",
            mode = "Semantic",
        });
        response.EnsureSuccessStatusCode();

        var results = await response.Content.ReadFromJsonAsync<List<SearchResultItem>>();
        Assert.NotNull(results);
        Assert.Empty(results!);
    }

    [Fact]
    public async Task Search_WithAnEmptyQuery_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var response = await client.PostAsJsonAsync("/api/search", new { query = "", mode = "Hybrid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/search", new { query = "test", mode = "Hybrid" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string NewEmail() => $"search-{Guid.NewGuid():N}@example.com";

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            firstName = "Search",
            lastName = "TestUser",
            country = "US",
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

    private static async Task<DocumentDto> UploadAndWaitForIndexedAsync(HttpClient client, params string[] pageTexts)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildMultiPagePdfBytes(pageTexts));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", $"{Guid.NewGuid():N}.pdf");

        var uploadResponse = await client.PostAsync("/api/documents", content);
        uploadResponse.EnsureSuccessStatusCode();
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var getResponse = await client.GetAsync($"/api/documents/{uploaded!.Id}");
            getResponse.EnsureSuccessStatusCode();
            var dto = await getResponse.Content.ReadFromJsonAsync<DocumentDto>();

            if (dto!.ProcessingStatus == DocumentProcessingStatus.Indexed) return dto;
            if (dto.ProcessingStatus == DocumentProcessingStatus.Failed)
            {
                throw new InvalidOperationException($"Document unexpectedly failed to index: {dto.ProcessingError}");
            }

            await Task.Delay(300);
        }

        throw new TimeoutException("Document did not finish indexing within the timeout.");
    }
}
