namespace MyJdownloader.Json;

using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads and writes an enum as an UPPER_SNAKE_CASE JSON string. Any unrecognized string is read as the
/// default (<c>Unknown</c>) member; the default member and undefined values cannot be written.
/// </summary>
/// <typeparam name="TEnum">The enum type; its zero member is the <c>Unknown</c> value.</typeparam>
internal sealed class TolerantEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly FrozenDictionary<string, TEnum> _readMap = BuildReadMap();
    private static readonly FrozenDictionary<TEnum, string> _writeMap = BuildWriteMap();

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a JSON string for an enum value.");
        }

        var text = reader.GetString();
        return text is not null && _readMap.TryGetValue(text, out var value) ? value : default;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!_writeMap.TryGetValue(value, out var name))
        {
            throw new ArgumentException("The enum value cannot be serialized.", nameof(value));
        }

        writer.WriteStringValue(name);
    }

    private static string ToWireName(TEnum value)
        => JsonNamingPolicy.SnakeCaseUpper.ConvertName(Enum.GetName(value)!);

    private static FrozenDictionary<string, TEnum> BuildReadMap()
    {
        var map = new Dictionary<string, TEnum>(StringComparer.Ordinal);
        foreach (var value in Enum.GetValues<TEnum>())
        {
            map[ToWireName(value)] = value;
        }

        return map.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FrozenDictionary<TEnum, string> BuildWriteMap()
    {
        var map = new Dictionary<TEnum, string>();
        foreach (var value in Enum.GetValues<TEnum>())
        {
            if (!EqualityComparer<TEnum>.Default.Equals(value, default))
            {
                map[value] = ToWireName(value);
            }
        }

        return map.ToFrozenDictionary();
    }
}
