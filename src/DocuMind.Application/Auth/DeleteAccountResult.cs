namespace DocuMind.Application.Auth;

public record DeleteAccountResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static DeleteAccountResult Success() => new(true, []);
    public static DeleteAccountResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
