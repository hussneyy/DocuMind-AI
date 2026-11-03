using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Application.Common;
using DocuMind.Application.Documents;
using DocuMind.Application.Tags;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class TagsApiTests
{
    private readonly DocuMindApiFactory _factory;

    public TagsApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenList_ReturnsTheNewTag()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var createResponse = await client.PostAsJsonAsync("/api/tags", new { name = "Policy" });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.Equal("Policy", created!.Name);

        var listResponse = await client.GetAsync("/api/tags");
        listResponse.EnsureSuccessStatusCode();
        var tags = await listResponse.Content.ReadFromJsonAsync<List<TagDto>>();
        Assert.Contains(tags!, t => t.Id == created.Id);
    }

    [Fact]
    public async Task Create_WithTheSameNameTwice_ReturnsTheSameTagInstead()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var first = await client.PostAsJsonAsync("/api/tags", new { name = "Policy" });
        var firstTag = await first.Content.ReadFromJsonAsync<TagDto>();

        var second = await client.PostAsJsonAsync("/api/tags", new { name = "Policy" });
        var secondTag = await second.Content.ReadFromJsonAsync<TagDto>();

        Assert.Equal(firstTag!.Id, secondTag!.Id);

        var listResponse = await client.GetAsync("/api/tags");
        var tags = await listResponse.Content.ReadFromJsonAsync<List<TagDto>>();
        Assert.Single(tags!, t => t.Name == "Policy");
    }

    [Fact]
    public async Task Create_WithAnEmptyName_IsRejected()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var response = await client.PostAsJsonAsync("/api/tags", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AttachTag_ThenGetDocument_IncludesTheTag_AndDetach_RemovesIt()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var uploadContent = BuildPdfUpload();
        var uploadResponse = await client.PostAsync("/api/documents", uploadContent);
        var document = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var tagResponse = await client.PostAsJsonAsync("/api/tags", new { name = "Policy" });
        var tag = await tagResponse.Content.ReadFromJsonAsync<TagDto>();

        var attachResponse = await client.PostAsync($"/api/documents/{document!.Id}/tags/{tag!.Id}", content: null);
        attachResponse.EnsureSuccessStatusCode();
        var afterAttach = await attachResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.Contains(afterAttach!.Tags, t => t.Id == tag.Id);

        var getResponse = await client.GetAsync($"/api/documents/{document.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.Contains(fetched!.Tags, t => t.Id == tag.Id);

        var detachResponse = await client.DeleteAsync($"/api/documents/{document.Id}/tags/{tag.Id}");
        detachResponse.EnsureSuccessStatusCode();
        var afterDetach = await detachResponse.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.DoesNotContain(afterDetach!.Tags, t => t.Id == tag.Id);
    }

    [Fact]
    public async Task ListDocuments_FilteredByTagId_OnlyReturnsTaggedDocuments()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        using var taggedUpload = BuildPdfUpload();
        var taggedResponse = await client.PostAsync("/api/documents", taggedUpload);
        var tagged = await taggedResponse.Content.ReadFromJsonAsync<DocumentDto>();

        using var untaggedUpload = BuildPdfUpload();
        var untaggedResponse = await client.PostAsync("/api/documents", untaggedUpload);
        var untagged = await untaggedResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var tagResponse = await client.PostAsJsonAsync("/api/tags", new { name = "Policy" });
        var tag = await tagResponse.Content.ReadFromJsonAsync<TagDto>();
        await client.PostAsync($"/api/documents/{tagged!.Id}/tags/{tag!.Id}", content: null);

        var listResponse = await client.GetAsync($"/api/documents?tagId={tag.Id}");
        listResponse.EnsureSuccessStatusCode();
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<DocumentDto>>();

        Assert.Contains(page!.Items, d => d.Id == tagged.Id);
        Assert.DoesNotContain(page.Items, d => d.Id == untagged!.Id);
    }

    [Fact]
    public async Task AttachTag_WithATagOwnedByAnotherUser_IsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        using var uploadContent = BuildPdfUpload();
        var uploadResponse = await ownerClient.PostAsync("/api/documents", uploadContent);
        var document = await uploadResponse.Content.ReadFromJsonAsync<DocumentDto>();

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());
        var otherTagResponse = await otherClient.PostAsJsonAsync("/api/tags", new { name = "Policy" });
        var otherTag = await otherTagResponse.Content.ReadFromJsonAsync<TagDto>();

        var attachResponse = await ownerClient.PostAsync($"/api/documents/{document!.Id}/tags/{otherTag!.Id}", content: null);

        Assert.Equal(HttpStatusCode.NotFound, attachResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTag_RemovesItFromList()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var tagResponse = await client.PostAsJsonAsync("/api/tags", new { name = "Policy" });
        var tag = await tagResponse.Content.ReadFromJsonAsync<TagDto>();

        var deleteResponse = await client.DeleteAsync($"/api/tags/{tag!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await client.GetAsync("/api/tags");
        var tags = await listResponse.Content.ReadFromJsonAsync<List<TagDto>>();
        Assert.DoesNotContain(tags!, t => t.Id == tag.Id);
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
