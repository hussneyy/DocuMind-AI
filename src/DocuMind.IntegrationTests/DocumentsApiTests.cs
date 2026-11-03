using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Application.Common;
using DocuMind.Application.Documents;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class DocumentsApiTests
{
    private readonly DocuMindApiFactory _factory;

    public DocumentsApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/documents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutTheCsrfHeader_IsRejected()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var content = BuildPdfUpload();
        client.DefaultRequestHeaders.Remove("X-Requested-With");

        var response = await client.PostAsync("/api/documents", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_UploadListGetDelete_WorksForTheOwningUser()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var uploadContent = BuildPdfUpload();
        var uploadResponse = await client.PostAsync("/api/documents", uploadContent);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(uploaded);
        Assert.Equal(1, uploaded!.PageCount);
        Assert.Equal("sample.pdf", uploaded.OriginalFileName);

        var listResponse = await client.GetAsync("/api/documents");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<DocumentDto>>();
        Assert.Contains(page!.Items, d => d.Id == uploaded.Id);

        var getResponse = await client.GetAsync($"/api/documents/{uploaded.Id}");
        getResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync($"/api/documents/{uploaded.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await client.GetAsync($"/api/documents/{uploaded.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Document_IsNotVisibleToADifferentUser()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());

        using var uploadContent = BuildPdfUpload();
        var uploadResponse = await ownerClient.PostAsync("/api/documents", uploadContent);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var getResponse = await otherClient.GetAsync($"/api/documents/{uploaded!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var listResponse = await otherClient.GetAsync("/api/documents");
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<DocumentDto>>();
        Assert.DoesNotContain(page!.Items, d => d.Id == uploaded.Id);

        var deleteResponse = await otherClient.DeleteAsync($"/api/documents/{uploaded.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Upload_RejectsAFileThatIsNotActuallyAPdf()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("this is definitely not a pdf"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "fake.pdf");

        var response = await client.PostAsync("/api/documents", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_RejectsAFileWithTheWrongExtension()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildOnePagePdfBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "sample.docx");

        var response = await client.PostAsync("/api/documents", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            var cookieHeader = cookies.First().Split(';')[0];
            client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }
        else
        {
            throw new InvalidOperationException("Register did not return an auth cookie.");
        }
    }

    private static MultipartFormDataContent BuildPdfUpload()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(TestPdf.BuildOnePagePdfBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "sample.pdf");
        return content;
    }
}
