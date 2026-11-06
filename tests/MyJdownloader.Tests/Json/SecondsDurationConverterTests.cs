namespace MyJdownloader.Tests.Json;

using System.Text.Json;
using MyJdownloader.Json;

[TestFixture]
public sealed class SecondsDurationConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new SecondsDurationConverter() },
    };

    [Test]
    public void SecondsDurationConverter_Positive_Reads()
    {
        var value = JsonSerializer.Deserialize<TimeSpan?>(/*lang=json,strict*/ "90", _options);

        Assert.That(value, Is.EqualTo(TimeSpan.FromSeconds(90)));
    }

    [Test]
    public void SecondsDurationConverter_Zero_ReadsZero()
    {
        var value = JsonSerializer.Deserialize<TimeSpan?>(/*lang=json,strict*/ "0", _options);

        Assert.That(value, Is.EqualTo(TimeSpan.Zero));
    }

    [TestCase("-1")]
    [TestCase("null")]
    public void SecondsDurationConverter_NegativeOrNull_ReadsNull(string json)
    {
        var value = JsonSerializer.Deserialize<TimeSpan?>(json, _options);

        Assert.That(value, Is.Null);
    }

    [Test]
    public void SecondsDurationConverter_Write_RoundTrips()
    {
        TimeSpan? value = TimeSpan.FromSeconds(90);

        var json = JsonSerializer.Serialize(value, _options);

        Assert.That(json, Is.EqualTo("90"));
    }

    [Test]
    public void SecondsDurationConverter_OutOfRange_ThrowsJsonException()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TimeSpan?>(/*lang=json,strict*/ "9223372036854775807", _options));

    [Test]
    public void SecondsDurationConverter_Fractional_ThrowsJsonException()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TimeSpan?>(/*lang=json,strict*/ "1.5", _options));
}
