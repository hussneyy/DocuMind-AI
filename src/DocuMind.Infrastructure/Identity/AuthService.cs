using System.Globalization;
using DocuMind.Application.Auditing;
using DocuMind.Application.Auth;
using DocuMind.Application.Documents;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace DocuMind.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IDocumentRepository documentRepository,
    IFileStorage fileStorage,
    IConfiguration configuration,
    IAuditLogger auditLogger) : IAuthService
{
    private const string AuthenticatorIssuer = "DocuMind AI";

    private readonly string _frontendBaseUrl = (configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173").TrimEnd('/');

    public async Task<AuthResult> RegisterAsync(
        string email, string password, string displayName, string? bio, string? location, string? phoneNumber, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
            Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim(),
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        auditLogger.Log(AuditEventType.UserRegistered, user.Id, $"email={email}");

        return AuthResult.Success(ToDto(user));
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            auditLogger.Log(AuditEventType.LoginFailed, null, $"email={email} reason=no-such-account");
            return AuthResult.Failure(["Invalid email or password."]);
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);

        if (result.RequiresTwoFactor)
        {
            auditLogger.Log(AuditEventType.UserLoggedIn, user.Id, $"email={email} reason=2fa-pending");
            return AuthResult.TwoFactorRequired();
        }

        if (result.IsLockedOut)
        {
            auditLogger.Log(AuditEventType.LoginFailed, user.Id, $"email={email} reason=locked-out");
            return AuthResult.Failure(["Too many failed attempts. Please try again later."]);
        }

        if (!result.Succeeded)
        {
            auditLogger.Log(AuditEventType.LoginFailed, user.Id, $"email={email} reason=bad-credentials");
            return AuthResult.Failure(["Invalid email or password."]);
        }

        auditLogger.Log(AuditEventType.UserLoggedIn, user.Id, $"email={email}");
        return AuthResult.Success(ToDto(user));
    }

    public async Task LogoutAsync(Guid userId, CancellationToken ct)
    {
        await signInManager.SignOutAsync();
        auditLogger.Log(AuditEventType.UserLoggedOut, userId, "logout");
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToDto(user);
    }

    public async Task<AuthResult> UpdateProfileAsync(
        Guid userId, string displayName, string? bio, string? location, string? phoneNumber, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthResult.Failure(["User not found."]);
        }

        user.DisplayName = displayName;
        user.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        user.Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        auditLogger.Log(AuditEventType.ProfileUpdated, user.Id, $"displayName={displayName}");
        return AuthResult.Success(ToDto(user));
    }

    public async Task<CurrentUserDto?> UpdateAvatarAsync(Guid userId, Stream content, string contentType, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var previousKey = user.AvatarStorageKey;
        var extension = AvatarUploadValidator.ExtensionFor(contentType);
        var newKey = await fileStorage.SaveAsync(content, extension, ct);

        user.AvatarStorageKey = newKey;
        user.AvatarContentType = contentType;
        await userManager.UpdateAsync(user);

        if (previousKey is not null)
        {
            await fileStorage.DeleteAsync(previousKey, ct);
        }

        auditLogger.Log(AuditEventType.AvatarUpdated, user.Id, "avatar updated");
        return ToDto(user);
    }

    public async Task<CurrentUserDto?> RemoveAvatarAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        if (user.AvatarStorageKey is { } key)
        {
            await fileStorage.DeleteAsync(key, ct);
            user.AvatarStorageKey = null;
            user.AvatarContentType = null;
            await userManager.UpdateAsync(user);
            auditLogger.Log(AuditEventType.AvatarRemoved, user.Id, "avatar removed");
        }

        return ToDto(user);
    }

    public async Task<(Stream Content, string ContentType)?> OpenAvatarAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user?.AvatarStorageKey is null || user.AvatarContentType is null)
        {
            return null;
        }

        var stream = await fileStorage.OpenReadAsync(user.AvatarStorageKey, ct);
        return (stream, user.AvatarContentType);
    }

    public async Task<PasswordResetRequestResult> RequestPasswordResetAsync(string email, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            auditLogger.Log(AuditEventType.PasswordResetRequested, null, $"email={email} reason=no-such-account");
            return new PasswordResetRequestResult(false, null);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = $"{_frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        auditLogger.Log(AuditEventType.PasswordResetRequested, user.Id, $"email={email}");
        return new PasswordResetRequestResult(true, resetLink);
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return PasswordResetResult.Failure(["Invalid or expired reset link."]);
        }

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return PasswordResetResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        auditLogger.Log(AuditEventType.PasswordResetCompleted, user.Id, $"email={email}");
        return PasswordResetResult.Success();
    }

    public async Task<DeleteAccountResult> DeleteAccountAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return DeleteAccountResult.Failure(["User not found."]);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return DeleteAccountResult.Failure(["Incorrect password."]);
        }

        // Database rows cascade via foreign keys once the user row is deleted, but files on disk
        // (documents, avatar) don't — delete those explicitly first.
        var documents = await documentRepository.GetAllForOwnerAsync(userId, ct);
        foreach (var document in documents)
        {
            await fileStorage.DeleteAsync(document.StorageKey, ct);
        }

        if (user.AvatarStorageKey is { } avatarKey)
        {
            await fileStorage.DeleteAsync(avatarKey, ct);
        }

        var email = user.Email;
        await signInManager.SignOutAsync();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return DeleteAccountResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        auditLogger.Log(AuditEventType.AccountDeleted, userId, $"email={email}");
        return DeleteAccountResult.Success();
    }

    public async Task<TwoFactorSetupResult?> GetTwoFactorSetupAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        var authenticatorUri = BuildAuthenticatorUri(user.Email!, unformattedKey!);
        return new TwoFactorSetupResult(unformattedKey!, authenticatorUri);
    }

    public async Task<TwoFactorEnableResult> EnableTwoFactorAsync(Guid userId, string code, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return TwoFactorEnableResult.Failure(["User not found."]);
        }

        var isCodeValid = await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider!, code);
        if (!isCodeValid)
        {
            return TwoFactorEnableResult.Failure(["Invalid verification code."]);
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        auditLogger.Log(AuditEventType.TwoFactorEnabled, user.Id, "2fa enabled");
        return TwoFactorEnableResult.Success(recoveryCodes?.ToList() ?? []);
    }

    public async Task<AuthResult> DisableTwoFactorAsync(Guid userId, string password, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthResult.Failure(["User not found."]);
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return AuthResult.Failure(["Incorrect password."]);
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);

        auditLogger.Log(AuditEventType.TwoFactorDisabled, user.Id, "2fa disabled");
        return AuthResult.Success(ToDto(user));
    }

    public async Task<AuthResult> LoginWithTwoFactorCodeAsync(string code, CancellationToken ct)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return AuthResult.Failure(["No pending two-factor sign-in. Please log in again."]);
        }

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(code, isPersistent: true, rememberClient: false);
        if (result.Succeeded)
        {
            auditLogger.Log(AuditEventType.UserLoggedIn, user.Id, $"email={user.Email} via=2fa-code");
            return AuthResult.Success(ToDto(user));
        }

        if (result.IsLockedOut)
        {
            auditLogger.Log(AuditEventType.LoginFailed, user.Id, "reason=locked-out via=2fa-code");
            return AuthResult.Failure(["Too many failed attempts. Please try again later."]);
        }

        auditLogger.Log(AuditEventType.LoginFailed, user.Id, "reason=bad-2fa-code");
        return AuthResult.Failure(["Invalid authenticator code."]);
    }

    public async Task<AuthResult> LoginWithRecoveryCodeAsync(string recoveryCode, CancellationToken ct)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return AuthResult.Failure(["No pending two-factor sign-in. Please log in again."]);
        }

        var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);
        if (result.Succeeded)
        {
            auditLogger.Log(AuditEventType.UserLoggedIn, user.Id, $"email={user.Email} via=recovery-code");
            return AuthResult.Success(ToDto(user));
        }

        if (result.IsLockedOut)
        {
            auditLogger.Log(AuditEventType.LoginFailed, user.Id, "reason=locked-out via=recovery-code");
            return AuthResult.Failure(["Too many failed attempts. Please try again later."]);
        }

        auditLogger.Log(AuditEventType.LoginFailed, user.Id, "reason=bad-recovery-code");
        return AuthResult.Failure(["Invalid recovery code."]);
    }

    private static string BuildAuthenticatorUri(string email, string unformattedKey) => string.Format(
        CultureInfo.InvariantCulture,
        "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
        Uri.EscapeDataString(AuthenticatorIssuer),
        Uri.EscapeDataString(email),
        unformattedKey);

    private static CurrentUserDto ToDto(ApplicationUser user) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.DisplayName,
        user.CreatedAt,
        user.Bio,
        user.Location,
        user.PhoneNumber,
        user.AvatarStorageKey is not null,
        user.TwoFactorEnabled);
}
