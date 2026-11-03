using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class VerifyTwoFactorCodeRequest
{
    [Required, MinLength(6), MaxLength(10)]
    public string Code { get; set; } = string.Empty;
}
