namespace DocuMind.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string displayName, CancellationToken ct);

    /// <summary>Validates credentials and, on success, signs the user into the current HTTP context.</summary>
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct);

    Task LogoutAsync(CancellationToken ct);

    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct);
}
