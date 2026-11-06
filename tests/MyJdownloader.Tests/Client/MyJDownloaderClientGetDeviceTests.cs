namespace MyJdownloader.Tests.Client;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class MyJDownloaderClientGetDeviceTests
{
    private const string VersionPath = "/jd/version";
    private const string DeviceIdParamName = "deviceId";
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static MyJdSession CreateSession() =>
        new("user@example.com", Key(1), Key(2), "0011aabb", "ccdd2233", Key(3), Key(4));

    private static IEnumerable<string?> InvalidDeviceIds()
    {
        yield return null;
        yield return string.Empty;
        yield return " ";
        yield return new string('a', 257);
        yield return "a\u0001";
        yield return "a\uD800";
    }

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static Rig CreateRig()
    {
        var requests = new List<Sent>();
        var server = new FakeMyJdServer(request =>
        {
            requests.Add(new Sent(request.Method, request.RequestUri!.PathAndQuery));
            var rid = FakeMyJdServer.BodyRidOf(request, Key(4));
            return FakeMyJdServer.EncryptedJson(
                Key(4),
                string.Create(CultureInfo.InvariantCulture, $"{{\"data\":true,\"rid\":{rid}}}"));
        });
        return new Rig(new MyJDownloaderClient(new HttpClient(server)), requests);
    }

    [Test]
    public void GetDevice_ValidId_ReturnsDeviceWithId()
    {
        var rig = CreateRig();

        var device = rig.Client.GetDevice("dev1");

        Assert.Multiple(() =>
        {
            Assert.That(device.Id, Is.EqualTo("dev1"));
            Assert.That(device.Channel, Is.Not.Null);
        });
    }

    [Test]
    public async Task GetDevice_ChannelCall_PostsToDevicePath()
    {
        var rig = CreateRig();
        rig.Client.RestoreSession(CreateSession());
        var device = rig.Client.GetDevice("dev1");

        await device.Channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        var request = rig.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.PathAndQuery, Is.EqualTo("/t_0011aabb_dev1/jd/version"));
        });
    }

    [Test]
    public async Task GetDevice_IdWithSlash_IsEscapedInPath()
    {
        var rig = CreateRig();
        rig.Client.RestoreSession(CreateSession());
        var device = rig.Client.GetDevice("dev/1");

        await device.Channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        Assert.That(rig.Requests.Single().PathAndQuery, Is.EqualTo("/t_0011aabb_dev%2F1/jd/version"));
    }

    [TestCaseSource(nameof(InvalidDeviceIds))]
    public void GetDevice_InvalidId_Throws(string? deviceId)
    {
        var rig = CreateRig();

        var thrown = Assert.Catch<ArgumentException>(() => rig.Client.GetDevice(deviceId!));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo(DeviceIdParamName));
            Assert.That(rig.Requests, Is.Empty);
        });
    }

    [Test]
    public void GetDevice_CalledTwice_ReturnsDistinctInstances()
    {
        var rig = CreateRig();

        var first = rig.Client.GetDevice("dev1");
        var second = rig.Client.GetDevice("dev1");

        Assert.That(second, Is.Not.SameAs(first));
    }

    [Test]
    public async Task GetDevice_BeforeSession_CallThrowsNotConnected()
    {
        var rig = CreateRig();
        var device = rig.Client.GetDevice("dev1");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => device.Channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.Message, Is.EqualTo("The client is not connected."));
            Assert.That(rig.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task GetDevice_BeforeRestore_WorksAfterRestore()
    {
        var rig = CreateRig();
        var device = rig.Client.GetDevice("dev1");
        rig.Client.RestoreSession(CreateSession());

        await device.Channel.CallAsync(VersionPath, []).WaitAsync(_waitTimeout);

        Assert.That(rig.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetDevice_ToString_DoesNotContainSessionToken()
    {
        var rig = CreateRig();
        rig.Client.RestoreSession(CreateSession());
        var device = rig.Client.GetDevice("dev1");

        Assert.That(device.ToString(), Does.Not.Contain("0011aabb"));
    }

    private sealed record Sent(HttpMethod Method, string PathAndQuery);

    private sealed record Rig(MyJDownloaderClient Client, List<Sent> Requests);
}
