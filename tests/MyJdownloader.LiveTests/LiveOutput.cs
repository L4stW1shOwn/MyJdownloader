namespace MyJdownloader.LiveTests;

using System.Buffers;
using System.Text;
using System.Text.Json;

/// <summary>Writes service responses to the test output with sensitive values masked.</summary>
public static class LiveOutput
{
    private const string MaskedValue = "***";

    private static readonly string[] _sensitiveFragments =
        ["pass", "token", "secret", "key", "cookie", "auth", "mail", "user", "login", "pwd", "credential", "session", "signature"];

    /// <summary>Writes a masked, indented copy of <paramref name="json"/> to the test output.</summary>
    /// <param name="label">A label that MUST be a test constant, never data coming from the service.</param>
    /// <param name="json">The service response to display.</param>
    public static void Dump(string label, JsonElement json)
    {
        TestContext.Out.WriteLine($"--- {label} ---");
        TestContext.Out.WriteLine(Mask(json));
    }

    internal static string Mask(JsonElement json)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            Write(writer, json);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(MaskUrl(property.Name));
                    if (IsSensitive(property.Name))
                    {
                        writer.WriteStringValue(MaskedValue);
                    }
                    else
                    {
                        Write(writer, property.Value);
                    }
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    Write(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(MaskUrl(element.GetString()!));
                break;
            case JsonValueKind.Undefined:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool IsSensitive(string name)
    {
        foreach (var fragment in _sensitiveFragments)
        {
            if (name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string MaskUrl(string value)
    {
        if (!value.Contains("://", StringComparison.Ordinal))
        {
            return value;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.IsFile || uri.IsUnc || uri.Host.Length == 0)
        {
            return MaskedValue;
        }

        var builder = new StringBuilder(uri.Scheme).Append("://");
        if (uri.UserInfo.Length > 0)
        {
            builder.Append(MaskedValue).Append('@');
        }

        builder.Append(uri.Host);
        if (!uri.IsDefaultPort)
        {
            builder.Append(':').Append(uri.Port);
        }

        // Schemes without query support (ftp, sftp...) keep '?' and '#' escaped inside the path.
        var path = uri.AbsolutePath;
        var cut = path.IndexOf('%', StringComparison.Ordinal);
        var hasQuery = uri.Query.Length > 0;
        while (cut >= 0)
        {
            if (path.AsSpan(cut).StartsWith("%3F", StringComparison.OrdinalIgnoreCase)
                || path.AsSpan(cut).StartsWith("%23", StringComparison.OrdinalIgnoreCase))
            {
                hasQuery = true;
                path = path[..cut];
                break;
            }

            cut = path.IndexOf('%', cut + 1);
        }

        builder.Append(path);
        if (hasQuery)
        {
            builder.Append('?').Append(MaskedValue);
        }

        return builder.ToString();
    }
}
