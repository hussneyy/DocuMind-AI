using DocuMind.Application.Chat;
using DocuMind.Application.Common.Exceptions;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;

namespace DocuMind.Tests.Chat;

public class ChatServiceTests
{
    [Fact]
    public async Task AskAsync_WithNoConversationId_CreatesANewConversation()
    {
        var repository = new FakeConversationRepository();
        var generator = new FakeRagAnswerGenerator(new RagAnswer("The answer.", [], ContextWasAvailable: true));
        var service = new ChatService(repository, generator);
        var ownerUserId = Guid.NewGuid();

        var result = await service.AskAsync(ownerUserId, null, "What is the deadline?", null, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.ConversationId);
        var stored = repository.Conversations.Single();
        Assert.Equal(ownerUserId, stored.OwnerUserId);
        Assert.Equal("What is the deadline?", stored.Title);
    }

    [Fact]
    public async Task AskAsync_WithALongQuestion_TruncatesTheGeneratedTitle()
    {
        var repository = new FakeConversationRepository();
        var generator = new FakeRagAnswerGenerator(new RagAnswer("answer", [], true));
        var service = new ChatService(repository, generator);
        var longQuestion = string.Join(' ', Enumerable.Repeat("word", 30));

        await service.AskAsync(Guid.NewGuid(), null, longQuestion, null, CancellationToken.None);

        var title = repository.Conversations.Single().Title;
        Assert.True(title.Length <= 61, $"Expected a truncated title, got {title.Length} chars: {title}");
        Assert.EndsWith("…", title);
    }

    [Fact]
    public async Task AskAsync_WithBlankQuestion_ThrowsValidation()
    {
        var service = new ChatService(new FakeConversationRepository(), new FakeRagAnswerGenerator(new RagAnswer("x", [], true)));

        await Assert.ThrowsAsync<AppValidationException>(() =>
            service.AskAsync(Guid.NewGuid(), null, "   ", null, CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_WithAnUnknownConversationId_ThrowsNotFound()
    {
        var service = new ChatService(new FakeConversationRepository(), new FakeRagAnswerGenerator(new RagAnswer("x", [], true)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.AskAsync(Guid.NewGuid(), Guid.NewGuid(), "question", null, CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_WithAnExistingConversation_ReplaysPriorMessagesAsHistoryToTheGenerator()
    {
        var repository = new FakeConversationRepository();
        var ownerUserId = Guid.NewGuid();
        var generator = new FakeRagAnswerGenerator(new RagAnswer("second answer", [], true));
        var service = new ChatService(repository, generator);

        var first = await service.AskAsync(ownerUserId, null, "first question", null, CancellationToken.None);
        await service.AskAsync(ownerUserId, first.ConversationId, "second question", null, CancellationToken.None);

        Assert.NotNull(generator.LastHistory);
        Assert.Equal(2, generator.LastHistory!.Count);
        Assert.Equal(("user", "first question"), generator.LastHistory[0]);
        Assert.Equal(("assistant", "second answer"), generator.LastHistory[1]);
    }

    [Fact]
    public async Task AskAsync_AttachesCitationsOnlyToTheAssistantMessage()
    {
        var citation = new RagCitation(Guid.NewGuid(), Guid.NewGuid(), "handbook.pdf", 2, 2, "excerpt");
        var repository = new FakeConversationRepository();
        var generator = new FakeRagAnswerGenerator(new RagAnswer("answer [1]", [citation], true));
        var service = new ChatService(repository, generator);

        var result = await service.AskAsync(Guid.NewGuid(), null, "question", null, CancellationToken.None);

        Assert.Empty(result.UserMessage.Citations);
        Assert.Single(result.AssistantMessage.Citations);
        Assert.Equal("handbook.pdf", result.AssistantMessage.Citations[0].DocumentFileName);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsMessagesInChronologicalOrder()
    {
        var repository = new FakeConversationRepository();
        var ownerUserId = Guid.NewGuid();
        var generator = new FakeRagAnswerGenerator(new RagAnswer("a1", [], true));
        var service = new ChatService(repository, generator);

        var ask1 = await service.AskAsync(ownerUserId, null, "q1", null, CancellationToken.None);
        await service.AskAsync(ownerUserId, ask1.ConversationId, "q2", null, CancellationToken.None);

        var detail = await service.GetConversationAsync(ownerUserId, ask1.ConversationId, CancellationToken.None);

        Assert.Equal(4, detail.Messages.Count);
        Assert.Equal(["q1", "a1", "q2", "a1"], detail.Messages.Select(m => m.Content));
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        public List<Conversation> Conversations { get; } = [];
        public List<Message> Messages { get; } = [];
        public List<MessageCitation> Citations { get; } = [];

        public Task AddAsync(Conversation conversation, CancellationToken ct)
        {
            Conversations.Add(conversation);
            return Task.CompletedTask;
        }

        public Task<Conversation?> GetAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct) =>
            Task.FromResult(Conversations.SingleOrDefault(c => c.Id == conversationId && c.OwnerUserId == ownerUserId));

        public Task<IReadOnlyList<Conversation>> ListAsync(Guid ownerUserId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Conversation>>(Conversations.Where(c => c.OwnerUserId == ownerUserId).ToList());

        public Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Message>>(Messages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.CreatedAt).ToList());

        public Task<IReadOnlyList<MessageCitation>> GetCitationsAsync(IReadOnlyList<Guid> messageIds, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MessageCitation>>(Citations.Where(c => messageIds.Contains(c.MessageId)).ToList());

        public Task AddMessageAsync(Message message, IReadOnlyList<MessageCitation> citations, CancellationToken ct)
        {
            Messages.Add(message);
            Citations.AddRange(citations);
            return Task.CompletedTask;
        }

        public Task TouchAsync(Guid conversationId, DateTimeOffset updatedAt, CancellationToken ct)
        {
            var conversation = Conversations.Single(c => c.Id == conversationId);
            conversation.UpdatedAt = updatedAt;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct)
        {
            Conversations.RemoveAll(c => c.Id == conversationId && c.OwnerUserId == ownerUserId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRagAnswerGenerator(RagAnswer answer) : IRagAnswerGenerator
    {
        public List<(string Role, string Content)>? LastHistory { get; private set; }

        public Task<RagAnswer> GenerateAsync(
            Guid ownerUserId, string question, IReadOnlyList<Guid>? documentIds,
            IReadOnlyList<(string Role, string Content)> history, CancellationToken ct)
        {
            LastHistory = history.ToList();
            return Task.FromResult(answer);
        }
    }
}
