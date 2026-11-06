namespace MyJdownloader.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads and writes a duration as a JSON number of whole seconds. A negative value or <c>null</c> means
/// "unknown" and is read as <see langword="null"/>.
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

        var seconds = reader.GetInt64();
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
