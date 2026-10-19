using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Chat;

public class AskRequestDto
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Question { get; set; } = string.Empty;

    /// <summary>Omit to start a new conversation; supply to continue an existing one.</summary>
    public Guid? ConversationId { get; set; }

    /// <summary>Optional: restrict retrieval to these documents. Omit to search everything the caller owns.</summary>
    public IReadOnlyList<Guid>? DocumentIds { get; set; }
}
