using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Auth;

public class UpdateDisplayNameRequest
{
    [Required, MinLength(1), MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;
}
