namespace MyJdownloader.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads and writes a duration as a JSON number of whole milliseconds. A negative value or <c>null</c> means
/// "unknown" and is read as <see langword="null"/>. A non-integer or out-of-range value throws a
/// <see cref="JsonException"/>. Writing truncates to whole milliseconds.
/// </summary>
internal sealed class MillisecondsDurationConverter : JsonConverter<TimeSpan?>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out var milliseconds))
        {
            throw new JsonException("A duration must be an integer number of milliseconds.");
        }

        if (milliseconds > TimeSpan.MaxValue.TotalMilliseconds)
        {
            throw new JsonException("The duration is out of the representable range.");
        }

        return milliseconds < 0 ? null : TimeSpan.FromMilliseconds(milliseconds);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteNumberValue((long)value.Value.TotalMilliseconds);
    }
}
