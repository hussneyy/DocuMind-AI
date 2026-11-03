namespace DocuMind.Application.Auth;

public record TwoFactorSetupResult(string SharedKey, string AuthenticatorUri);
