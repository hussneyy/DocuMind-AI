namespace DocuMind.Application.Auth;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    DateTimeOffset CreatedAt,
    string? Bio,
    string? Location,
    string Country,
    string PreferredLanguage,
    string? PhoneNumber,
    bool HasAvatar,
    bool TwoFactorEnabled);
