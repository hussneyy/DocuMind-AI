using DocuMind.Domain.Entities;

namespace DocuMind.Application.Chat;

public interface IConversationRepository
{
    Task AddAsync(Conversation conversation, CancellationToken ct);

    /// <summary>Returns null if the conversation doesn't exist or isn't owned by <paramref name="ownerUserId"/>.</summary>
    Task<Conversation?> GetAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct);

    Task<IReadOnlyList<Conversation>> ListAsync(Guid ownerUserId, CancellationToken ct);

    Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, CancellationToken ct);

    Task<IReadOnlyList<MessageCitation>> GetCitationsAsync(IReadOnlyList<Guid> messageIds, CancellationToken ct);

    Task AddMessageAsync(Message message, IReadOnlyList<MessageCitation> citations, CancellationToken ct);

    Task TouchAsync(Guid conversationId, DateTimeOffset updatedAt, CancellationToken ct);

    /// <summary>No-ops if the conversation doesn't exist or isn't owned by <paramref name="ownerUserId"/>.</summary>
    Task DeleteAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct);
}
