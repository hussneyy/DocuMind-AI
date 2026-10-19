namespace DocuMind.Application.Chat;

public interface IChatService
{
    /// <summary>
    /// Asks a question, grounded in the caller's own indexed documents. Pass <paramref name="conversationId"/>
    /// to continue an existing conversation (prior messages are replayed to the model as follow-up
    /// context) or omit it to start a new one.
    /// </summary>
    Task<AskResultDto> AskAsync(
        Guid ownerUserId, Guid? conversationId, string question, IReadOnlyList<Guid>? documentIds, CancellationToken ct);

    Task<IReadOnlyList<ConversationSummaryDto>> ListConversationsAsync(Guid ownerUserId, CancellationToken ct);

    Task<ConversationDetailDto> GetConversationAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct);

    Task DeleteConversationAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct);
}
