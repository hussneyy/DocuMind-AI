using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Application.Documents;
using DocuMind.Application.Summaries;
using DocuMind.Domain.Enums;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class DocumentSummaryAndFileApiTests
{
    private readonly DocuMindApiFactory _factory;

    public DocumentSummaryAndFileApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSummary_ForAnIndexedDocument_ReturnsAGroundedSummary()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        var document = await UploadAndWaitForIndexedAsync(client, "The exam registration deadline is March fifteenth this year.");

        var response = await client.GetAsync($"/api/documents/{document.Id}/summary?type=Short");
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<SummaryDto>();
        Assert.NotNull(dto);
        Assert.Equal("Short", dto!.SummaryType);
        Assert.False(string.IsNullOrWhiteSpace(dto.Content));
        Assert.True(dto.SourceChunkCount > 0);
    }

    [Fact]
    public async Task GetSummary_CalledTwiceWithoutRegenerate_ReturnsTheSameCachedResult()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        var document = await UploadAndWaitForIndexedAsync(client, "Library hours are posted at the main entrance.");

        var first = await (await client.GetAsync($"/api/documents/{document.Id}/summary?type=Detailed")).Content.ReadFromJsonAsync<SummaryDto>();
        var second = await (await client.GetAsync($"/api/documents/{document.Id}/summary?type=Detailed")).Content.ReadFromJsonAsync<SummaryDto>();

        // Compare with a small tolerance rather than bit-exact equality: a PostgreSQL
        // "timestamp with time zone" round-trips at microsecond precision, so two independent
        // reads of the very same stored value can differ by a sub-microsecond rounding artifact
        // that has nothing to do with whether the cache was actually reused.
        Assert.True(Math.Abs((first!.GeneratedAt - second!.GeneratedAt).TotalMilliseconds) < 1,
            $"Expected the cached GeneratedAt to be reused, got {first.GeneratedAt:O} then {second.GeneratedAt:O}");
        Assert.Equal(first.Content, second.Content);
    }

    [Fact]
    public async Task GetSummary_WithRegenerateTrue_ProducesAFreshGeneratedAt()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        var document = await UploadAndWaitForIndexedAsync(client, "Office hours are posted on the door.");

        var first = await (await client.GetAsync($"/api/documents/{document.Id}/summary?type=KeyPoints")).Content.ReadFromJsonAsync<SummaryDto>();
        await Task.Delay(50);
        var second = await (await client.GetAsync($"/api/documents/{document.Id}/summary?type=KeyPoints&regenerate=true")).Content.ReadFromJsonAsync<SummaryDto>();

        Assert.True(second!.GeneratedAt > first!.GeneratedAt);
    }

    [Fact]
    public async Task GetSummary_EachTypeIsCachedIndependently()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        var document = await UploadAndWaitForIndexedAsync(client, "Parking permits are issued at the front desk.");

        var shortDto = await (await client.GetAsync($"/api/documents/{document.Id}/summary?type=Short")).Content.ReadFromJsonAsync<SummaryDto>();
        var detailedDto = await (await client.GetAsync($"/api/documents/{document.Id}/summary?type=Detailed")).Content.ReadFromJsonAsync<SummaryDto>();

        Assert.Equal("Short", shortDto!.SummaryType);
        Assert.Equal("Detailed", detailedDto!.SummaryType);
    }

    [Fact]
    public async Task GetSummary_WithAnInvalidType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        var document = await UploadAndWaitForIndexedAsync(client, "Some content.");

        var response = await client.GetAsync($"/api/documents/{document.Id}/summary?type=Nonsense");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_ForADocumentThatFailedToIndex_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildBlankPageBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", $"{Guid.NewGuid():N}.pdf");

        var uploadResponse = await client.PostAsync("/api/documents", content);
        uploadResponse.EnsureSuccessStatusCode();
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var document = await WaitForTerminalStatusAsync(client, uploaded!.Id);
        Assert.Equal(DocumentProcessingStatus.Failed, document.ProcessingStatus);

        var response = await client.GetAsync($"/api/documents/{document.Id}/summary?type=Short");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_ForAnotherUsersDocument_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        var document = await UploadAndWaitForIndexedAsync(ownerClient, "Confidential budget figures.");

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var response = await otherClient.GetAsync($"/api/documents/{document.Id}/summary?type=Short");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFile_ReturnsTheOriginalPdfBytesWithTheCorrectContentType()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var originalBytes = TestPdf.BuildMultiPagePdfBytes("Page one text.", "Page two text.");
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(originalBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", $"{Guid.NewGuid():N}.pdf");

        var uploadResponse = await client.PostAsync("/api/documents", content);
        uploadResponse.EnsureSuccessStatusCode();
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var response = await client.GetAsync($"/api/documents/{uploaded!.Id}/file");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(originalBytes, bytes);
    }

    [Fact]
    public async Task GetFile_ForAnotherUsersDocument_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildOnePagePdfBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", $"{Guid.NewGuid():N}.pdf");
        var uploadResponse = await ownerClient.PostAsync("/api/documents", content);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var response = await otherClient.GetAsync($"/api/documents/{uploaded!.Id}/file");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFile_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}/file");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string NewEmail() => $"summary-{Guid.NewGuid():N}@example.com";

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            displayName = "Summary Test User",
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

        var document = await WaitForTerminalStatusAsync(client, uploaded!.Id);
        if (document.ProcessingStatus != DocumentProcessingStatus.Indexed)
        {
            throw new InvalidOperationException($"Document unexpectedly failed to index: {document.ProcessingError}");
        }

        return document;
    }

    private static async Task<DocumentDto> WaitForTerminalStatusAsync(HttpClient client, Guid documentId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var getResponse = await client.GetAsync($"/api/documents/{documentId}");
            getResponse.EnsureSuccessStatusCode();
            var dto = await getResponse.Content.ReadFromJsonAsync<DocumentDto>();

            if (dto!.ProcessingStatus is DocumentProcessingStatus.Indexed or DocumentProcessingStatus.Failed)
            {
                return dto;
            }

            await Task.Delay(300);
        }

        throw new TimeoutException("Document did not reach a terminal status within the timeout.");
    }
}
