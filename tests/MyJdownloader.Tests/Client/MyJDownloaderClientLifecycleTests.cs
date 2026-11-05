namespace MyJdownloader.Tests.Client;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using MyJdownloader.Crypto;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class MyJDownloaderClientLifecycleTests
{
    private const string Email = "user@example.com";
    private const string Password = "password";
    private const string ConnectPath = "/my/connect";
    private const string ReconnectPath = "/my/reconnect";
    private const string DisconnectPath = "/my/disconnect";
    private const string ListDevicesPath = "/my/listdevices";
    private const string ConnectedSessionToken = "5a5a5a5a";
    private const string ConnectedRegainToken = "6b6b6b6b";
    private const string RestoredSessionToken = "0011aabb";
    private const string NewSessionToken = "4455eeff";
    private const string NewRegainToken = "66778899";
    private const string AuthFailedBody = /*lang=json,strict*/ """{"src":"SERVER","type":"AUTH_FAILED"}""";
    private const string NotConnectedMessage = "The client is not connected.";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private delegate Task<HttpResponseMessage> Route(HttpRequestMessage request, CancellationToken cancellationToken);

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static byte[] LoginSecret() => MyJdCrypto.DeriveLoginSecret(Email, Password);

    /// <summary>The server encryption token of a session obtained through <c>ConnectAsync</c>.</summary>
    private static byte[] ConnectedServerKey() => MyJdCrypto.DeriveToken(LoginSecret(), ConnectedSessionToken);

    private static MyJdSession CreateSession() =>
        new(Email, Key(1), Key(2), RestoredSessionToken, "ccdd2233", Key(3), Key(4));

    private static string Body(string fields, long rid) =>
        string.Create(CultureInfo.InvariantCulture, $"{{{fields},\"rid\":{rid}}}");

    private static string Tokens(string sessionToken, string regainToken) =>
        $"\"sessiontoken\":\"{sessionToken}\",\"regaintoken\":\"{regainToken}\"";

    private static Task<HttpResponseMessage> Reply(HttpResponseMessage response) => Task.FromResult(response);

    private static Route ConnectOk() =>
        (request, _) => Reply(FakeMyJdServer.EncryptedJson(
            LoginSecret(),
            Body(Tokens(ConnectedSessionToken, ConnectedRegainToken), FakeMyJdServer.RidOf(request))));

    private static Route ReconnectOk(byte[] key) =>
        (request, _) => Reply(FakeMyJdServer.EncryptedJson(
            key,
            Body(Tokens(NewSessionToken, NewRegainToken), FakeMyJdServer.RidOf(request))));

    private static Route DisconnectOk(byte[] key) =>
        (request, _) => Reply(FakeMyJdServer.EncryptedJson(key, Body("\"ok\":true", FakeMyJdServer.RidOf(request))));

    private static Route DevicesOk(byte[] key) =>
        (request, _) => Reply(FakeMyJdServer.EncryptedJson(
            key,
            Body(
                "\"list\":[{\"id\":\"d1\",\"name\":\"Home\",\"type\":\"jd\"},{\"id\":\"d2\",\"name\":\"NAS\",\"type\":\"jd\"}]",
                FakeMyJdServer.RidOf(request))));

    /// <summary>Wraps a route so that it signals when reached and then waits for the gate or cancellation.</summary>
    private static Route Gated(Route inner, TaskCompletionSource entered, TaskCompletionSource gate) =>
        async (request, cancellationToken) =>
        {
            entered.TrySetResult();
            await gate.Task.WaitAsync(cancellationToken);
            return await inner(request, cancellationToken);
        };

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static MyJDownloaderClient CreateClient(ScriptedServer server) => new(new HttpClient(server));

    private static ConcurrentQueue<MyJdSessionChangedEventArgs> Collect(MyJDownloaderClient client)
    {
        var events = new ConcurrentQueue<MyJdSessionChangedEventArgs>();
        client.SessionChanged += (_, args) => events.Enqueue(args);
        return events;
    }

    [Test]
    public async Task ConnectAsync_ValidCredentials_IsConnectedAndExportsSession()
    {
        using var server = new ScriptedServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);

        await client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(client.IsConnected, Is.True);
            Assert.That(client.ExportSession()!.SessionToken, Is.EqualTo(ConnectedSessionToken));
            Assert.That(client.ExportSession()!.Email, Is.EqualTo(Email));
        });
    }

    [Test]
    public async Task ConnectAsync_ValidCredentials_RaisesSessionChangedOnce()
    {
        using var server = new ScriptedServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);
        var events = Collect(client);

        await client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events.Single().Session, Is.SameAs(client.ExportSession()));
        });
    }

    [Test]
    public async Task ConnectAsync_ServerRejects_StaysDisconnectedWithoutEvent()
    {
        using var server = new ScriptedServer().On(
            ConnectPath,
            (_, _) => Reply(FakeMyJdServer.Raw(AuthFailedBody, HttpStatusCode.Forbidden)));
        var client = CreateClient(server);
        var events = Collect(client);

        await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(client.IsConnected, Is.False);
            Assert.That(events, Is.Empty);
        });
    }

    [Test]
    public void ConnectAsync_InvalidEmail_ThrowsSynchronouslyWithoutRequest()
    {
        using var server = new ScriptedServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);

        Assert.Catch<ArgumentException>(() => _ = client.ConnectAsync(string.Empty, Password));

        Assert.That(server.Calls, Is.Empty);
    }

    [Test]
    public async Task ConnectAsync_AlreadyConnected_ReplacesSession()
    {
        using var server = new ScriptedServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);
        var restored = CreateSession();
        client.RestoreSession(restored);

        await client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(client.ExportSession(), Is.Not.SameAs(restored));
            Assert.That(client.ExportSession()!.SessionToken, Is.EqualTo(ConnectedSessionToken));
        });
    }

    [Test]
    public async Task ConnectAsync_HandlerCallsDisconnect_DoesNotDeadlock()
    {
        using var server = new ScriptedServer()
            .On(ConnectPath, ConnectOk())
            .On(DisconnectPath, DisconnectOk(ConnectedServerKey()));
        var client = CreateClient(server);
        var completed = false;
        client.SessionChanged += (_, args) =>
        {
            if (args.Session is not null)
            {
                completed = client.DisconnectAsync().Wait(WaitTimeout);
            }
        };

        await client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True);
            Assert.That(client.IsConnected, Is.False);
        });
    }

    [Test]
    public async Task ReconnectAsync_Connected_ExportsNewSessionAndRaisesEvent()
    {
        using var server = new ScriptedServer().On(ReconnectPath, ReconnectOk(Key(3)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        await client.ReconnectAsync().WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(client.ExportSession()!.SessionToken, Is.EqualTo(NewSessionToken));
            Assert.That(client.ExportSession()!.RegainToken, Is.EqualTo(NewRegainToken));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events.Single().Session, Is.SameAs(client.ExportSession()));
        });
    }

    [Test]
    public async Task ReconnectAsync_NotConnected_ThrowsInvalidOperationException()
    {
        using var server = new ScriptedServer().On(ReconnectPath, ReconnectOk(Key(3)));
        var client = CreateClient(server);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ReconnectAsync().WaitAsync(WaitTimeout));

        Assert.That(ex!.Message, Is.EqualTo(NotConnectedMessage));
        Assert.That(server.Calls, Is.Empty);
    }

    [Test]
    public async Task ReconnectAsync_CancelledWhileWaiting_LeavesStateUnchanged()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        using var server = new ScriptedServer()
            .On(ConnectPath, Gated(ConnectOk(), entered, gate))
            .On(ReconnectPath, ReconnectOk(Key(3)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var connect = client.ConnectAsync(Email, Password);
        await entered.Task.WaitAsync(WaitTimeout);
        using var cts = new CancellationTokenSource();
        var reconnect = client.ReconnectAsync(cts.Token);
        await cts.CancelAsync();

        await Assert.CatchAsync<OperationCanceledException>(() => reconnect.WaitAsync(WaitTimeout));
        gate.SetResult();
        await connect.WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(client.ExportSession()!.SessionToken, Is.EqualTo(ConnectedSessionToken));
            Assert.That(server.Calls.Any(call => call.StartsWith(ReconnectPath, StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public async Task DisconnectAsync_Connected_ClearsSessionRaisesNullAndCallsServer()
    {
        using var server = new ScriptedServer().On(DisconnectPath, DisconnectOk(Key(3)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        await client.DisconnectAsync().WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(client.IsConnected, Is.False);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events.Single().Session, Is.Null);
            Assert.That(server.Calls.Single(), Does.StartWith($"{DisconnectPath}?sessiontoken={RestoredSessionToken}&"));
        });
    }

    [Test]
    public async Task DisconnectAsync_ServerFails_StillDisconnectedAndThrows()
    {
        using var server = new ScriptedServer().On(
            DisconnectPath,
            (_, _) => Reply(FakeMyJdServer.Raw("<html><body>Internal Server Error</body></html>", HttpStatusCode.InternalServerError)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        await Assert.CatchAsync<MyJdApiException>(() => client.DisconnectAsync().WaitAsync(WaitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(client.IsConnected, Is.False);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events.Single().Session, Is.Null);
        });
    }

    [Test]
    public async Task DisconnectAsync_NotConnected_NoRequestNoEvent()
    {
        using var server = new ScriptedServer().On(DisconnectPath, DisconnectOk(Key(3)));
        var client = CreateClient(server);
        var events = Collect(client);

        await client.DisconnectAsync().WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(server.Calls, Is.Empty);
            Assert.That(events, Is.Empty);
        });
    }

    [Test]
    public async Task DisconnectAsync_DuringConnect_EndsDisconnected()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        using var server = new ScriptedServer()
            .On(ConnectPath, Gated(ConnectOk(), entered, gate))
            .On(DisconnectPath, DisconnectOk(ConnectedServerKey()));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var connect = client.ConnectAsync(Email, Password);
        await entered.Task.WaitAsync(WaitTimeout);

        var disconnect = client.DisconnectAsync();
        gate.SetResult();
        await Task.WhenAll(connect, disconnect).WaitAsync(WaitTimeout);

        Assert.That(client.IsConnected, Is.False);
    }

    [Test]
    public async Task DisconnectAsync_HandlerThrows_ServerStillCalled()
    {
        using var server = new ScriptedServer().On(DisconnectPath, DisconnectOk(Key(3)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        client.SessionChanged += (_, _) => throw new InvalidOperationException("handler failure");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.DisconnectAsync().WaitAsync(WaitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("handler failure"));
            Assert.That(server.Calls.Single(), Does.StartWith(DisconnectPath));
            Assert.That(client.IsConnected, Is.False);
        });
    }

    [Test]
    public async Task SessionChanged_StaleDisconnectCompletesAfterConnect_IsNotRaised()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        using var server = new ScriptedServer()
            .On(ConnectPath, ConnectOk())
            .On(DisconnectPath, Gated(DisconnectOk(Key(3)), entered, gate));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);
        var disconnect = client.DisconnectAsync();
        await entered.Task.WaitAsync(WaitTimeout);

        await client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout);
        gate.SetResult();
        await disconnect.WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events.Single().Session, Is.SameAs(client.ExportSession()));
            Assert.That(client.IsConnected, Is.True);
        });
    }

    [Test]
    public async Task GetDevicesAsync_Connected_ReturnsDevices()
    {
        using var server = new ScriptedServer().On(ListDevicesPath, DevicesOk(Key(3)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());

        var devices = await client.GetDevicesAsync().WaitAsync(WaitTimeout);

        DeviceInfo[] expected = [new DeviceInfo { Id = "d1", Name = "Home", Type = "jd" }, new DeviceInfo { Id = "d2", Name = "NAS", Type = "jd" }];
        Assert.That(devices, Is.EqualTo(expected));
    }

    [Test]
    public async Task GetDevicesAsync_NotConnected_ThrowsInvalidOperationException()
    {
        using var server = new ScriptedServer().On(ListDevicesPath, DevicesOk(Key(3)));
        var client = CreateClient(server);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetDevicesAsync().WaitAsync(WaitTimeout));

        Assert.That(ex!.Message, Is.EqualTo(NotConnectedMessage));
        Assert.That(server.Calls, Is.Empty);
    }

    [Test]
    public async Task GetDevicesAsync_AfterRestore_UsesRestoredSessionToken()
    {
        using var server = new ScriptedServer().On(ListDevicesPath, DevicesOk(Key(3)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());

        await client.GetDevicesAsync().WaitAsync(WaitTimeout);

        Assert.That(server.Calls.Single(), Does.StartWith($"{ListDevicesPath}?sessiontoken={RestoredSessionToken}&"));
    }

    [Test]
    public async Task SessionChanged_HandlerThrows_ExceptionPropagatesAndStateUpdated()
    {
        using var server = new ScriptedServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);
        client.SessionChanged += (_, _) => throw new InvalidOperationException("handler failure");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("handler failure"));
            Assert.That(client.IsConnected, Is.True);
            Assert.That(client.ExportSession()!.SessionToken, Is.EqualTo(ConnectedSessionToken));
        });
    }

    [Test]
    public async Task SessionChanged_FirstHandlerThrows_SecondHandlerStillInvoked()
    {
        using var server = new ScriptedServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);
        client.SessionChanged += (_, _) => throw new InvalidOperationException("handler failure");
        var events = Collect(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout));

        Assert.That(events, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SessionChanged_TwoHandlersThrow_ThrowsAggregateException()
    {
        using var server = new ScriptedServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);
        client.SessionChanged += (_, _) => throw new InvalidOperationException("first");
        client.SessionChanged += (_, _) => throw new ArgumentException("second");

        var ex = await Assert.ThrowsAsync<AggregateException>(() => client.ConnectAsync(Email, Password).WaitAsync(WaitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(ex.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.InnerExceptions[1], Is.TypeOf<ArgumentException>());
            Assert.That(client.IsConnected, Is.True);
        });
    }

    [Test]
    public async Task ReconnectAsync_TwoConcurrentCalls_AreSerialized()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        var inFlight = 0;
        var maxInFlight = 0;
        var secondKey = MyJdCrypto.DeriveToken(Key(3), NewSessionToken);
        Route Tracked(Route inner) => async (request, cancellationToken) =>
        {
            var now = Interlocked.Increment(ref inFlight);
            InterlockedMax(ref maxInFlight, now);
            try
            {
                return await inner(request, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        };

        var count = 0;
        Task<HttpResponseMessage> Reconnect(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Interlocked.Increment(ref count) == 1
                ? Tracked(Gated(ReconnectOk(Key(3)), entered, gate))(request, cancellationToken)
                : Tracked(ReconnectOk(secondKey))(request, cancellationToken);
        using var server = new ScriptedServer().On(ReconnectPath, Reconnect);
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var first = client.ReconnectAsync();
        await entered.Task.WaitAsync(WaitTimeout);
        var second = client.ReconnectAsync();

        Assert.That(second.IsCompleted, Is.False);
        gate.SetResult();
        await Task.WhenAll(first, second).WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(maxInFlight, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ConnectAsync_CancelledWhileWaitingForGate_SendsNoConnectAndLeavesStateUnchanged()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        using var server = new ScriptedServer()
            .On(ReconnectPath, Gated(ReconnectOk(Key(3)), entered, gate))
            .On(ConnectPath, ConnectOk());
        var client = CreateClient(server);
        var restored = CreateSession();
        client.RestoreSession(restored);
        var reconnect = client.ReconnectAsync();
        await entered.Task.WaitAsync(WaitTimeout);
        using var cts = new CancellationTokenSource();
        var connect = client.ConnectAsync(Email, Password, cts.Token);
        await cts.CancelAsync();

        await Assert.CatchAsync<OperationCanceledException>(() => connect.WaitAsync(WaitTimeout));
        gate.SetResult();
        await reconnect.WaitAsync(WaitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(server.Calls.Any(call => call.StartsWith(ConnectPath, StringComparison.Ordinal)), Is.False);
            Assert.That(client.ExportSession()!.SessionToken, Is.EqualTo(NewSessionToken));
        });
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        do
        {
            current = Volatile.Read(ref target);
            if (value <= current)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref target, value, current) != current);
    }

    /// <summary>Routes requests by path to async responders and records the path and query of each call.</summary>
    private sealed class ScriptedServer : HttpMessageHandler
    {
        private readonly Dictionary<string, Route> routes = [];
        private readonly ConcurrentQueue<string> calls = new();

        public IReadOnlyCollection<string> Calls => this.calls;

        public ScriptedServer On(string path, Route route)
        {
            this.routes[path] = route;
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.calls.Enqueue(request.RequestUri!.PathAndQuery);
            return await this.routes[request.RequestUri.AbsolutePath](request, cancellationToken);
        }
    }
}
