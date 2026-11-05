namespace MyJdownloader.Tests.Transport;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MyJdownloader.Crypto;
using MyJdownloader.Transport;

[TestFixture]
public sealed class ServerChannelConnectTests
{
    private const string Email = "user@example.com";
    private const string Password = "password";
    private const string SessionToken = "0011aabb";
    private const string RegainToken = "ccdd2233";
    private const string MismatchMessage = "The MyJD server response does not match the request.";
    private const string UnreadableMessage = "The MyJD server returned a response that could not be read.";

    private static string ValidBody(long rid) =>
        string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"{{SessionToken}}","regaintoken":"{{RegainToken}}","rid":{{rid}}}""");

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static ServerChannel CreateChannel(FakeMyJdServer server, MyJDownloaderOptions? options = null)
    {
        var clock = new FakeTimeProvider { UtcNow = new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero) };
        var httpClient = new HttpClient(server);
        return new ServerChannel(httpClient, options ?? new MyJDownloaderOptions(), new RequestIdGenerator(clock), clock);
    }

    private static FakeMyJdServer ServerReturningValid(string email = Email) =>
        new(request => FakeMyJdServer.EncryptedJson(MyJdCrypto.DeriveLoginSecret(email, Password), ValidBody(FakeMyJdServer.RidOf(request))));

    private static FakeMyJdServer ServerReturning(Func<long, string> body, string email = Email) =>
        new(request => FakeMyJdServer.EncryptedJson(MyJdCrypto.DeriveLoginSecret(email, Password), body(FakeMyJdServer.RidOf(request))));

    private static Task<MyJdSession> ConnectWith(ServerChannel channel) =>
        channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None);

    [Test]
    public async Task ConnectAsync_ValidCredentials_SendsSignedConnectRequest()
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);

        await channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None);

        var request = server.Requests.Single();
        var rid = FakeMyJdServer.RidOf(request);
        var expectedSigned = $"/my/connect?email=user%40example.com&appkey=MyJdownloader.NET&rid={rid}";
        var loginSecret = MyJdCrypto.DeriveLoginSecret(Email, Password);
        Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/my/connect"));
        Assert.That(FakeMyJdServer.SignedStringOf(request), Is.EqualTo(expectedSigned));
        Assert.That(FakeMyJdServer.SignatureOf(request), Is.EqualTo(MyJdCrypto.Sign(loginSecret, expectedSigned)));
        Assert.That(request.RequestUri.PathAndQuery, Is.EqualTo($"{expectedSigned}&signature={MyJdCrypto.Sign(loginSecret, expectedSigned)}"));
    }

    [Test]
    public async Task ConnectAsync_ValidCredentials_ReturnsDerivedSession()
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);
        var loginSecret = MyJdCrypto.DeriveLoginSecret(Email, Password);
        var deviceSecret = MyJdCrypto.DeriveDeviceSecret(Email, Password);

        var session = await channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None);

        Assert.That(session.Email, Is.EqualTo(Email));
        Assert.That(session.SessionToken, Is.EqualTo(SessionToken));
        Assert.That(session.RegainToken, Is.EqualTo(RegainToken));
        Assert.That(session.LoginSecret.ToArray(), Is.EqualTo(loginSecret));
        Assert.That(session.DeviceSecret.ToArray(), Is.EqualTo(deviceSecret));
        Assert.That(session.ServerEncryptionToken.ToArray(), Is.EqualTo(MyJdCrypto.DeriveToken(loginSecret, SessionToken)));
        Assert.That(session.DeviceEncryptionToken.ToArray(), Is.EqualTo(MyJdCrypto.DeriveToken(deviceSecret, SessionToken)));
    }

    [Test]
    public async Task ConnectAsync_PlusInEmail_IsPercentEncoded()
    {
        const string email = "user+tag@example.com";
        using var server = ServerReturningValid(email);
        var channel = CreateChannel(server);

        await channel.ConnectAsync(channel.CreateConnectRequest(email, Password), CancellationToken.None);

        var request = server.Requests.Single();
        Assert.That(request.RequestUri!.Query, Does.Contain("email=user%2Btag%40example.com&"));
        Assert.That(FakeMyJdServer.HasValidSignature(request, MyJdCrypto.DeriveLoginSecret(email, Password)), Is.True);
    }

    [Test]
    public async Task ConnectAsync_AppKeyWithReservedChars_IsEncodedAndSigned()
    {
        using var server = ServerReturningValid();
        var options = new MyJDownloaderOptions { AppKey = "My App&Co" };
        var channel = CreateChannel(server, options);

        await channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None);

        var request = server.Requests.Single();
        Assert.That(request.RequestUri!.Query, Does.Contain("appkey=My%20App%26Co&"));
        Assert.That(FakeMyJdServer.HasValidSignature(request, MyJdCrypto.DeriveLoginSecret(Email, Password)), Is.True);
    }

    [Test]
    public async Task ConnectAsync_BaseAddressWithPath_KeepsBasePath()
    {
        using var server = ServerReturningValid();
        var options = new MyJDownloaderOptions { BaseAddress = new Uri("https://relay.test/api/") };
        var channel = CreateChannel(server, options);

        await channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None);

        var request = server.Requests.Single();
        Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/api/my/connect"));
        Assert.That(FakeMyJdServer.SignedStringOf(request, "/api"), Does.StartWith("/my/connect?"));
        Assert.That(FakeMyJdServer.HasValidSignature(request, MyJdCrypto.DeriveLoginSecret(Email, Password), "/api"), Is.True);
    }

    [TestCase("")]
    [TestCase("  ")]
    [TestCase("user\n@example.com")]
    public void CreateConnectRequest_InvalidEmail_ThrowsWithoutRequest(string email)
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);

        Assert.Catch<ArgumentException>(() => channel.CreateConnectRequest(email, Password));
        Assert.That(server.Requests, Is.Empty);
    }

    [Test]
    public void CreateConnectRequest_EmailOver254Chars_ThrowsWithoutRequest()
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);
        var email = new string('a', 243) + "@example.com";

        Assert.That(email, Has.Length.EqualTo(255));
        Assert.Catch<ArgumentException>(() => channel.CreateConnectRequest(email, Password));
        Assert.That(server.Requests, Is.Empty);
    }

    [Test]
    public void CreateConnectRequest_EmptyPassword_ThrowsWithoutRequest()
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);

        Assert.Catch<ArgumentException>(() => channel.CreateConnectRequest(Email, string.Empty));
        Assert.That(server.Requests, Is.Empty);
    }

    [Test]
    public async Task ConnectAsync_RidMismatch_ThrowsMismatch()
    {
        using var server = ServerReturning(rid => ValidBody(rid + 1));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(MismatchMessage));
        Assert.That(exception.ErrorSource, Is.EqualTo(MyJdErrorSource.Server));
    }

    [Test]
    public async Task ConnectAsync_RidMissing_ThrowsMismatch()
    {
        using var server = ServerReturning(_ => $$"""{"sessiontoken":"{{SessionToken}}","regaintoken":"{{RegainToken}}"}""");
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(MismatchMessage));
    }

    [Test]
    public async Task ConnectAsync_RidOnlyNested_ThrowsMismatch()
    {
        using var server = ServerReturning(rid =>
            string.Create(CultureInfo.InvariantCulture, $$"""{"x":{"rid":{{rid}}},"sessiontoken":"{{SessionToken}}","regaintoken":"{{RegainToken}}"}"""));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(MismatchMessage));
    }

    [Test]
    public async Task ConnectAsync_RidAsString_ThrowsMismatch()
    {
        using var server = ServerReturning(rid =>
            string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"{{SessionToken}}","regaintoken":"{{RegainToken}}","rid":"{{rid}}"}"""));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(MismatchMessage));
    }

    [TestCase("invalidBase64", TestName = "ConnectAsync_UnreadableResponse_InvalidBase64_ThrowsUnreadable")]
    [TestCase("randomBlock", TestName = "ConnectAsync_UnreadableResponse_BadPadding_ThrowsUnreadable")]
    [TestCase("notJson", TestName = "ConnectAsync_UnreadableResponse_NotJson_ThrowsUnreadable")]
    [TestCase("badSessionToken", TestName = "ConnectAsync_UnreadableResponse_RejectedSessionToken_ThrowsUnreadable")]
    [TestCase("missingRegainToken", TestName = "ConnectAsync_UnreadableResponse_MissingRegainToken_ThrowsUnreadable")]
    public async Task ConnectAsync_UnreadableResponse_ThrowsUnreadable(string scenario)
    {
        var loginSecret = MyJdCrypto.DeriveLoginSecret(Email, Password);
        using var server = new FakeMyJdServer(request =>
        {
            var rid = FakeMyJdServer.RidOf(request);
            return scenario switch
            {
                "invalidBase64" => FakeMyJdServer.Raw("***not base64***"),
                "randomBlock" => FakeMyJdServer.Raw(Convert.ToBase64String(new byte[16])),
                "notJson" => FakeMyJdServer.EncryptedJson(loginSecret, "not json"),
                "badSessionToken" => FakeMyJdServer.EncryptedJson(
                    loginSecret,
                    string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"zz","regaintoken":"{{RegainToken}}","rid":{{rid}}}""")),
                "missingRegainToken" => FakeMyJdServer.EncryptedJson(
                    loginSecret,
                    string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"{{SessionToken}}","rid":{{rid}}}""")),
                _ => throw new InvalidOperationException(scenario),
            };
        });
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(UnreadableMessage));
        Assert.That(exception.InnerException, Is.Null);
        Assert.That(exception.ErrorSource, Is.EqualTo(MyJdErrorSource.Server));
    }

    [Test]
    public async Task ConnectAsync_EmptyBody_ThrowsUnreadable()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw(string.Empty));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(UnreadableMessage));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public async Task ConnectAsync_BodyOver1MiB_ThrowsUnreadable()
    {
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw(new string('A', 1_048_577)));
        var channel = CreateChannel(server);

        var exception = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(UnreadableMessage));
        Assert.That(exception.ErrorSource, Is.EqualTo(MyJdErrorSource.Server));
    }

    [Test]
    public async Task ConnectAsync_AnyFailure_MessageContainsNoEmailOrToken()
    {
        using var mismatch = ServerReturning(rid => ValidBody(rid + 1));
        using var unreadable = ServerReturning(_ => "not json");

        var mismatchException = await Assert.ThrowsAsync<MyJdProtocolException>(() => ConnectWith(CreateChannel(mismatch)));
        var unreadableException = await Assert.ThrowsAsync<MyJdProtocolException>(() => ConnectWith(CreateChannel(unreadable)));

        foreach (var exception in new[] { mismatchException!, unreadableException! })
        {
            Assert.That(exception.Message, Does.Not.Contain(Email));
            Assert.That(exception.Message, Does.Not.Contain(SessionToken));
            Assert.That(exception.Message, Does.Not.Contain(RegainToken));
        }
    }

    [Test]
    public async Task ConnectAsync_RequestReused_ThrowsInvalidOperationException()
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);
        var request = channel.CreateConnectRequest(Email, Password);

        await channel.ConnectAsync(request, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => channel.ConnectAsync(request, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("The connect request has already been used."));
        Assert.That(server.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ConnectAsync_DisposedRequest_ThrowsInvalidOperationException()
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);
        var request = channel.CreateConnectRequest(Email, Password);

        request.Dispose();
        request.Dispose();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => channel.ConnectAsync(request, CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("The connect request has already been used."));
        Assert.That(server.Requests, Is.Empty);
    }

    [Test]
    public async Task ConnectAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var server = ServerReturningValid();
        var channel = CreateChannel(server);

        await Assert.ThrowsAsync<ArgumentNullException>(() => channel.ConnectAsync(null!, CancellationToken.None));
        Assert.That(server.Requests, Is.Empty);
    }

    [Test]
    public async Task ConnectAsync_ExtraResponseField_IsIgnored()
    {
        using var server = ServerReturning(rid =>
            string.Create(CultureInfo.InvariantCulture, $$"""{"sessiontoken":"{{SessionToken}}","regaintoken":"{{RegainToken}}","rid":{{rid}},"future":1}"""));
        var channel = CreateChannel(server);

        var session = await channel.ConnectAsync(channel.CreateConnectRequest(Email, Password), CancellationToken.None);

        Assert.That(session.SessionToken, Is.EqualTo(SessionToken));
        Assert.That(session.RegainToken, Is.EqualTo(RegainToken));
    }
}
