using DocuMind.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace DocuMind.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IAuthService
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

        return AuthResult.Success(ToDto(user));
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthResult.Failure(["Invalid email or password."]);
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return AuthResult.Failure(["Too many failed attempts. Please try again later."]);
        }

        if (!result.Succeeded)
        {
            return AuthResult.Failure(["Invalid email or password."]);
        }

        return AuthResult.Success(ToDto(user));
    }

    public async Task LogoutAsync(CancellationToken ct)
    {
        await signInManager.SignOutAsync();
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToDto(user);
    }

    private static CurrentUserDto ToDto(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.CreatedAt);
}
