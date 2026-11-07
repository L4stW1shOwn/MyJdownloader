namespace MyJdownloader.Tests.Json;

using System.Text.Json;
using MyJdownloader.Json;

[TestFixture]
public sealed class MillisecondsDurationConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new MillisecondsDurationConverter() },
    };

    [Test]
    public void MillisecondsDuration_Positive_Reads()
    {
        var value = JsonSerializer.Deserialize<TimeSpan?>(/*lang=json,strict*/ "25000", _options);

        Assert.That(value, Is.EqualTo(TimeSpan.FromSeconds(25)));
    }

    [Test]
    public void MillisecondsDuration_Zero_ReadsZero()
    {
        var value = JsonSerializer.Deserialize<TimeSpan?>(/*lang=json,strict*/ "0", _options);

        Assert.That(value, Is.EqualTo(TimeSpan.Zero));
    }

    [TestCase("-1")]
    [TestCase("null")]
    public void MillisecondsDuration_NegativeOrNull_ReadsNull(string json)
    {
        var value = JsonSerializer.Deserialize<TimeSpan?>(json, _options);

        Assert.That(value, Is.Null);
    }

    [Test]
    public void MillisecondsDuration_Write_WholeMilliseconds()
    {
        TimeSpan? value = TimeSpan.FromMilliseconds(1500);

        var json = JsonSerializer.Serialize(value, _options);

        Assert.That(json, Is.EqualTo("1500"));
    }

    [Test]
    public void MillisecondsDuration_WriteNull_WritesNull()
    {
        var json = JsonSerializer.Serialize<TimeSpan?>(null, _options);

        Assert.That(json, Is.EqualTo("null"));
    }
}
