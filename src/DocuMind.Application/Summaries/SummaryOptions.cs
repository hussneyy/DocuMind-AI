namespace DocuMind.Application.Summaries;

public class SummaryOptions
{
    public const string SectionName = "Summary";

    /// <summary>Max chunks fed to the model for a Detailed summary — bounds prompt size for large documents.</summary>
    public int DetailedMaxChunks { get; set; } = 40;

    /// <summary>
    /// Chunks sampled (evenly spaced across the whole document, not just the start) for Short and
    /// KeyPoints summaries, which are meant to be brief regardless of document length.
    /// </summary>
    public int ShortSampleSize { get; set; } = 6;

    public int KeyPointsSampleSize { get; set; } = 8;
}
