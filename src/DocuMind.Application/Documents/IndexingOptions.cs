namespace DocuMind.Application.Documents;

public class IndexingOptions
{
    public const string SectionName = "Indexing";

    /// <summary>How often the background worker checks for newly queued documents.</summary>
    public int PollIntervalSeconds { get; set; } = 2;

    /// <summary>Documents claimed per poll.</summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>Documents indexed in parallel within a batch.</summary>
    public int MaxConcurrency { get; set; } = 2;
}
