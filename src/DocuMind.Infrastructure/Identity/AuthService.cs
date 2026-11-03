using DocuMind.Application.Auditing;
using DocuMind.Application.Auth;
using DocuMind.Application.Documents;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace DocuMind.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IFileStorage fileStorage,
    IConfiguration configuration,
    IAuditLogger auditLogger) : IAuthService
{
    private readonly string _frontendBaseUrl = (configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:5173").TrimEnd('/');

    public async Task<AuthResult> RegisterAsync(string email, string password, string displayName, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
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

    private static CurrentUserDto ToDto(ApplicationUser user) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.DisplayName,
        user.CreatedAt,
        user.Bio,
        user.Location,
        user.PhoneNumber,
        user.AvatarStorageKey is not null);
}
