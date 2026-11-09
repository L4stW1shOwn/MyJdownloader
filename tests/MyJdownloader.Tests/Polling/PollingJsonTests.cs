namespace MyJdownloader.Tests.Polling;

using System.Text.Json;
using MyJdownloader.Polling;

[TestFixture]
public sealed class PollingJsonTests
{
    [Test]
    public void PollingResult_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            [{"eventName":"jdState","eventData":{"data":"RUNNING"}},{"eventName":"linkGrabberState","eventData":{"data":"IDLE"}}]
            """;

        var items = JsonSerializer.Deserialize(payload, PollingJsonContext.Default.ListPollingResult)!;

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].EventName, Is.EqualTo("jdState"));
            Assert.That(items[0].EventData!.Value.GetProperty("data").GetString(), Is.EqualTo("RUNNING"));
            Assert.That(items[1].EventName, Is.EqualTo("linkGrabberState"));
        });
    }

    [Test]
    public void PollingResult_UnknownFields_AreSkipped()
    {
        const string payload = /*lang=json,strict*/ """
            [{"eventName":"jdState","extra":1,"eventData":{"data":"IDLE"}}]
            """;

        var items = JsonSerializer.Deserialize(payload, PollingJsonContext.Default.ListPollingResult)!;

        Assert.That(items[0].EventName, Is.EqualTo("jdState"));
    }

    [Test]
    public void PollingResult_MissingFields_AreNull()
    {
        var items = JsonSerializer.Deserialize("[{}]", PollingJsonContext.Default.ListPollingResult)!;

        Assert.Multiple(() =>
        {
            Assert.That(items[0].EventName, Is.Null);
            Assert.That(items[0].EventData, Is.Null);
        });
    }

    [Test]
    public void PollingResult_ToString_HidesEventData()
    {
        var result = new PollingResult
        {
            EventName = "jdState",
            EventData = JsonSerializer.Deserialize(/*lang=json,strict*/ """{"data":"secret"}""", PollingJsonContext.Default.NullableJsonElement),
        };

        var text = result.ToString();

        Assert.That(text, Does.Contain("jdState"));
        Assert.That(text, Does.Contain("<Object>"));
        Assert.That(text, Does.Not.Contain("secret"));
    }

    [Test]
    public void PollingResult_Equality_IgnoresToStringOverride()
    {
        var first = new PollingResult { EventName = "jdState" };
        var second = new PollingResult { EventName = "jdState" };

        Assert.That(first, Is.EqualTo(second));
    }
}
