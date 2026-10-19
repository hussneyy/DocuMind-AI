namespace DocuMind.Application.Chat;

public record MessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CitationDto> Citations);
