namespace DocuMind.Application.Chat;

public record ConversationDetailDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<MessageDto> Messages);
