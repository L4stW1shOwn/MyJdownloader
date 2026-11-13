namespace MyJdownloader.Config;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Reads and writes the enumeration options of a configuration entry as the API's array of
/// <c>[name, label]</c> pairs. A missing or <c>null</c> label is read as <see langword="null"/>.
/// Any other shape throws a <see cref="JsonException"/>.
/// </summary>
internal sealed class EnumOptionPairsConverter : JsonConverter<IReadOnlyList<ConfigEnumOption>?>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override IReadOnlyList<ConfigEnumOption>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw Malformed();
        }

        var result = new List<ConfigEnumOption>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw Malformed();
            }

            result.Add(ReadPair(ref reader));
        }

        return result;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<ConfigEnumOption>? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        foreach (var option in value)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(option.Name);
            writer.WriteStringValue(option.Label);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }

    private static ConfigEnumOption ReadPair(ref Utf8JsonReader reader)
    {
        string? name = null;
        string? label = null;
        var count = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.Null) || count >= 2)
            {
                throw Malformed();
            }

            if (count == 0)
            {
                name = reader.GetString();
            }
            else
            {
                label = reader.GetString();
            }

            count++;
        }

        return new ConfigEnumOption { Name = name, Label = label };
    }

    private static JsonException Malformed() => new("The enumeration options must be an array of [name, label] string pairs.");
}
