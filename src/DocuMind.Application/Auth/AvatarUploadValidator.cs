namespace DocuMind.Application.Auth;

/// <summary>
/// Pure, I/O-free validation of an avatar upload candidate. Mirrors
/// <see cref="Documents.DocumentUploadValidator"/>: the browser-supplied content type is only
/// used for an early rejection and to pick a file extension — the real gatekeeping is the
/// magic-byte signature check against the file's actual content.
/// </summary>
public static class AvatarUploadValidator
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypeExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
    };

    public static string? ValidateMetadata(string? contentType, long declaredLength)
    {
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypeExtensions.ContainsKey(contentType))
        {
            return "Only PNG, JPEG, or WEBP images are supported.";
        }

        if (declaredLength <= 0)
        {
            return "The file is empty.";
        }

        if (declaredLength > MaxFileSizeBytes)
        {
            var maxMb = MaxFileSizeBytes / (1024.0 * 1024.0);
            return $"The image exceeds the maximum allowed size of {maxMb:0.#} MB.";
        }

        return null;
    }

    /// <summary>Checks the leading bytes against known PNG/JPEG signatures. WEBP is checked via <see cref="HasValidWebpSignature"/>.</summary>
    public static bool HasValidImageSignature(ReadOnlySpan<byte> header)
    {
        return HasSignature(header, PngSignature) || HasSignature(header, JpegSignature) || HasValidWebpSignature(header);
    }

    private static bool HasValidWebpSignature(ReadOnlySpan<byte> header)
    {
        // RIFF....WEBP: bytes 0-3 "RIFF", bytes 8-11 "WEBP".
        return header.Length >= 12
            && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
            && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';
    }

    private static bool HasSignature(ReadOnlySpan<byte> header, byte[] signature)
    {
        return header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);
    }

    public static string ExtensionFor(string contentType) => AllowedContentTypeExtensions[contentType];
}
