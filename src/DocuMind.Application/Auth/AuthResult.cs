namespace DocuMind.Application.Auth;

public record AuthResult(bool Succeeded, bool RequiresTwoFactor, CurrentUserDto? User, IReadOnlyList<string> Errors)
{
    public static AuthResult Success(CurrentUserDto user) => new(true, false, user, []);
    public static AuthResult Failure(IReadOnlyList<string> errors) => new(false, false, null, errors);
    public static AuthResult TwoFactorRequired() => new(false, true, null, []);
}
