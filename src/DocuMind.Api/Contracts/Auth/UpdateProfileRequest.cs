using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class UpdateProfileRequest
{
    [Required, MinLength(1), MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(280)]
    public string? Bio { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(30)]
    public string? PhoneNumber { get; set; }
}
