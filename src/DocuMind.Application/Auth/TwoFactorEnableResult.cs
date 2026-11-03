namespace DocuMind.Application.Auth;

public record TwoFactorEnableResult(bool Succeeded, IReadOnlyList<string> RecoveryCodes, IReadOnlyList<string> Errors)
{
    public static TwoFactorEnableResult Success(IReadOnlyList<string> recoveryCodes) => new(true, recoveryCodes, []);
    public static TwoFactorEnableResult Failure(IReadOnlyList<string> errors) => new(false, [], errors);
}
