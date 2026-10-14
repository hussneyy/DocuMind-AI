namespace DocuMind.Application.Auth;

public record CurrentUserDto(Guid Id, string Email, string DisplayName, DateTimeOffset CreatedAt);
