using Microsoft.AspNetCore.Identity;

namespace DocuMind.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? AvatarStorageKey { get; set; }
    public string? AvatarContentType { get; set; }
}
