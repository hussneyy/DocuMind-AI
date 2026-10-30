using DocuMind.Application.Auditing;
using DocuMind.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace DocuMind.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuditLogger auditLogger) : IAuthService
{
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

    public async Task<AuthResult> UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return AuthResult.Failure(["User not found."]);
        }

        user.DisplayName = displayName;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        auditLogger.Log(AuditEventType.DisplayNameChanged, user.Id, $"displayName={displayName}");
        return AuthResult.Success(ToDto(user));
    }

    private static CurrentUserDto ToDto(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.CreatedAt);
}
