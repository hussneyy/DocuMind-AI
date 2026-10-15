using System.ComponentModel.DataAnnotations;

namespace DocuMind.Api.Contracts.Search;

public class SearchRequestDto
{
    [Required, MinLength(1), MaxLength(500)]
    public string Query { get; set; } = string.Empty;

    public List<Guid>? DocumentIds { get; set; }

    /// <summary>"Semantic", "Keyword", or "Hybrid" (default).</summary>
    public string Mode { get; set; } = "Hybrid";

    [Range(1, 50)]
    public int? TopK { get; set; }
}
