namespace MyJdownloader.Tests.Json;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.Json;

[TestFixture]
public sealed class TolerantEnumConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters =
        {
            new TolerantEnumConverter<AvailableLinkState>(),
            new TolerantEnumConverter<Priority>(),
        },
    };

    [Test]
    public void TolerantEnumConverter_KnownValue_Reads()
    {
        var state = JsonSerializer.Deserialize<AvailableLinkState>(/*lang=json,strict*/ "\"TEMP_UNKNOWN\"", _options);
        var priority = JsonSerializer.Deserialize<Priority>(/*lang=json,strict*/ "\"HIGHEST\"", _options);

        Assert.That(state, Is.EqualTo(AvailableLinkState.TempUnknown));
        Assert.That(priority, Is.EqualTo(Priority.Highest));
    }

    [Test]
    public void TolerantEnumConverter_UnknownValue_ReadsUnknown()
    {
        var fromNew = JsonSerializer.Deserialize<Priority>(/*lang=json,strict*/ "\"SOMETHING_NEW\"", _options);
        var fromUnknown = JsonSerializer.Deserialize<Priority>(/*lang=json,strict*/ "\"UNKNOWN\"", _options);

        Assert.That(fromNew, Is.EqualTo(Priority.Unknown));
        Assert.That(fromUnknown, Is.EqualTo(Priority.Unknown));
    }

    [Test]
    public void TolerantEnumConverter_Write_UsesUpperSnake()
    {
        var json = JsonSerializer.Serialize(Priority.Default, _options);

        Assert.That(json, Is.EqualTo(/*lang=json,strict*/ "\"DEFAULT\""));
    }

    [Test]
    public void TolerantEnumConverter_WriteUnknown_Throws()
        => Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(Priority.Unknown, _options));

    [TestCase(/*lang=json,strict*/ "\"42\"")]
    [TestCase(/*lang=json,strict*/ "\"HIGH,LOW\"")]
    public void TolerantEnumConverter_NumericOrCompositeString_ReadsUnknown(string json)
    {
        var priority = JsonSerializer.Deserialize<Priority>(json, _options);

        Assert.That(priority, Is.EqualTo(Priority.Unknown));
    }

    [Test]
    public void TolerantEnumConverter_JsonNumber_Throws()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Priority>(/*lang=json,strict*/ "3", _options));

    [Test]
    public void TolerantEnumConverter_WriteUndefinedValue_Throws()
        => Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize((Priority)42, _options));

    [Test]
    public void Priority_DefaultReflectionOptions_SerializationUnaffected()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

        Assert.That(() => JsonSerializer.Serialize(Priority.Unknown, options), Throws.Nothing);
    }
}
