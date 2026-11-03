namespace DocuMind.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(
        string email, string password, string firstName, string lastName, string country, string phoneNumber,
        string preferredLanguage, CancellationToken ct);

    /// <summary>
    /// Validates credentials and, on success, signs the user into the current HTTP context.
    /// If the account has two-factor authentication enabled, signs in only partially and returns
    /// <see cref="AuthResult.RequiresTwoFactor"/> — call <see cref="LoginWithTwoFactorCodeAsync"/> or
    /// <see cref="LoginWithRecoveryCodeAsync"/> next to complete the sign-in.
    /// </summary>
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct);

    Task LogoutAsync(Guid userId, CancellationToken ct);

    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct);

    Task<AuthResult> UpdateProfileAsync(
        Guid userId, string displayName, string? bio, string? location, string? phoneNumber,
        string? country, string? preferredLanguage, CancellationToken ct);

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

    /// <summary>
    /// Permanently deletes the account after verifying the given password, including the user's
    /// uploaded files and avatar (database rows cascade via foreign keys; files on disk don't, so
    /// they're deleted explicitly first).
    /// </summary>
    Task<DeleteAccountResult> DeleteAccountAsync(Guid userId, string password, CancellationToken ct);

    /// <summary>Returns the current (or newly generated) authenticator key and QR enrollment URI.</summary>
    Task<TwoFactorSetupResult?> GetTwoFactorSetupAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Verifies the authenticator code, and if valid, turns on two-factor auth and returns a
    /// fresh set of one-time recovery codes — shown to the user exactly once.
    /// </summary>
    Task<TwoFactorEnableResult> EnableTwoFactorAsync(Guid userId, string code, CancellationToken ct);

    /// <summary>Turns off two-factor auth and invalidates the authenticator key, after verifying the password.</summary>
    Task<AuthResult> DisableTwoFactorAsync(Guid userId, string password, CancellationToken ct);

    /// <summary>Completes a sign-in that returned <see cref="AuthResult.RequiresTwoFactor"/>.</summary>
    Task<AuthResult> LoginWithTwoFactorCodeAsync(string code, CancellationToken ct);

    /// <summary>Completes a pending two-factor sign-in using a one-time recovery code instead of an authenticator code.</summary>
    Task<AuthResult> LoginWithRecoveryCodeAsync(string recoveryCode, CancellationToken ct);
}
