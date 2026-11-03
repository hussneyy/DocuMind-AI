using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class TwoFactorRecoveryLoginRequest
{
    [Required]
    public string RecoveryCode { get; set; } = string.Empty;
}
