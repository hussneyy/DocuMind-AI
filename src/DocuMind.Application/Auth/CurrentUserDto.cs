namespace DocuMind.Application.Auth;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAt,
    string? Bio,
    string? Location,
    string? PhoneNumber,
    bool HasAvatar);
