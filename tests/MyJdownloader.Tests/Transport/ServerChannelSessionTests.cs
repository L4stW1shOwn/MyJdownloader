namespace MyJdownloader.Tests.Transport;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MyJdownloader.Crypto;
using MyJdownloader.Transport;

[TestFixture]
public sealed class ServerChannelSessionTests
{
    private const string Email = "user@example.com";
    private const string Password = "password";
    private const string SessionToken = "0011aabb";
    private const string RegainToken = "ccdd2233";
    private const string NewSessionToken = "4455eeff";
    private const string NewRegainToken = "66778899";
    private const string MismatchMessage = "The MyJD server response does not match the request.";
    private const string UnreadableMessage = "The MyJD server returned a response that could not be read.";

    private static MyJdSession CreateSession()
    {
        var loginSecret = MyJdCrypto.DeriveLoginSecret(Email, Password);
        var deviceSecret = MyJdCrypto.DeriveDeviceSecret(Email, Password);
        return new MyJdSession(
            Email,
            loginSecret,
            deviceSecret,
            SessionToken,
            RegainToken,
            MyJdCrypto.DeriveToken(loginSecret, SessionToken),
            MyJdCrypto.DeriveToken(deviceSecret, SessionToken));
    }

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static ServerChannel CreateChannel(FakeMyJdServer server)
    {
        var clock = new FakeTimeProvider { UtcNow = new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero) };
        var httpClient = new HttpClient(server);
        return new ServerChannel(httpClient, new MyJDownloaderOptions(), new RequestIdGenerator(clock), clock);
    }

    private static FakeMyJdServer ServerReturning(MyJdSession session, Func<long, string> body) =>
        new(request => FakeMyJdServer.EncryptedJson(session.ServerEncryptionToken.ToArray(), body(FakeMyJdServer.RidOf(request))));

    private static string ReconnectBody(long rid) =>
        string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"{{NewSessionToken}}","regaintoken":"{{NewRegainToken}}","rid":{{rid}}}""");

    private static string RidBody(long rid) => string.Create(CultureInfo.InvariantCulture, $$"""{"rid":{{rid}}}""");

#pragma warning disable JSON002 // Probable JSON string: intentional protocol payloads built as plain strings
    private static string TwoDevicesBody(long rid) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $$"""{"list":[{"id":"d1","name":"Home","type":"jd"},{"id":"d2","name":"NAS","type":"jd"}],"rid":{{rid}}}""");

    private static string EmptyListBody(long rid) => string.Create(CultureInfo.InvariantCulture, $$"""{"list":[],"rid":{{rid}}}""");

    private static string MissingIdBody(long rid) =>
        string.Create(CultureInfo.InvariantCulture, $$"""{"list":[{"name":"Home","type":"jd"}],"rid":{{rid}}}""");
#pragma warning restore JSON002

    [Test]
    public async Task ReconnectAsync_ValidSession_SendsSignedRequest()
    {
        var session = CreateSession();
        using var server = ServerReturning(session, ReconnectBody);
        var channel = CreateChannel(server);

        await channel.ReconnectAsync(session, CancellationToken.None);

        var request = server.Requests.Single();
        var rid = FakeMyJdServer.RidOf(request);
        var expectedSigned = $"/my/reconnect?sessiontoken={SessionToken}&regaintoken={RegainToken}&rid={rid}";
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/my/reconnect"));
        Assert.That(FakeMyJdServer.SignedStringOf(request), Is.EqualTo(expectedSigned));
        Assert.That(FakeMyJdServer.SignatureOf(request), Is.EqualTo(MyJdCrypto.Sign(session.ServerEncryptionToken.ToArray(), expectedSigned)));
    }

    [Test]
    public async Task ReconnectAsync_ValidSession_ReturnsChainedTokens()
    {
        var session = CreateSession();
        using var server = ServerReturning(session, ReconnectBody);
        var channel = CreateChannel(server);

        var renewed = await channel.ReconnectAsync(session, CancellationToken.None);

        Assert.That(renewed.SessionToken, Is.EqualTo(NewSessionToken));
        Assert.That(renewed.RegainToken, Is.EqualTo(NewRegainToken));
        Assert.That(renewed.ServerEncryptionToken.ToArray(), Is.EqualTo(MyJdCrypto.DeriveToken(session.ServerEncryptionToken.ToArray(), NewSessionToken)));
        Assert.That(renewed.DeviceEncryptionToken.ToArray(), Is.EqualTo(MyJdCrypto.DeriveToken(session.DeviceSecret.ToArray(), NewSessionToken)));
        Assert.That(renewed.Email, Is.EqualTo(Email));
        Assert.That(renewed.LoginSecret.ToArray(), Is.EqualTo(session.LoginSecret.ToArray()));
        Assert.That(renewed.DeviceSecret.ToArray(), Is.EqualTo(session.DeviceSecret.ToArray()));
    }

    [Test]
    public async Task ReconnectAsync_Always_LeavesOriginalSessionUnchanged()
    {
        var session = CreateSession();
        var expectedServerToken = session.ServerEncryptionToken.ToArray();
        var expectedDeviceToken = session.DeviceEncryptionToken.ToArray();
        using var server = ServerReturning(session, ReconnectBody);
        var channel = CreateChannel(server);

        await channel.ReconnectAsync(session, CancellationToken.None);

        Assert.That(session.SessionToken, Is.EqualTo(SessionToken));
        Assert.That(session.RegainToken, Is.EqualTo(RegainToken));
        Assert.That(session.ServerEncryptionToken.ToArray(), Is.EqualTo(expectedServerToken));
        Assert.That(session.DeviceEncryptionToken.ToArray(), Is.EqualTo(expectedDeviceToken));
    }

    [Test]
    public async Task DisconnectAsync_ValidSession_SendsSignedRequest()
    {
        var session = CreateSession();
        using var server = ServerReturning(session, RidBody);
        var channel = CreateChannel(server);

        await channel.DisconnectAsync(session, CancellationToken.None);

        var request = server.Requests.Single();
        var rid = FakeMyJdServer.RidOf(request);
        var expectedSigned = $"/my/disconnect?sessiontoken={SessionToken}&rid={rid}";
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/my/disconnect"));
        Assert.That(FakeMyJdServer.SignedStringOf(request), Is.EqualTo(expectedSigned));
        Assert.That(FakeMyJdServer.SignatureOf(request), Is.EqualTo(MyJdCrypto.Sign(session.ServerEncryptionToken.ToArray(), expectedSigned)));
    }

    [Test]
    public async Task DisconnectAsync_RidMismatch_ThrowsMismatch()
    {
        var session = CreateSession();
        using var server = ServerReturning(session, rid => RidBody(rid + 1));
        var channel = CreateChannel(server);

        var ex = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.DisconnectAsync(session, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(MismatchMessage));
    }

    [Test]
    public async Task ListDevicesAsync_TwoDevices_ReturnsDeviceInfos()
    {
        var session = CreateSession();
        using var server = ServerReturning(session, TwoDevicesBody);
        var channel = CreateChannel(server);

        var devices = await channel.ListDevicesAsync(session, CancellationToken.None);

        var request = server.Requests.Single();
        var rid = FakeMyJdServer.RidOf(request);
        var expectedSigned = $"/my/listdevices?sessiontoken={SessionToken}&rid={rid}";
        Assert.That(FakeMyJdServer.SignedStringOf(request), Is.EqualTo(expectedSigned));
        Assert.That(FakeMyJdServer.HasValidSignature(request, session.ServerEncryptionToken.ToArray()), Is.True);
        DeviceInfo[] expected = [new DeviceInfo("d1", "Home", "jd"), new DeviceInfo("d2", "NAS", "jd")];
        Assert.That(devices, Is.EqualTo(expected));
    }

    [Test]
    public async Task ListDevicesAsync_EmptyList_ReturnsEmpty()
    {
        var session = CreateSession();
        using var server = ServerReturning(session, EmptyListBody);
        var channel = CreateChannel(server);

        var devices = await channel.ListDevicesAsync(session, CancellationToken.None);

        Assert.That(devices, Is.Empty);
    }

    [Test]
    public async Task ListDevicesAsync_DeviceMissingId_ThrowsUnreadable()
    {
        var session = CreateSession();
        using var server = ServerReturning(session, MissingIdBody);
        var channel = CreateChannel(server);

        var ex = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ListDevicesAsync(session, CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo(UnreadableMessage));
    }

    [TestCase("reconnect", TestName = "ReconnectAsync_NullSession_ThrowsArgumentNullException")]
    [TestCase("disconnect", TestName = "DisconnectAsync_NullSession_ThrowsArgumentNullException")]
    [TestCase("listdevices", TestName = "ListDevicesAsync_NullSession_ThrowsArgumentNullException")]
    public async Task Methods_NullSession_ThrowArgumentNullException(string method)
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw(string.Empty));
        var channel = CreateChannel(server);

        await Assert.ThrowsAsync<ArgumentNullException>(() => method switch
        {
            "reconnect" => channel.ReconnectAsync(null!, CancellationToken.None),
            "disconnect" => channel.DisconnectAsync(null!, CancellationToken.None),
            _ => channel.ListDevicesAsync(null!, CancellationToken.None),
        });
        Assert.That(server.Requests, Is.Empty);
    }

    [Test]
    public void DeviceInfo_SameValues_AreEqual()
    {
        var first = new DeviceInfo("d1", "Home", "jd");
        var second = new DeviceInfo("d1", "Home", "jd");

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }
}
