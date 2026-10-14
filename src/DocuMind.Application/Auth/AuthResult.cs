namespace DocuMind.Application.Auth;

public record AuthResult(bool Succeeded, CurrentUserDto? User, IReadOnlyList<string> Errors)
{
    public static AuthResult Success(CurrentUserDto user) => new(true, user, []);
    public static AuthResult Failure(IReadOnlyList<string> errors) => new(false, null, errors);
}
