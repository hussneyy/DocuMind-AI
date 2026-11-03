using System.Security.Cryptography;

namespace DocuMind.IntegrationTests;

/// <summary>
/// A minimal RFC 6238 TOTP code generator, so tests can produce a real authenticator code from
/// the base32 shared key the API returns, instead of mocking time or the token provider.
/// </summary>
public static class TotpTestHelper
{
    public static string GenerateCode(string base32Secret)
    {
        var secretBytes = Base32Decode(base32Secret);
        var timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var timestepBytes = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timestepBytes);
        }

        var hash = HMACSHA1.HashData(secretBytes, timestepBytes);
        var offset = hash[^1] & 0xF;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        var code = binaryCode % 1_000_000;
        return code.ToString("D6");
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.TrimEnd('=').ToUpperInvariant();

        var bits = new List<bool>();
        foreach (var c in base32)
        {
            var value = alphabet.IndexOf(c);
            for (var i = 4; i >= 0; i--)
            {
                bits.Add(((value >> i) & 1) == 1);
            }
        }

        var bytes = new byte[bits.Count / 8];
        for (var i = 0; i < bytes.Length; i++)
        {
            byte b = 0;
            for (var j = 0; j < 8; j++)
            {
                b = (byte)((b << 1) | (bits[(i * 8) + j] ? 1 : 0));
            }

            bytes[i] = b;
        }

        return bytes;
    }
}
