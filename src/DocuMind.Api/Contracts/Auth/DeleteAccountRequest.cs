using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class DeleteAccountRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
