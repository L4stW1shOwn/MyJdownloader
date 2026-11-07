namespace MyJdownloader.Tests.Events;

using System.Text.Json;
using MyJdownloader.Events;

[TestFixture]
public sealed class EventsJsonTests
{
    private static readonly string[] _subscriptions = ["downloads.*"];
    private static readonly string[] _eventIds = ["a", "b"];

    private static DeviceEvent DeserializeEvent(string objectJson)
    {
        var events = JsonSerializer.Deserialize("[" + objectJson + "]", EventsJsonContext.Default.ListDeviceEvent);
        return events![0];
    }

    [Test]
    public void EventSubscription_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"subscriptionid":5,"subscribed":true,"subscriptions":["downloads.*"],"exclusions":[],"maxPolltimeout":25000,"maxKeepalive":120000,"extra":1}
            """;

        var subscription = JsonSerializer.Deserialize(payload, EventsJsonContext.Default.EventSubscription);

        Assert.That(subscription, Is.Not.Null);
        Assert.That(subscription!.SubscriptionId, Is.EqualTo(5L));
        Assert.That(subscription.Subscribed, Is.True);
        Assert.That(subscription.Subscriptions, Is.EqualTo(_subscriptions));
        Assert.That(subscription.Exclusions, Is.Empty);
        Assert.That(subscription.MaxPollTimeout, Is.EqualTo(TimeSpan.FromSeconds(25)));
        Assert.That(subscription.MaxKeepalive, Is.EqualTo(TimeSpan.FromSeconds(120)));
    }

    [Test]
    public void EventSubscriptionStatus_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"subscriptionid":5,"subscribed":false,"queueSize":3}
            """;

        var status = JsonSerializer.Deserialize(payload, EventsJsonContext.Default.EventSubscriptionStatus);

        Assert.That(status, Is.Not.Null);
        Assert.That(status!.SubscriptionId, Is.EqualTo(5L));
        Assert.That(status.Subscribed, Is.False);
        Assert.That(status.QueueSize, Is.EqualTo(3));
    }

    [Test]
    public void EventPublisher_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            [{"publisher":"downloads","eventids":["a","b"]}]
            """;

        var publishers = JsonSerializer.Deserialize(payload, EventsJsonContext.Default.ListEventPublisher);

        Assert.That(publishers, Has.Count.EqualTo(1));
        Assert.That(publishers![0].Publisher, Is.EqualTo("downloads"));
        Assert.That(publishers[0].EventIds, Is.EqualTo(_eventIds));
    }

    [Test]
    public void DeviceEvent_Sample_Deserializes()
    {
        var deviceEvent = DeserializeEvent(/*lang=json,strict*/ """
            {"publisher":"downloadwatchdog","eventid":"RUNNING_STATE","eventData":{"data":"RUNNING"}}
            """);

        Assert.That(deviceEvent.Publisher, Is.EqualTo("downloadwatchdog"));
        Assert.That(deviceEvent.EventId, Is.EqualTo("RUNNING_STATE"));
        Assert.That(deviceEvent.Data!.Value.GetProperty("data").GetString(), Is.EqualTo("RUNNING"));
    }

    [Test]
    public void DeviceEvent_LowerCaseKeys_Deserializes()
    {
        var lower = DeserializeEvent(/*lang=json,strict*/ """
            {"publisher":"p","eventid":"E","eventdata":{"k":1}}
            """);
        var camel = DeserializeEvent(/*lang=json,strict*/ """
            {"publisher":"p","eventId":"E","eventData":{"k":1}}
            """);

        Assert.That(lower.EventId, Is.EqualTo("E"));
        Assert.That(lower.Data, Is.Not.Null);
        Assert.That(camel.EventId, Is.EqualTo("E"));
        Assert.That(camel.Data, Is.Not.Null);
    }

    [Test]
    public void DeviceEvent_NullData_ReadsNull()
    {
        var deviceEvent = DeserializeEvent(/*lang=json,strict*/ """
            {"publisher":"p","eventid":"E","eventData":null}
            """);

        Assert.That(deviceEvent.Data, Is.Null);
    }

    [Test]
    public void DeviceEvent_ToString_HidesData()
    {
        using var document = JsonDocument.Parse(/*lang=json,strict*/ """{"password":"s3cret"}""");
        var deviceEvent = new DeviceEvent { Publisher = "p", EventId = "E", Data = document.RootElement };

        var text = deviceEvent.ToString();

        Assert.That(text, Does.Contain("<Object>"));
        Assert.That(text, Does.Not.Contain("s3cret"));
    }
}
