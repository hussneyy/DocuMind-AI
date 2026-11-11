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

    /// <summary>
    /// Overrides the S3 endpoint for an S3-compatible provider that isn't AWS itself (e.g.
    /// Cloudflare R2's "https://&lt;account-id&gt;.r2.cloudflarestorage.com"). Leave empty for
    /// real AWS S3, where the SDK derives the endpoint from <see cref="Region"/> instead.
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Explicit credentials for an S3-compatible provider with no concept of an EC2 instance
    /// role (e.g. R2). Leave both empty on real AWS S3, where the SDK's default credential
    /// chain (the EC2 instance role) is used instead.
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
}
