namespace DocuMind.Application.Documents;

public class UploadOptions
{
    public const string SectionName = "Upload";

    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
    public int MaxPageCount { get; set; } = 500;
}
