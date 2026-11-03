using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Application.Documents;
using DocuMind.Domain.Enums;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class DocumentIndexingTests
{
    private readonly DocuMindApiFactory _factory;

    public DocumentIndexingTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UploadedDocument_IsIndexedByTheBackgroundWorker_WithCorrectPageReferences()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var content = BuildPdfUpload(
            "sample.pdf",
            "Page one introduces the onboarding process for new employees.",
            "Page two explains that the exam registration deadline is March fifteenth.",
            "Page three covers the appeals process for disputed grades.");

        var uploadResponse = await client.PostAsync("/api/documents", content);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(uploaded);

        var finalStatus = await PollUntilFinishedAsync(client, uploaded!.Id);
        Assert.Equal(DocumentProcessingStatus.Indexed, finalStatus.ProcessingStatus);
        Assert.NotNull(finalStatus.IndexedAt);

        var chunksResponse = await client.GetAsync($"/api/documents/{uploaded.Id}/chunks");
        chunksResponse.EnsureSuccessStatusCode();
        var chunks = await chunksResponse.Content.ReadFromJsonAsync<List<DocumentChunkDto>>();

        Assert.NotNull(chunks);
        Assert.NotEmpty(chunks!);

        var deadlineChunk = chunks!.Single(c => c.TextContent.Contains("March fifteenth"));
        Assert.True(deadlineChunk.PageStart <= 2 && deadlineChunk.PageEnd >= 2,
            $"Expected the exam-deadline sentence to be attributed to page 2, but got pages {deadlineChunk.PageStart}-{deadlineChunk.PageEnd}.");

        var appealsChunk = chunks.Single(c => c.TextContent.Contains("appeals process"));
        Assert.True(appealsChunk.PageStart <= 3 && appealsChunk.PageEnd >= 3,
            $"Expected the appeals sentence to be attributed to page 3, but got pages {appealsChunk.PageStart}-{appealsChunk.PageEnd}.");
    }

    [Fact]
    public async Task DocumentWithNoExtractableText_FailsWithAnExplicitOcrMessage_NotSilentSuccess()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        // No content stream at all -> PdfPig parses it fine but there is zero extractable text,
        // the same shape as a scanned/image-only PDF.
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildBlankPageBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "scanned.pdf");

        var uploadResponse = await client.PostAsync("/api/documents", content);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var finalStatus = await PollUntilFinishedAsync(client, uploaded!.Id);

        Assert.Equal(DocumentProcessingStatus.Failed, finalStatus.ProcessingStatus);
        Assert.Contains("OCR", finalStatus.ProcessingError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Retry_OnlyWorksForFailedDocuments()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var content = BuildPdfUpload("sample.pdf", "Some real content that will index successfully.");
        var uploadResponse = await client.PostAsync("/api/documents", content);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        await PollUntilFinishedAsync(client, uploaded!.Id);

        var retryResponse = await client.PostAsync($"/api/documents/{uploaded.Id}/retry", null);
        Assert.Equal(HttpStatusCode.BadRequest, retryResponse.StatusCode);
    }

    [Fact]
    public async Task Retry_RequeuesAFailedDocumentAndItIsReprocessed()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildBlankPageBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "scanned.pdf");

        var uploadResponse = await client.PostAsync("/api/documents", content);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();
        await PollUntilFinishedAsync(client, uploaded!.Id);

        var retryResponse = await client.PostAsync($"/api/documents/{uploaded.Id}/retry", null);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        var retried = await retryResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.Equal(DocumentProcessingStatus.Queued, retried!.ProcessingStatus);

        // Same input, so it deterministically fails again — this proves the worker actually
        // picked it back up rather than retry being a no-op.
        var finalStatus = await PollUntilFinishedAsync(client, uploaded.Id);
        Assert.Equal(DocumentProcessingStatus.Failed, finalStatus.ProcessingStatus);
    }

    [Fact]
    public async Task Chunks_AreNotVisibleToADifferentUser()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        using var content = BuildPdfUpload("sample.pdf", "Owner-only content about a confidential policy.");
        var uploadResponse = await ownerClient.PostAsync("/api/documents", content);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();
        await PollUntilFinishedAsync(ownerClient, uploaded!.Id);

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());
        var response = await otherClient.GetAsync($"/api/documents/{uploaded.Id}/chunks");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<DocumentDto> PollUntilFinishedAsync(HttpClient client, Guid documentId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/documents/{documentId}");
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<DocumentDto>();

            if (dto!.ProcessingStatus is DocumentProcessingStatus.Indexed or DocumentProcessingStatus.Failed)
            {
                return dto;
            }

            await Task.Delay(300);
        }

        throw new TimeoutException($"Document {documentId} did not finish processing within the timeout.");
    }

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            firstName = "Test",
            lastName = "User",
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

    private static MultipartFormDataContent BuildPdfUpload(string fileName, params string[] pageTexts)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildMultiPagePdfBytes(pageTexts));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", fileName);
        return content;
    }
}
