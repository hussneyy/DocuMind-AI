using Amazon.S3;
using Amazon.S3.Model;
using DocuMind.Application.Documents;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Storage;

/// <summary>
/// Stores files in a private S3 bucket. Storage keys are server-generated GUIDs — never derived
/// from user-supplied file names — matching <see cref="LocalFileStorage"/>'s key scheme, since
/// both live behind the same <see cref="IFileStorage"/> contract. Credentials come from the
/// EC2 instance role (or any ambient AWS credential source); none are configured here.
/// </summary>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3FileStorage(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _bucketName = options.Value.BucketName;
    }

    public async Task<string> SaveAsync(Stream content, string fileExtension, CancellationToken ct)
    {
        var storageKey = $"{Guid.NewGuid():N}{fileExtension}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = content,
            AutoCloseStream = false,
        }, ct);

        return storageKey;
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var response = await _s3.GetObjectAsync(_bucketName, storageKey, ct);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        await _s3.DeleteObjectAsync(_bucketName, storageKey, ct);
    }
}
