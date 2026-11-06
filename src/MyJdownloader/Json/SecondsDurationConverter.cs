namespace MyJdownloader.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads and writes a duration as a JSON number of whole seconds. A negative value or <c>null</c> means
/// "unknown" and is read as <see langword="null"/>. A non-integer or out-of-range value throws a
/// <see cref="JsonException"/>. Writing truncates to whole seconds.
/// </summary>
internal sealed class SecondsDurationConverter : JsonConverter<TimeSpan?>
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

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out var seconds))
        {
            throw new JsonException("A duration must be an integer number of seconds.");
        }

        if (seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new JsonException("The duration is out of the representable range.");
        }

        return seconds < 0 ? null : TimeSpan.FromSeconds(seconds);
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

        writer.WriteNumberValue((long)value.Value.TotalSeconds);
    }
}
