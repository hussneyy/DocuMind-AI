namespace DocuMind.Application.Search;

public class SearchOptions
{
    public const string SectionName = "Search";

    public int DefaultTopK { get; set; } = 10;

    public int MaxTopK { get; set; } = 50;

    /// <summary>Minimum cosine similarity (1 - distance) for a semantic match to be considered relevant at all.</summary>
    public double MinSimilarity { get; set; } = 0.15;
}
