namespace MyJdownloader.Tests.DownloadEvents;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader.DownloadEvents;
using MyJdownloader.Errors;
using NSubstitute;

[TestFixture]
public sealed class DownloadEventsApiTests
{
    private const string Path = "/downloadevents/setStatusEventInterval";

    private static (IDeviceChannel Channel, DownloadEventsApi Api) Create(bool? returned)
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync(Path, Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(returned));
        return (channel, new DownloadEventsApi(channel));
    }

    [TestCase(1500.0, "1500")]
    [TestCase(1.0, "1")]
    [TestCase(1.9, "1")]
    [TestCase(1000.0, "1000")]
    public async Task SetStatusEventIntervalAsync_Interval_SendsIdAndWholeMilliseconds(double milliseconds, string expected)
    {
        var (channel, api) = Create(true);

        await api.SetStatusEventIntervalAsync(42, TimeSpan.FromMilliseconds(milliseconds));

        var received = channel.ReceivedCalls().Single().GetArguments();
        var args = (IReadOnlyList<JsonElement>)received[2]!;
        Assert.Multiple(() =>
        {
            Assert.That((string)received[0]!, Is.EqualTo(Path));
            Assert.That(args, Has.Count.EqualTo(2));
            Assert.That(args[0].GetRawText(), Is.EqualTo("42"));
            Assert.That(args[1].GetRawText(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void SetStatusEventIntervalAsync_SubMillisecondInterval_ThrowsWithoutCall()
    {
        var (channel, api) = Create(true);

        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() =>
            api.SetStatusEventIntervalAsync(1, TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond - 1)));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("interval"));
            Assert.That(channel.ReceivedCalls(), Is.Empty);
        });
    }

    [Test]
    public void SetStatusEventIntervalAsync_BelowOneMillisecond_MessageOmitsValue()
    {
        var (_, api) = Create(true);

        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() =>
            api.SetStatusEventIntervalAsync(1, TimeSpan.FromMilliseconds(-987654)));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo("interval"));
            Assert.That(thrown.Message, Does.Not.Contain("987654"));
            Assert.That(thrown.ActualValue, Is.Null);
        });
    }

    [TestCase(0L)]
    [TestCase(-10000L)]
    public void SetStatusEventIntervalAsync_ZeroOrNegativeInterval_ThrowsWithoutCall(long ticks)
    {
        var (channel, api) = Create(true);

        Assert.Throws<ArgumentOutOfRangeException>(() => api.SetStatusEventIntervalAsync(1, TimeSpan.FromTicks(ticks)));
        Assert.That(channel.ReceivedCalls(), Is.Empty);
    }

    [Test]
    public async Task SetStatusEventIntervalAsync_MaxInterval_IsSent()
    {
        var (channel, api) = Create(true);

        await api.SetStatusEventIntervalAsync(1, TimeSpan.MaxValue);

        var args = (IReadOnlyList<JsonElement>)channel.ReceivedCalls().Single().GetArguments()[2]!;
        Assert.That(args[1].GetInt64(), Is.EqualTo((long)TimeSpan.MaxValue.TotalMilliseconds));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SetStatusEventIntervalAsync_Result_IsReturned(bool returned)
    {
        var (_, api) = Create(returned);

        Assert.That(await api.SetStatusEventIntervalAsync(1, TimeSpan.FromSeconds(1)), Is.EqualTo(returned));
    }

    [Test]
    public void SetStatusEventIntervalAsync_NullResult_ThrowsProtocolException()
    {
        var (_, api) = Create(null);

        Assert.ThrowsAsync<MyJdProtocolException>(() => api.SetStatusEventIntervalAsync(1, TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task SetStatusEventIntervalAsync_Token_PassesTokenToChannel()
    {
        using var cts = new CancellationTokenSource();
        var (channel, api) = Create(true);

        await api.SetStatusEventIntervalAsync(1, TimeSpan.FromSeconds(1), cts.Token);

        Assert.That(channel.ReceivedCalls().Single().GetArguments().Last(), Is.EqualTo(cts.Token));
    }
}
