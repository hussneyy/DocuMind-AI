namespace DocuMind.Application.Documents;

public class ChunkingOptions
{
    public const string SectionName = "Chunking";

    /// <summary>Target maximum characters per chunk. A single sentence longer than this is hard-split.</summary>
    public int ChunkSizeChars { get; set; } = 1000;

    /// <summary>Trailing characters from the end of one chunk carried into the start of the next, for retrieval context continuity.</summary>
    public int ChunkOverlapChars { get; set; } = 150;
}
