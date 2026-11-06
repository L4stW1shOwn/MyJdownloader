namespace MyJdownloader.Tests.Client;

using System.Net;
using MyJdownloader.Crypto;
using MyJdownloader.Tests.Transport;

public sealed partial class MyJDownloaderClientLifecycleTests
{
    private const string TokenInvalidType = "TOKEN_INVALID";
    private const string TokenInvalidBody = /*lang=json,strict*/ """{"src":"SERVER","type":"TOKEN_INVALID"}""";
    private const string DeviceTokenInvalidBody = /*lang=json,strict*/ """{"src":"DEVICE","type":"TOKEN_INVALID"}""";
    private const string OtherEmail = "other@example.com";
    private const string OtherSessionToken = "aa55aa55";

    private static Route TokenInvalid() =>
        (_, _) => Reply(FakeMyJdServer.Raw(TokenInvalidBody, HttpStatusCode.Forbidden));

    /// <summary>Uses the n-th route for the n-th request; the last route repeats.</summary>
    private static Route Sequence(params Route[] routes)
    {
        var count = 0;
        return (request, cancellationToken) =>
        {
            var index = Math.Min(Interlocked.Increment(ref count), routes.Length) - 1;
            return routes[index](request, cancellationToken);
        };
    }

    /// <summary>The server token after renewing <see cref="CreateSession"/> (server key <c>Key(3)</c>).</summary>
    private static byte[] RenewedServerKey() => MyJdCrypto.DeriveToken(Key(3), NewSessionToken);

    /// <summary>Like <see cref="Gated"/> but the wait does not observe the request cancellation token.</summary>
    private static Route GatedIgnoringCancellation(Route inner, TaskCompletionSource entered, TaskCompletionSource gate) =>
        async (request, cancellationToken) =>
        {
            entered.TrySetResult();
            await gate.Task;
            return await inner(request, cancellationToken);
        };

    private static int CountCalls(ScriptedServer server, string path) =>
        server.Calls.Count(call => call.StartsWith(path, StringComparison.Ordinal));

    private static string LastCall(ScriptedServer server, string path) =>
        server.Calls.Last(call => call.StartsWith(path, StringComparison.Ordinal));

