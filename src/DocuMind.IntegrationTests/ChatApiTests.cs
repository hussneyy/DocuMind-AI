using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocuMind.Application.Chat;
using DocuMind.Application.Documents;
using DocuMind.Domain.Enums;

namespace DocuMind.IntegrationTests;

[Collection(DocuMindApiCollection.Name)]
public class ChatApiTests
{
    private readonly DocuMindApiFactory _factory;

    public ChatApiTests(DocuMindApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ask_WithNoRelevantDocuments_ReturnsAGroundedRefusal_NotAFabricatedAnswer()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var result = await AskAsync(client, "What is the exam registration deadline?");

        Assert.Contains("don't have enough information", result.AssistantMessage.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.AssistantMessage.Citations);
    }

    [Fact]
    public async Task Ask_WithARelevantIndexedDocument_ReturnsAGroundedAnswerWithACorrectPageCitation()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var document = await UploadAndWaitForIndexedAsync(client,
            "Employee onboarding covers desk setup and introductions.",
            "The exam registration deadline is March fifteenth this year.");

        var result = await AskAsync(client, "When is the exam registration deadline?");

        Assert.Contains("March fifteenth", result.AssistantMessage.Content);
        Assert.NotEmpty(result.AssistantMessage.Citations);
        var citation = result.AssistantMessage.Citations[0];
        Assert.Equal(document.Id, citation.DocumentId);
        Assert.True(citation.PageStart <= 2 && citation.PageEnd >= 2, $"Expected page 2 to be covered, got {citation.PageStart}-{citation.PageEnd}");
    }

    [Fact]
    public async Task Ask_ContinuingAConversation_AppendsMessagesToTheSameConversation()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        await UploadAndWaitForIndexedAsync(client, "The library closes at midnight on weekdays.");

        var first = await AskAsync(client, "What time does the library close?");
        var second = await AskAsync(client, "And on weekends?", first.ConversationId);

        Assert.Equal(first.ConversationId, second.ConversationId);

        var detailResponse = await client.GetAsync($"/api/chat/conversations/{first.ConversationId}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<ConversationDetailDto>();

        Assert.Equal(4, detail!.Messages.Count);
        Assert.Equal(["user", "assistant", "user", "assistant"], detail.Messages.Select(m => m.Role));
    }

    [Fact]
    public async Task Ask_ScopedToASpecificDocument_OnlyCitesThatDocument()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var docA = await UploadAndWaitForIndexedAsync(client, "The exam registration deadline is March fifteenth.");
        await UploadAndWaitForIndexedAsync(client, "The exam registration deadline is also mentioned in this other document.");

        var result = await AskAsync(client, "When is the exam registration deadline?", documentIds: [docA.Id]);

        Assert.NotEmpty(result.AssistantMessage.Citations);
        Assert.All(result.AssistantMessage.Citations, c => Assert.Equal(docA.Id, c.DocumentId));
    }

    [Fact]
    public async Task Ask_NeverCitesAnotherUsersDocument()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        await UploadAndWaitForIndexedAsync(ownerClient, "This confidential policy document mentions a secret budget figure.");

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var result = await AskAsync(otherClient, "What is the confidential policy's secret budget figure?");

        Assert.Empty(result.AssistantMessage.Citations);
    }

    [Fact]
    public async Task Ask_WithAnotherUsersConversationId_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        var owned = await AskAsync(ownerClient, "first question");

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var response = await otherClient.PostAsJsonAsync("/api/chat/ask", new { question = "trying to hijack", conversationId = owned.ConversationId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetConversation_ForAnotherUsersConversation_ReturnsNotFound()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        var owned = await AskAsync(ownerClient, "first question");

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var response = await otherClient.GetAsync($"/api/chat/conversations/{owned.ConversationId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListConversations_OnlyReturnsTheCallersOwnConversations()
    {
        var ownerClient = _factory.CreateClient();
        await RegisterAsync(ownerClient, NewEmail());
        await AskAsync(ownerClient, "a question only this user asked");

        var otherClient = _factory.CreateClient();
        await RegisterAsync(otherClient, NewEmail());

        var response = await otherClient.GetAsync("/api/chat/conversations");
        response.EnsureSuccessStatusCode();
        var conversations = await response.Content.ReadFromJsonAsync<List<ConversationSummaryDto>>();

        Assert.Empty(conversations!);
    }

    [Fact]
    public async Task DeleteConversation_RemovesIt_SoASubsequentGetReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());
        var result = await AskAsync(client, "a question to delete later");

        var deleteResponse = await client.DeleteAsync($"/api/chat/conversations/{result.ConversationId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/chat/conversations/{result.ConversationId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Ask_WithABlankQuestion_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await RegisterAsync(client, NewEmail());

        var response = await client.PostAsJsonAsync("/api/chat/ask", new { question = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ask_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/chat/ask", new { question = "test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string NewEmail() => $"chat-{Guid.NewGuid():N}@example.com";

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        client.DefaultRequestHeaders.Add("X-Requested-With", "IntegrationTests");

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Sup3rSecret!1",
            firstName = "Chat",
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

    private static async Task<AskResultDto> AskAsync(
        HttpClient client, string question, Guid? conversationId = null, IReadOnlyList<Guid>? documentIds = null)
    {
        var response = await client.PostAsJsonAsync("/api/chat/ask", new { question, conversationId, documentIds });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AskResultDto>();
        return result!;
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
