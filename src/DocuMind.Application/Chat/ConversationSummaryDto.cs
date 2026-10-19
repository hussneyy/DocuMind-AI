namespace DocuMind.Application.Chat;

public record ConversationSummaryDto(Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
