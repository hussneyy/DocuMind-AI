using DocuMind.Application.Auditing;
using DocuMind.Application.Common.Exceptions;
using DocuMind.Domain.Entities;
using DocuMind.Domain.Enums;

namespace DocuMind.Application.Chat;

public class ChatService(
    IConversationRepository conversationRepository,
    IRagAnswerGenerator ragAnswerGenerator,
    IAuditLogger auditLogger) : IChatService
{
    private const int TitleMaxLength = 60;

    public async Task<AskResultDto> AskAsync(
        Guid ownerUserId, Guid? conversationId, string question, IReadOnlyList<Guid>? documentIds, CancellationToken ct)
    {
        var trimmedQuestion = question.Trim();
        if (trimmedQuestion.Length == 0)
        {
            throw new AppValidationException(["A question is required."]);
        }

        Conversation conversation;
        IReadOnlyList<Message> priorMessages;

        if (conversationId is { } existingId)
        {
            conversation = await conversationRepository.GetAsync(ownerUserId, existingId, ct)
                ?? throw new NotFoundException("Conversation not found.");
            priorMessages = await conversationRepository.GetMessagesAsync(existingId, ct);
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                OwnerUserId = ownerUserId,
                Title = BuildTitle(trimmedQuestion),
                CreatedAt = now,
                UpdatedAt = now,
            };
            await conversationRepository.AddAsync(conversation, ct);
            priorMessages = [];
        }

        var history = priorMessages
            .OrderBy(m => m.CreatedAt)
            .Select(m => (Role: m.Role == MessageRole.User ? "user" : "assistant", m.Content))
            .ToList();

        var ragAnswer = await ragAnswerGenerator.GenerateAsync(ownerUserId, trimmedQuestion, documentIds, history, ct);

        var askedAt = DateTimeOffset.UtcNow;
        var userMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = trimmedQuestion,
            CreatedAt = askedAt,
        };
        var assistantMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = ragAnswer.AnswerText,
            // Strictly after the user's message so conversation order is unambiguous when sorted by CreatedAt.
            CreatedAt = askedAt.AddMilliseconds(1),
        };

        var citations = ragAnswer.Citations
            .Select(c => new MessageCitation
            {
                Id = Guid.NewGuid(),
                MessageId = assistantMessage.Id,
                ChunkId = c.ChunkId,
                DocumentId = c.DocumentId,
                DocumentFileName = c.DocumentFileName,
                PageStart = c.PageStart,
                PageEnd = c.PageEnd,
                Excerpt = c.Excerpt,
            })
            .ToList();

        await conversationRepository.AddMessageAsync(userMessage, [], ct);
        await conversationRepository.AddMessageAsync(assistantMessage, citations, ct);
        await conversationRepository.TouchAsync(conversation.Id, assistantMessage.CreatedAt, ct);

        return new AskResultDto(
            conversation.Id,
            conversation.Title,
            ToDto(userMessage, []),
            ToDto(assistantMessage, citations));
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> ListConversationsAsync(Guid ownerUserId, CancellationToken ct)
    {
        var conversations = await conversationRepository.ListAsync(ownerUserId, ct);
        return conversations
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryDto(c.Id, c.Title, c.CreatedAt, c.UpdatedAt))
            .ToList();
    }

    public async Task<ConversationDetailDto> GetConversationAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct)
    {
        var conversation = await conversationRepository.GetAsync(ownerUserId, conversationId, ct)
            ?? throw new NotFoundException("Conversation not found.");

        var messages = await conversationRepository.GetMessagesAsync(conversationId, ct);
        var messageIds = messages.Select(m => m.Id).ToList();
        var citations = await conversationRepository.GetCitationsAsync(messageIds, ct);
        var citationsByMessage = citations.GroupBy(c => c.MessageId).ToDictionary(g => g.Key, g => g.ToList());

        var messageDtos = messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => ToDto(m, citationsByMessage.GetValueOrDefault(m.Id, [])))
            .ToList();

        return new ConversationDetailDto(conversation.Id, conversation.Title, conversation.CreatedAt, conversation.UpdatedAt, messageDtos);
    }

    public async Task DeleteConversationAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct)
    {
        // Ownership-check-then-act, the same pattern DocumentService.DeleteAsync uses — both so a
        // request for someone else's conversation gets a 404 like every other resource in this
        // app, and so the audit log below only fires when something was actually deleted.
        _ = await conversationRepository.GetAsync(ownerUserId, conversationId, ct)
            ?? throw new NotFoundException("Conversation not found.");

        await conversationRepository.DeleteAsync(ownerUserId, conversationId, ct);
        auditLogger.Log(AuditEventType.ConversationDeleted, ownerUserId, $"conversationId={conversationId}");
    }

    private static MessageDto ToDto(Message message, IReadOnlyList<MessageCitation> citations) => new(
        message.Id,
        message.Role == MessageRole.User ? "user" : "assistant",
        message.Content,
        message.CreatedAt,
        citations.Select(c => new CitationDto(c.ChunkId, c.DocumentId, c.DocumentFileName, c.PageStart, c.PageEnd, c.Excerpt)).ToList());

    private static string BuildTitle(string question)
    {
        var singleLine = question.ReplaceLineEndings(" ");
        if (singleLine.Length <= TitleMaxLength)
        {
            return singleLine;
        }

        var truncated = singleLine[..TitleMaxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > TitleMaxLength / 2)
        {
            truncated = truncated[..lastSpace];
        }

        return truncated.TrimEnd() + "…";
    }
}
