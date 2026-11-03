using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(280)]
    public string? Bio { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }
}
