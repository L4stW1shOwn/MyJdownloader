namespace MyJdownloader.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads and writes a timestamp as a JSON number of milliseconds since the Unix epoch. A value of zero or
/// less, or <c>null</c>, is the API's "not set" sentinel and is read as <see langword="null"/>.
/// </summary>
internal sealed class UnixMillisecondsConverter : JsonConverter<DateTimeOffset?>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var milliseconds = reader.GetInt64();
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
