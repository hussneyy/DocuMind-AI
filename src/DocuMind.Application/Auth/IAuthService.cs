namespace DocuMind.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string displayName, CancellationToken ct);

    /// <summary>Validates credentials and, on success, signs the user into the current HTTP context.</summary>
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct);

    Task LogoutAsync(Guid userId, CancellationToken ct);

    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct);

    Task<AuthResult> UpdateProfileAsync(
        Guid userId, string displayName, string? bio, string? location, string? phoneNumber, CancellationToken ct);

    /// <summary>Saves a new avatar, replacing and deleting any previous one. Null if the user doesn't exist.</summary>
    Task<CurrentUserDto?> UpdateAvatarAsync(Guid userId, Stream content, string contentType, CancellationToken ct);

    /// <summary>Null if the user doesn't exist. A no-op (but still returns the current profile) if there was no avatar.</summary>
    Task<CurrentUserDto?> RemoveAvatarAsync(Guid userId, CancellationToken ct);

    /// <summary>Null if the user doesn't exist or has no avatar set.</summary>
    Task<(Stream Content, string ContentType)?> OpenAvatarAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Generates a password reset token for the given email, if an account exists for it. This
    /// project has no email sending configured, so the caller is expected to surface the returned
    /// link directly (demo-mode behavior — see <see cref="PasswordResetRequestResult"/>).
    /// </summary>
    Task<PasswordResetRequestResult> RequestPasswordResetAsync(string email, CancellationToken ct);

    Task<PasswordResetResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct);
}
