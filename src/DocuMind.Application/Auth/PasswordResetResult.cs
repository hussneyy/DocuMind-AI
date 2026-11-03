namespace DocuMind.Application.Auth;

public record PasswordResetResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static PasswordResetResult Success() => new(true, []);
    public static PasswordResetResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
