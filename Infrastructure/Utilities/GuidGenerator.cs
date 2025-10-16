using System.Security.Cryptography;
using System.Text;

namespace VisitorManagementSystem.Api.Infrastructure.Utilities;

/// <summary>
/// Secure GUID generation utility with various formatting options
/// </summary>
public class GuidGenerator
{
    /// <summary>
    /// Generates a new GUID
    /// </summary>
    /// <returns>New GUID</returns>
    public static Guid NewGuid()
    {
        return Guid.NewGuid();
    }

    /// <summary>
    /// Generates a new GUID as string
    /// </summary>
    /// <param name="format">GUID format (N, D, B, P, X)</param>
    /// <returns>GUID as formatted string</returns>
    public static string NewGuidString(string format = "D")
    {
        return Guid.NewGuid().ToString(format);
    }

    /// <summary>
    /// Generates a cryptographically secure GUID
    /// </summary>
    /// <returns>Cryptographically secure GUID</returns>
    public static Guid NewSecureGuid()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

        // Set version to 4 (random)
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);
        // Set variant
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes);
    }

    /// <summary>
    /// Generates a sequential GUID for better database performance
    /// </summary>
    /// <returns>Sequential GUID</returns>
    public static Guid NewSequentialGuid()
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var now = DateTime.UtcNow;

        // Convert to binary
        var daysArray = BitConverter.GetBytes(now.Subtract(new DateTime(1900, 1, 1)).Days);
        var msecsArray = BitConverter.GetBytes((long)(now.TimeOfDay.TotalMilliseconds / 3.333333));

        // Reverse the arrays to match SQL Server's ordering
        Array.Reverse(daysArray);
        Array.Reverse(msecsArray);

        // Copy the bytes into the GUID
        Array.Copy(daysArray, daysArray.Length - 2, guidBytes, guidBytes.Length - 6, 2);
        Array.Copy(msecsArray, msecsArray.Length - 4, guidBytes, guidBytes.Length - 4, 4);

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Generates a short GUID (22 characters)
    /// </summary>
    /// <returns>Short GUID string</returns>
    public static string NewShortGuid()
    {
        var guid = Guid.NewGuid();
        var base64 = Convert.ToBase64String(guid.ToByteArray());
        return base64.Replace("/", "_").Replace("+", "-").Substring(0, 22);
    }

    /// <summary>
    /// Generates a URL-safe short GUID
    /// </summary>
    /// <returns>URL-safe short GUID</returns>
    public static string NewUrlSafeGuid()
    {
        var guid = Guid.NewGuid();
        var base64 = Convert.ToBase64String(guid.ToByteArray());
        return base64.Replace("/", "_").Replace("+", "-").TrimEnd('=');
    }

    /// <summary>
    /// Generates a GUID from a string using SHA-1 hash (deterministic)
    /// </summary>
    /// <param name="input">Input string</param>
    /// <returns>Deterministic GUID</returns>
    public static Guid NewGuidFromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        // Set version to 5 (name-based with SHA-1)
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        // Set variant
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Generates a GUID with specific namespace and name
    /// </summary>
    /// <param name="namespaceId">Namespace GUID</param>
    /// <param name="name">Name string</param>
    /// <returns>Namespaced GUID</returns>
    public static Guid NewNamespaceGuid(Guid namespaceId, string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        var namespaceBytes = namespaceId.ToByteArray();
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var combined = new byte[namespaceBytes.Length + nameBytes.Length];

        Array.Copy(namespaceBytes, combined, namespaceBytes.Length);
        Array.Copy(nameBytes, 0, combined, namespaceBytes.Length, nameBytes.Length);

        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(combined);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        // Set version to 5 (name-based with SHA-1)
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        // Set variant
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Generates a correlation ID for request tracking
    /// </summary>
    /// <returns>Correlation ID string</returns>
    public static string NewCorrelationId()
    {
        return $"corr_{NewShortGuid()}";
    }

    /// <summary>
    /// Generates a request ID for API tracking
    /// </summary>
    /// <returns>Request ID string</returns>
    public static string NewRequestId()
    {
        return $"req_{NewShortGuid()}";
    }

    /// <summary>
    /// Generates a session ID
    /// </summary>
    /// <returns>Session ID string</returns>
    public static string NewSessionId()
    {
        return $"sess_{NewShortGuid()}";
    }

    /// <summary>
    /// Generates a JWT ID (JTI)
    /// </summary>
    /// <returns>JWT ID string</returns>
    public static string NewJwtId()
    {
        return NewShortGuid();
    }

    /// <summary>
    /// Generates a security stamp
    /// </summary>
    /// <returns>Security stamp string</returns>
    public static string NewSecurityStamp()
    {
        return NewSecureGuid().ToString("N").ToUpperInvariant();
    }

    /// <summary>
    /// Generates a device fingerprint ID
    /// </summary>
    /// <returns>Device fingerprint string</returns>
    public static string NewDeviceFingerprint()
    {
        return $"dev_{NewShortGuid()}";
    }

    /// <summary>
    /// Generates an API key
    /// </summary>
    /// <param name="prefix">Optional prefix</param>
    /// <returns>API key string</returns>
    public static string NewApiKey(string prefix = "vms")
    {
        var guid1 = NewSecureGuid().ToString("N");
        var guid2 = NewSecureGuid().ToString("N");
        return $"{prefix}_{guid1}{guid2}";
    }

    /// <summary>
    /// Generates a webhook secret
    /// </summary>
    /// <returns>Webhook secret string</returns>
    public static string NewWebhookSecret()
    {
        return $"whsec_{NewSecureGuid().ToString("N")}";
    }

    /// <summary>
    /// Validates if string is a valid GUID
    /// </summary>
    /// <param name="guidString">String to validate</param>
    /// <returns>True if valid GUID</returns>
    public static bool IsValidGuid(string guidString)
    {
        return Guid.TryParse(guidString, out _);
    }

    /// <summary>
    /// Tries to parse a GUID from string
    /// </summary>
    /// <param name="guidString">String to parse</param>
    /// <param name="guid">Parsed GUID</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParseGuid(string guidString, out Guid guid)
    {
        return Guid.TryParse(guidString, out guid);
    }

    /// <summary>
    /// Converts GUID to different formats
    /// </summary>
    /// <param name="guid">GUID to convert</param>
    /// <param name="format">Target format</param>
    /// <returns>Formatted GUID string</returns>
    public static string FormatGuid(Guid guid, GuidFormat format)
    {
        return format switch
        {
            GuidFormat.Digits => guid.ToString("N"),
            GuidFormat.Hyphens => guid.ToString("D"),
            GuidFormat.Braces => guid.ToString("B"),
            GuidFormat.Parentheses => guid.ToString("P"),
            GuidFormat.Hex => guid.ToString("X"),
            GuidFormat.Short => ConvertToShortGuid(guid),
            GuidFormat.UrlSafe => ConvertToUrlSafeGuid(guid),
            _ => guid.ToString()
        };
    }

    /// <summary>
    /// Converts GUID to short format
    /// </summary>
    /// <param name="guid">GUID to convert</param>
    /// <returns>Short GUID string</returns>
    private static string ConvertToShortGuid(Guid guid)
    {
        var base64 = Convert.ToBase64String(guid.ToByteArray());
        return base64.Replace("/", "_").Replace("+", "-").Substring(0, 22);
    }

    /// <summary>
    /// Converts GUID to URL-safe format
    /// </summary>
    /// <param name="guid">GUID to convert</param>
    /// <returns>URL-safe GUID string</returns>
    private static string ConvertToUrlSafeGuid(Guid guid)
    {
        var base64 = Convert.ToBase64String(guid.ToByteArray());
        return base64.Replace("/", "_").Replace("+", "-").TrimEnd('=');
    }

    /// <summary>
    /// Generates multiple GUIDs
    /// </summary>
    /// <param name="count">Number of GUIDs to generate</param>
    /// <returns>Array of GUIDs</returns>
    public static Guid[] NewGuids(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        var guids = new Guid[count];
        for (int i = 0; i < count; i++)
        {
            guids[i] = Guid.NewGuid();
        }
        return guids;
    }

    /// <summary>
    /// Checks if GUID is empty
    /// </summary>
    /// <param name="guid">GUID to check</param>
    /// <returns>True if empty</returns>
    public static bool IsEmpty(Guid guid)
    {
        return guid == Guid.Empty;
    }

    /// <summary>
    /// Gets GUID version
    /// </summary>
    /// <param name="guid">GUID to check</param>
    /// <returns>GUID version</returns>
    public static int GetGuidVersion(Guid guid)
    {
        var bytes = guid.ToByteArray();
        return (bytes[7] & 0xF0) >> 4;
    }

    /// <summary>
    /// Gets GUID variant
    /// </summary>
    /// <param name="guid">GUID to check</param>
    /// <returns>GUID variant</returns>
    public static int GetGuidVariant(Guid guid)
    {
        var bytes = guid.ToByteArray();
        return (bytes[8] & 0xC0) >> 6;
    }
}

/// <summary>
/// GUID format options
/// </summary>
public enum GuidFormat
{
    /// <summary>
    /// 32 digits: 00000000000000000000000000000000
    /// </summary>
    Digits,

    /// <summary>
    /// 32 digits with hyphens: 00000000-0000-0000-0000-000000000000
    /// </summary>
    Hyphens,

    /// <summary>
    /// 32 digits with hyphens in braces: {00000000-0000-0000-0000-000000000000}
    /// </summary>
    Braces,

    /// <summary>
    /// 32 digits with hyphens in parentheses: (00000000-0000-0000-0000-000000000000)
    /// </summary>
    Parentheses,

    /// <summary>
    /// Hexadecimal format: {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
    /// </summary>
    Hex,

    /// <summary>
    /// Base64 encoded (22 characters)
    /// </summary>
    Short,

    /// <summary>
    /// URL-safe Base64 encoded
    /// </summary>
    UrlSafe
}