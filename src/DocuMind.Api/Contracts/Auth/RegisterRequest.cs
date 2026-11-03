using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 country code, e.g. "US", "GB".</summary>
    [Required, RegularExpression("^[A-Z]{2}$")]
    public string Country { get; set; } = string.Empty;

    /// <summary>Full number including the country's dial code, e.g. "+44 7911123456".</summary>
    [Required, MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>ISO 639-1 language code — one of "en", "es", "fr", "de".</summary>
    [Required]
    public string PreferredLanguage { get; set; } = "en";
}
