using DocuMind.Application.Chat;
using DocuMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuMind.Infrastructure.Persistence;

public class ConversationRepository(AppDbContext dbContext) : IConversationRepository
{
    public async Task AddAsync(Conversation conversation, CancellationToken ct)
    {
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<Conversation?> GetAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct) =>
        dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OwnerUserId == ownerUserId, ct);

    public async Task<IReadOnlyList<Conversation>> ListAsync(Guid ownerUserId, CancellationToken ct) =>
        await dbContext.Conversations
            .Where(c => c.OwnerUserId == ownerUserId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, CancellationToken ct) =>
        await dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MessageCitation>> GetCitationsAsync(IReadOnlyList<Guid> messageIds, CancellationToken ct)
    {
        if (messageIds.Count == 0) return [];

        return await dbContext.MessageCitations
            .Where(c => messageIds.Contains(c.MessageId))
            .ToListAsync(ct);
    }

    public async Task AddMessageAsync(Message message, IReadOnlyList<MessageCitation> citations, CancellationToken ct)
    {
        dbContext.Messages.Add(message);
        if (citations.Count > 0)
        {
            dbContext.MessageCitations.AddRange(citations);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task TouchAsync(Guid conversationId, DateTimeOffset updatedAt, CancellationToken ct)
    {
        await dbContext.Conversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.UpdatedAt, updatedAt), ct);
    }

    public async Task DeleteAsync(Guid ownerUserId, Guid conversationId, CancellationToken ct)
    {
        var conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.OwnerUserId == ownerUserId, ct);

        if (conversation is null) return;

        dbContext.Conversations.Remove(conversation);
        await dbContext.SaveChangesAsync(ct);
    }
}
