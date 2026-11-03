using Microsoft.AspNetCore.Identity;

namespace DocuMind.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code, e.g. "US", "GB".</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>ISO 639-1 language code — one of "en", "es", "fr", "de".</summary>
    public string PreferredLanguage { get; set; } = "en";

    public string? AvatarStorageKey { get; set; }
    public string? AvatarContentType { get; set; }
}