    [Test]
    public async Task GetDevicesAsync_TokenInvalidThenRenewed_ReturnsDevices()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, Sequence(TokenInvalid(), DevicesOk(RenewedServerKey())));
        var client = CreateClient(server);
        var restored = CreateSession();
        client.RestoreSession(restored);

        var devices = await client.GetDevicesAsync().WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(devices, Has.Count.EqualTo(2));
            Assert.That(CountCalls(server, ReconnectPath), Is.EqualTo(1));
            Assert.That(LastCall(server, ListDevicesPath), Does.Contain($"sessiontoken={NewSessionToken}"));
            Assert.That(client.ExportSession(), Is.Not.SameAs(restored));
        });
    }

    [Test]
    public async Task GetDevicesAsync_TokenInvalidThenRenewed_RaisesSessionChangedOnce()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, Sequence(TokenInvalid(), DevicesOk(RenewedServerKey())));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        await client.GetDevicesAsync().WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events.Single().Session, Is.SameAs(client.ExportSession()));
        });
    }

    [Test]
    public async Task GetDevicesAsync_TokenInvalidOnReplay_ThrowsWithoutSecondRenewal()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, TokenInvalid());
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());

        var ex = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ErrorType, Is.EqualTo(TokenInvalidType));
            Assert.That(CountCalls(server, ReconnectPath), Is.EqualTo(1));
            Assert.That(CountCalls(server, ListDevicesPath), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetDevicesAsync_AuthFailed_DoesNotReconnect()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, (_, _) => Reply(FakeMyJdServer.Raw(AuthFailedBody, HttpStatusCode.Forbidden)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());

        var ex = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ErrorType, Is.EqualTo("AUTH_FAILED"));
            Assert.That(CountCalls(server, ReconnectPath), Is.Zero);
        });
    }

    [Test]
    public async Task GetDevicesAsync_NetworkError_DoesNotReconnect()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, (_, _) => throw new HttpRequestException("network down"));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.That(CountCalls(server, ReconnectPath), Is.Zero);
    }

    [Test]
    public async Task GetDevicesAsync_DeviceSourcedTokenInvalid_DoesNotReconnect()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, (_, _) => Reply(FakeMyJdServer.Raw(DeviceTokenInvalidBody, HttpStatusCode.Forbidden)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());

        var ex = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ErrorType, Is.EqualTo(TokenInvalidType));
            Assert.That(CountCalls(server, ReconnectPath), Is.Zero);
        });
    }

    // Both orderings (the second caller waiting on the gate, or arriving after the renewal completed)
    // take the same reference-comparison branch under the gate, so the outcome does not depend on timing.
    [Test]
    public async Task GetDevicesAsync_TwoConcurrentTokenInvalid_ReconnectsOnce()
    {
        var bothArrived = NewSignal();
        var reconnectEntered = NewSignal();
        var reconnectGate = NewSignal();
        var count = 0;
        Task<HttpResponseMessage> ListDevices(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref count);
            if (n > 2)
            {
                return DevicesOk(RenewedServerKey())(request, cancellationToken);
            }

            if (n == 2)
            {
                bothArrived.TrySetResult();
            }

            return WaitThenTokenInvalid(request, cancellationToken);
        }

        async Task<HttpResponseMessage> WaitThenTokenInvalid(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await bothArrived.Task.WaitAsync(cancellationToken);
            return await TokenInvalid()(request, cancellationToken);
        }

        using var server = new ScriptedServer()
            .On(ReconnectPath, Gated(ReconnectOk(Key(3)), reconnectEntered, reconnectGate))
            .On(ListDevicesPath, ListDevices);
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);
        var first = client.GetDevicesAsync();
        var second = client.GetDevicesAsync();
        await bothArrived.Task.WaitAsync(_waitTimeout);
        await reconnectEntered.Task.WaitAsync(_waitTimeout);

        reconnectGate.SetResult();
        var results = await Task.WhenAll(first, second).WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(results[0], Has.Count.EqualTo(2));
            Assert.That(results[1], Has.Count.EqualTo(2));
            Assert.That(CountCalls(server, ReconnectPath), Is.EqualTo(1));
            Assert.That(events, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task GetDevicesAsync_SessionRestoredAfterFailure_ReplaysWithoutReconnect()
    {
        var client = default(MyJDownloaderClient);
        var other = new MyJdSession(Email, Key(5), Key(6), OtherSessionToken, "bb66bb66", Key(7), Key(8));
        Task<HttpResponseMessage> RestoreThenTokenInvalid(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            client!.RestoreSession(other);
            return TokenInvalid()(request, cancellationToken);
        }

        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, Sequence(RestoreThenTokenInvalid, DevicesOk(Key(7))));
        client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        var devices = await client.GetDevicesAsync().WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(devices, Has.Count.EqualTo(2));
            Assert.That(CountCalls(server, ReconnectPath), Is.Zero);
            Assert.That(LastCall(server, ListDevicesPath), Does.Contain($"sessiontoken={OtherSessionToken}"));
            Assert.That(events, Is.Empty);
        });
    }

    [Test]
    public async Task GetDevicesAsync_SessionRestoredForOtherAccount_ThrowsOriginalTokenInvalid()
    {
        var client = default(MyJDownloaderClient);
        var other = new MyJdSession(OtherEmail, Key(5), Key(6), OtherSessionToken, "bb66bb66", Key(7), Key(8));
        Task<HttpResponseMessage> RestoreThenTokenInvalid(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            client!.RestoreSession(other);
            return TokenInvalid()(request, cancellationToken);
        }

        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, Sequence(RestoreThenTokenInvalid, DevicesOk(Key(7))));
        client = CreateClient(server);
        client.RestoreSession(CreateSession());

        var ex = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ErrorType, Is.EqualTo(TokenInvalidType));
            Assert.That(CountCalls(server, ReconnectPath), Is.Zero);
            Assert.That(CountCalls(server, ListDevicesPath), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetDevicesAsync_DisconnectedAfterFailure_ThrowsNotConnected()
    {
        var client = default(MyJDownloaderClient);
        async Task<HttpResponseMessage> DisconnectThenTokenInvalid(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await client!.DisconnectAsync(cancellationToken);
            return await TokenInvalid()(request, cancellationToken);
        }

        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(DisconnectPath, DisconnectOk(Key(3)))
            .On(ListDevicesPath, DisconnectThenTokenInvalid);
        client = CreateClient(server);
        client.RestoreSession(CreateSession());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo(NotConnectedMessage));
            Assert.That(CountCalls(server, ReconnectPath), Is.Zero);
        });
    }

    [Test]
    public async Task GetDevicesAsync_HandlerThrowsDuringRenewal_ReplaysThenRethrows()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, Sequence(TokenInvalid(), DevicesOk(RenewedServerKey())));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        static void Handler(object? sender, MyJdSessionChangedEventArgs args) => throw new InvalidOperationException("handler");
        client.SessionChanged += Handler;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));
        client.SessionChanged -= Handler;
        var callsAfterFailure = CountCalls(server, ListDevicesPath);
        await client.GetDevicesAsync().WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("handler"));
            Assert.That(callsAfterFailure, Is.EqualTo(2));
            Assert.That(LastCall(server, ListDevicesPath), Does.Contain($"sessiontoken={NewSessionToken}"));
        });
    }

    [Test]
    public async Task GetDevicesAsync_HandlerThrowsAndReplayFails_ThrowsReplayException()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, TokenInvalid());
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        client.SessionChanged += (_, _) => throw new InvalidOperationException("handler");

        var ex = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.That(ex!.ErrorType, Is.EqualTo(TokenInvalidType));
    }

    [Test]
    public async Task GetDevicesAsync_CancelledDuringReplay_KeepsRenewedSession()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, Sequence(TokenInvalid(), Gated(DevicesOk(RenewedServerKey()), entered, gate)));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);
        using var cts = new CancellationTokenSource();
        var call = client.GetDevicesAsync(cts.Token);
        await entered.Task.WaitAsync(_waitTimeout);

        await cts.CancelAsync();
        await Assert.CatchAsync<OperationCanceledException>(() => call.WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(client.IsConnected, Is.True);
            Assert.That(client.ExportSession()!.SessionToken, Is.EqualTo(NewSessionToken));
        });
    }

    [Test]
    public async Task GetDevicesAsync_CancelledDuringRenewal_StoresRenewedSession()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        using var server = new ScriptedServer()
            .On(ReconnectPath, GatedIgnoringCancellation(ReconnectOk(Key(3)), entered, gate))
            .On(ListDevicesPath, Sequence(TokenInvalid(), DevicesOk(RenewedServerKey())));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);
        using var cts = new CancellationTokenSource();
        var call = client.GetDevicesAsync(cts.Token);
        await entered.Task.WaitAsync(_waitTimeout);

        await cts.CancelAsync();
        gate.SetResult();
        await Assert.CatchAsync<OperationCanceledException>(() => call.WaitAsync(_waitTimeout));
        var callsBeforeFollowUp = CountCalls(server, ListDevicesPath);
        await client.GetDevicesAsync().WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(callsBeforeFollowUp, Is.EqualTo(1));
            Assert.That(LastCall(server, ListDevicesPath), Does.Contain($"sessiontoken={NewSessionToken}"));
        });
    }

    private const string TooManyRequestsBody = /*lang=json,strict*/ """{"src":"SERVER","type":"TOO_MANY_REQUESTS"}""";

    private static Route RenewalAuthFailed() =>
        (_, _) => Reply(FakeMyJdServer.Raw(AuthFailedBody, HttpStatusCode.Forbidden));

    private static ScriptedServer RejectedRenewalServer() =>
        new ScriptedServer()
            .On(ReconnectPath, RenewalAuthFailed())
            .On(ListDevicesPath, TokenInvalid());

    [Test]
    public async Task GetDevicesAsync_RenewalRejected_ClearsSessionAndThrows()
    {
        using var server = RejectedRenewalServer();
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());

        var ex = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ErrorType, Is.EqualTo("AUTH_FAILED"));
            Assert.That(client.IsConnected, Is.False);
            Assert.That(client.ExportSession(), Is.Null);
            Assert.That(CountCalls(server, ListDevicesPath), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetDevicesAsync_RenewalRejected_RaisesSessionChangedNull()
    {
        using var server = RejectedRenewalServer();
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events.Single().Session, Is.Null);
        });
    }

    [Test]
    public async Task GetDevicesAsync_RenewalRejectedHandlerThrows_ThrowsAuthenticationException()
    {
        using var server = RejectedRenewalServer();
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        client.SessionChanged += (_, _) => throw new InvalidOperationException("handler");

        await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.That(client.IsConnected, Is.False);
    }

    [Test]
    public async Task GetDevicesAsync_RenewalRejectedHandlerReconnects_DoesNotDeadlock()
    {
        using var server = RejectedRenewalServer().On(ConnectPath, ConnectOk());
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var gateFree = false;
        client.SessionChanged += (_, args) =>
        {
            if (args.Session is null)
            {
                // Bounded blocking wait: it only completes if the gate is free while the handler runs.
                gateFree = Task.Run(() => client.ConnectAsync(Email, Password)).Wait(_waitTimeout);
            }
        };

        await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(gateFree, Is.True);
            Assert.That(client.IsConnected, Is.True);
        });
    }

    [Test]
    public async Task GetDevicesAsync_RenewalRejectedAfterRestore_KeepsRestoredSession()
    {
        var client = default(MyJDownloaderClient);
        var other = new MyJdSession(Email, Key(5), Key(6), OtherSessionToken, "bb66bb66", Key(7), Key(8));
        Task<HttpResponseMessage> RestoreThenReject(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            client!.RestoreSession(other);
            return RenewalAuthFailed()(request, cancellationToken);
        }

        using var server = new ScriptedServer()
            .On(ReconnectPath, RestoreThenReject)
            .On(ListDevicesPath, TokenInvalid());
        client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(client.ExportSession(), Is.SameAs(other));
            Assert.That(client.IsConnected, Is.True);
            Assert.That(events, Is.Empty);
        });
    }

    [Test]
    public async Task GetDevicesAsync_RenewalServerError_KeepsSessionAndThrows()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, (_, _) => Reply(FakeMyJdServer.Raw("<html>unavailable</html>", HttpStatusCode.ServiceUnavailable)))
            .On(ListDevicesPath, TokenInvalid());
        var client = CreateClient(server);
        var restored = CreateSession();
        client.RestoreSession(restored);
        var events = Collect(client);

        await Assert.ThrowsAsync<MyJdApiException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(client.ExportSession(), Is.SameAs(restored));
            Assert.That(events, Is.Empty);
        });
    }

    [Test]
    public async Task GetDevicesAsync_RenewalRateLimited_KeepsSessionAndThrows()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, (_, _) => Reply(FakeMyJdServer.Raw(TooManyRequestsBody, (HttpStatusCode)429)))
            .On(ListDevicesPath, TokenInvalid());
        var client = CreateClient(server);
        var restored = CreateSession();
        client.RestoreSession(restored);
        var events = Collect(client);

        await Assert.ThrowsAsync<MyJdRateLimitException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(client.ExportSession(), Is.SameAs(restored));
            Assert.That(events, Is.Empty);
        });
    }

    [Test]
    public async Task GetDevicesAsync_CancelledWhileWaitingForGate_ThrowsWithoutRenewalOrReplay()
    {
        var entered = NewSignal();
        var gate = NewSignal();
        var tokenInvalidServed = NewSignal();
        Task<HttpResponseMessage> SignalledTokenInvalid(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            tokenInvalidServed.TrySetResult();
            return TokenInvalid()(request, cancellationToken);
        }

        using var server = new ScriptedServer()
            .On(ReconnectPath, Gated(ReconnectOk(Key(3)), entered, gate))
            .On(ListDevicesPath, SignalledTokenInvalid);
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var publicReconnect = client.ReconnectAsync();
        await entered.Task.WaitAsync(_waitTimeout);
        using var cts = new CancellationTokenSource();
        var call = client.GetDevicesAsync(cts.Token);
        await tokenInvalidServed.Task.WaitAsync(_waitTimeout);

        await cts.CancelAsync();
        gate.SetResult();
        await Assert.CatchAsync<OperationCanceledException>(() => call.WaitAsync(_waitTimeout));
        await publicReconnect.WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(CountCalls(server, ReconnectPath), Is.EqualTo(1));
            Assert.That(CountCalls(server, ListDevicesPath), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetDevicesAsync_TwoHandlersThrowDuringRenewal_ThrowsAggregateAfterReplay()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(ListDevicesPath, Sequence(TokenInvalid(), DevicesOk(RenewedServerKey())));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        client.SessionChanged += (_, _) => throw new InvalidOperationException("first");
        client.SessionChanged += (_, _) => throw new InvalidOperationException("second");

        var ex = await Assert.ThrowsAsync<AggregateException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(CountCalls(server, ListDevicesPath), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task GetDevicesAsync_HandlerDisconnectsDuringRenewal_DoesNotDeadlock()
    {
        using var server = new ScriptedServer()
            .On(ReconnectPath, ReconnectOk(Key(3)))
            .On(DisconnectPath, DisconnectOk(RenewedServerKey()))
            .On(ListDevicesPath, Sequence(TokenInvalid(), DevicesOk(RenewedServerKey())));
        var client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var gateFree = false;
        client.SessionChanged += (_, args) =>
        {
            if (args.Session is not null)
            {
                // Bounded blocking wait: it only completes if the gate is free while the handler runs.
                gateFree = Task.Run(() => client.DisconnectAsync()).Wait(_waitTimeout);
            }
        };

        // The replay outcome is irrelevant here: only completion (no deadlock) matters.
        await Task.WhenAny(client.GetDevicesAsync()).WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(gateFree, Is.True);
            Assert.That(CountCalls(server, DisconnectPath), Is.EqualTo(1));
        });
    }

    private static MyJdSession CreateOtherSession(string email) =>
        new(email, Key(5), Key(6), OtherSessionToken, "bb66bb66", Key(7), Key(8));

    private static Route RestoringDuringReconnect(Func<MyJDownloaderClient> client, MyJdSession other, Route inner) =>
        (request, cancellationToken) =>
        {
            client().RestoreSession(other);
            return inner(request, cancellationToken);
        };

    [Test]
    public async Task GetDevicesAsync_RestoreDuringRenewal_KeepsRestoredSession()
    {
        MyJDownloaderClient? client = null;
        var other = CreateOtherSession(OtherEmail);
        using var server = new ScriptedServer()
            .On(ReconnectPath, RestoringDuringReconnect(() => client!, other, ReconnectOk(Key(3))))
            .On(ListDevicesPath, TokenInvalid());
        client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        var ex = await Assert.ThrowsAsync<MyJdAuthenticationException>(() => client.GetDevicesAsync().WaitAsync(_waitTimeout));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ErrorType, Is.EqualTo(TokenInvalidType));
            Assert.That(client.ExportSession(), Is.SameAs(other));
            Assert.That(events, Is.Empty);
            Assert.That(CountCalls(server, ListDevicesPath), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GetDevicesAsync_SameAccountRestoreDuringRenewal_ReplaysWithRestoredSession()
    {
        MyJDownloaderClient? client = null;
        var other = CreateOtherSession(Email);
        using var server = new ScriptedServer()
            .On(ReconnectPath, RestoringDuringReconnect(() => client!, other, ReconnectOk(Key(3))))
            .On(ListDevicesPath, Sequence(TokenInvalid(), DevicesOk(Key(7))));
        client = CreateClient(server);
        client.RestoreSession(CreateSession());
        var events = Collect(client);

        var devices = await client.GetDevicesAsync().WaitAsync(_waitTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(devices, Has.Count.EqualTo(2));
            Assert.That(client.ExportSession(), Is.SameAs(other));
            Assert.That(events, Is.Empty);
            Assert.That(LastCall(server, ListDevicesPath), Does.Contain($"sessiontoken={OtherSessionToken}"));
        });
    }
}
