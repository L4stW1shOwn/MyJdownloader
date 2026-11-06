namespace MyJdownloader.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads and writes a timestamp as a JSON number of milliseconds since the Unix epoch. A value of zero or
/// less, or <c>null</c>, is the API's "not set" sentinel and is read as <see langword="null"/>.
/// A non-integer value or one beyond year 9999 throws a <see cref="JsonException"/>.
/// </summary>
internal sealed class UnixMillisecondsConverter : JsonConverter<DateTimeOffset?>
{
    private const long MaxMilliseconds = 253402300799999;

    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out var milliseconds))
        {
            throw new JsonException("A timestamp must be an integer number of milliseconds.");
        }

        if (milliseconds > MaxMilliseconds)
        {
            throw new JsonException("The timestamp is out of the representable range.");
        }

        return milliseconds <= 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteNumberValue(value.Value.ToUnixTimeMilliseconds());
    }
}
