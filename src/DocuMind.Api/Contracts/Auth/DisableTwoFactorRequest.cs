using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class DisableTwoFactorRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
