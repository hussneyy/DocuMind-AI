namespace DocuMind.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"Local" (default) or "S3".</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Private, non-web-accessible directory where uploaded files are stored. Used when Provider is "Local".</summary>
    public string Directory { get; set; } = "./storage";

    /// <summary>S3 bucket name. Used when Provider is "S3".</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>AWS region the bucket lives in. Used when Provider is "S3".</summary>
    public string Region { get; set; } = "us-east-1";
}
