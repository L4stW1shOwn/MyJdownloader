namespace MyJdownloader.Tests.Json;

using System.Text.Json;
using MyJdownloader.Json;

[TestFixture]
public sealed class UnixMillisecondsConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new UnixMillisecondsConverter() },
    };

    [Test]
    public void UnixMillisecondsConverter_Positive_Reads()
    {
        var value = JsonSerializer.Deserialize<DateTimeOffset?>(/*lang=json,strict*/ "1730900000000", _options);

        Assert.That(value, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1730900000000)));
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("null")]
    public void UnixMillisecondsConverter_ZeroNegativeOrNull_ReadsNull(string json)
    {
        var value = JsonSerializer.Deserialize<DateTimeOffset?>(json, _options);

        Assert.That(value, Is.Null);
    }

    [Test]
    public void UnixMillisecondsConverter_Write_RoundTrips()
    {
        DateTimeOffset? value = DateTimeOffset.FromUnixTimeMilliseconds(1730900000000);

        var json = JsonSerializer.Serialize(value, _options);

        Assert.That(json, Is.EqualTo("1730900000000"));
    }

    [Test]
    public void UnixMillisecondsConverter_WriteNull_WritesJsonNull()
    {
        var json = JsonSerializer.Serialize<DateTimeOffset?>(null, _options);

        Assert.That(json, Is.EqualTo("null"));
    }

    [Test]
    public void UnixMillisecondsConverter_OutOfRange_ThrowsJsonException()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset?>(/*lang=json,strict*/ "253402300800000", _options));

    [Test]
    public void UnixMillisecondsConverter_Fractional_ThrowsJsonException()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset?>(/*lang=json,strict*/ "1.5", _options));
}
