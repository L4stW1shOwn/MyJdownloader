namespace MyJdownloader;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using MyJdownloader.Transport;

/// <summary>Client for the MyJDownloader relay server.</summary>
/// <remarks>
/// <para>
/// The client is thread-safe and intended to be used as a singleton. It does not own the
/// <see cref="HttpClient"/> it receives and is not <see cref="IDisposable"/>.
/// </para>
/// <para>
/// <see cref="SessionChanged"/> handlers run on an arbitrary thread; marshal to a UI thread yourself.
/// Handlers should persist <see cref="ExportSession"/> (the latest state) rather than
/// <see cref="MyJdSessionChangedEventArgs.Session"/>.
/// </para>
/// <para>
/// Disconnecting raises <see cref="SessionChanged"/> with a <see langword="null"/> session after the server call,
/// even if that call fails. If both the server call and a handler throw, the handler's exception replaces the server error.
/// </para>
/// <para>
/// All <see cref="SessionChanged"/> handlers are invoked even if one throws. Handler exceptions propagate to the caller
/// of the operation after the session was updated (check <see cref="IsConnected"/> or <see cref="ExportSession"/>):
/// a single failure is rethrown as is, several failures are wrapped in an <see cref="AggregateException"/>.
/// <see cref="MyJdSessionChangedEventArgs.Session"/> may already be outdated when a handler runs; persist <see cref="ExportSession"/>.
/// </para>
/// </remarks>
[SuppressMessage("Design", "CA1001", Justification = "AvailableWaitHandle is never used, the semaphore holds no unmanaged resource")]
public sealed class MyJDownloaderClient
{
    private const int MaxAppKeyLength = 256;
    private const string NotConnectedMessage = "The client is not connected.";
    private const string TokenInvalidType = "TOKEN_INVALID";

    private readonly MyJDownloaderOptions _options;
    private readonly RequestIdGenerator _requestIds;

    private readonly ServerChannel _serverChannel;
    private readonly RelayTransport _transport;

    private readonly SemaphoreSlim _gate = new(1, 1);
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private MyJdSession? _session;
    private long _version;

    /// <summary>Initializes a new instance of the <see cref="MyJDownloaderClient"/> class.</summary>
    /// <remarks>
    /// For a caller-built <see cref="HttpClient"/>, use a long-lived <see cref="SocketsHttpHandler"/> with
    /// <see cref="SocketsHttpHandler.PooledConnectionLifetime"/> set, <see cref="SocketsHttpHandler.AllowAutoRedirect"/>
    /// set to <see langword="false"/>, and no HTTP logging: device-call URL paths carry the session token.
    /// </remarks>
    /// <param name="httpClient">The HTTP client used for every request; it is not disposed by the client.</param>
    /// <param name="options">The client options, or <see langword="null"/> for the defaults. The values are copied.</param>
    /// <param name="timeProvider">The time provider, or <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public MyJDownloaderClient(HttpClient httpClient, MyJDownloaderOptions? options = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this._options = CopyAndValidate(options ?? new MyJDownloaderOptions());
        var time = timeProvider ?? TimeProvider.System;
        this._requestIds = new RequestIdGenerator(time);
        this._transport = new RelayTransport(httpClient, time);
        this._serverChannel = new ServerChannel(this._transport, this._options, this._requestIds);
    }

    /// <summary>Occurs when the session is established, refreshed or cleared by the client.</summary>
    /// <remarks>
    /// <see cref="RestoreSession"/> does not raise this event. When an automatic renewal is rejected, handler exceptions are
    /// not propagated (the authentication exception prevails). All handlers are invoked even if one throws; their exceptions
    /// propagate to the caller of the operation after the session was updated. <see cref="MyJdSessionChangedEventArgs.Session"/>
    /// may already be outdated: persist <see cref="ExportSession"/>.
    /// </remarks>
    public event EventHandler<MyJdSessionChangedEventArgs>? SessionChanged;

    /// <summary>Gets a value indicating whether the client currently holds a session.</summary>
    public bool IsConnected => Volatile.Read(ref this._session) is not null;

    /// <summary>Returns the current session, or <see langword="null"/> when the client is not connected.</summary>
    /// <returns>The current session, or <see langword="null"/>.</returns>
    public MyJdSession? ExportSession() => Volatile.Read(ref this._session);

    /// <summary>Restores a previously exported session without sending any request or raising <see cref="SessionChanged"/>.</summary>
    /// <param name="session">The session to restore.</param>
    /// <remarks>A restore concurrent with a connect may be overwritten by it; a restore concurrent with a reconnect or an automatic renewal wins.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public void RestoreSession(MyJdSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Interlocked.Increment(ref this._version);
        Volatile.Write(ref this._session, session);
    }

    /// <summary>Connects to the MyJDownloader server and replaces any current session.</summary>
    /// <param name="email">The account email.</param>
    /// <param name="password">The account password; it is never stored.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new session.</returns>
    /// <remarks>
    /// Argument validation exceptions (<see cref="ArgumentException"/>, <see cref="ArgumentNullException"/>) are thrown synchronously,
    /// before the returned <see cref="Task{TResult}"/>; this is deliberate so that the password is never captured by asynchronous code.
    /// </remarks>
    /// <exception cref="ArgumentException">The email or password is invalid (thrown synchronously).</exception>
    /// <exception cref="ArgumentNullException">The email or password is <see langword="null"/> (thrown synchronously).</exception>
    /// <exception cref="MyJdException">The server rejected the request or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    /// <exception cref="AggregateException">Several <see cref="SessionChanged"/> handlers threw.</exception>
    public Task<MyJdSession> ConnectAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var request = this._serverChannel.CreateConnectRequest(email, password);
        return this.ConnectCoreAsync(request, cancellationToken);
    }

    /// <summary>Refreshes the current session using its regain token.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new session.</returns>
    /// <remarks>
    /// If <see cref="RestoreSession"/> replaces the session while the request is in flight, the restored session wins:
    /// the renewed tokens are discarded, <see cref="SessionChanged"/> is not raised and the restored session is returned.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The server rejected the request or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    /// <exception cref="AggregateException">Several <see cref="SessionChanged"/> handlers threw.</exception>
    public async Task<MyJdSession> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        MyJdSession newSession;
        long v;
        await this._gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = Volatile.Read(ref this._session) ?? throw new InvalidOperationException(NotConnectedMessage);
            newSession = await this._serverChannel.ReconnectAsync(current, cancellationToken).ConfigureAwait(false);

            // A concurrent RestoreSession wins: the renewed tokens are discarded (valid server-side until they expire).
            var replaced = Interlocked.CompareExchange(ref this._session, newSession, current);
            if (!ReferenceEquals(replaced, current))
            {
                return replaced ?? throw new InvalidOperationException(NotConnectedMessage);
            }

            v = Interlocked.Increment(ref this._version);
        }
        finally
        {
            this._gate.Release();
        }

        this.OnSessionChanged(newSession, v);
        return newSession;
    }

    /// <summary>Disconnects from the server; the client is disconnected even if the server call fails.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    /// <remarks>
    /// The local session is cleared before the server call. If <c>/my/disconnect</c> fails or is cancelled,
    /// the tokens stay valid on the server until they expire.
    /// </remarks>
    /// <exception cref="MyJdException">The server rejected the request or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    /// <exception cref="AggregateException">Several <see cref="SessionChanged"/> handlers threw.</exception>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        MyJdSession? old;
        long v;
        await this._gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            old = Interlocked.Exchange(ref this._session, null);
            if (old is null)
            {
                return;
            }

            v = Interlocked.Increment(ref this._version);
        }
        finally
        {
            this._gate.Release();
        }

        try
        {
            await this._serverChannel.DisconnectAsync(old, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this.OnSessionChanged(null, v);
        }
    }

    /// <summary>Lists the devices attached to the account.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The devices.</returns>
    /// <remarks>
    /// Uses a lock-free snapshot of the current session. Only a server-sourced <c>TOKEN_INVALID</c> error triggers a renewal:
    /// the session is renewed once (single-flight across concurrent callers) and the call is replayed once.
    /// A rejected renewal disconnects the client (if the failed session is still the current one) and raises
    /// <see cref="SessionChanged"/> with a <see langword="null"/> session; the authentication error is rethrown.
    /// Exceptions thrown by <see cref="SessionChanged"/> handlers in that case are not propagated: the authentication exception prevails.
    /// Other renewal failures keep the session. A session restored during the renewal is kept (the renewed one is discarded) and,
    /// if it belongs to the same account, used for the replay; for another account it is not replayed and
    /// the original <c>TOKEN_INVALID</c> error is rethrown. The renewal request is not cancellable by the caller
    /// (it is bounded by <see cref="HttpClient.Timeout"/>).
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdAuthenticationException">The session renewal was rejected by the server.</exception>
    /// <exception cref="MyJdException">The server rejected the request or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    /// <exception cref="AggregateException">Several <see cref="SessionChanged"/> handlers threw.</exception>
    public Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
        => this.ExecuteAsync(this._serverChannel.ListDevicesAsync, cancellationToken);

    /// <summary>Gets the facade root of one device of the account.</summary>
    /// <param name="deviceId">The device id, as returned by <see cref="GetDevicesAsync"/>.</param>
    /// <returns>A new <see cref="JDownloaderDevice"/> on every call.</returns>
    /// <remarks>
    /// The id is validated (and escaped in request paths) but no request is sent and the device is not checked to exist.
    /// The session is read on every call made through the device, not when it is obtained: the device can be obtained
    /// before <see cref="RestoreSession"/> and survives session renewals. <see cref="JDownloaderDevice.Channel"/> gives
    /// access to any device action now; typed namespace properties come later.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="deviceId"/> is blank, too long or contains invalid characters.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="deviceId"/> is <see langword="null"/>.</exception>
    public JDownloaderDevice GetDevice(string deviceId)
        => new(deviceId, this.CreateDeviceChannel(deviceId));

    /// <summary>Creates the channel to one device of the account.</summary>
    /// <param name="deviceId">The device id, as returned by <see cref="GetDevicesAsync"/>.</param>
    /// <returns>The device channel.</returns>
    /// <exception cref="ArgumentException"><paramref name="deviceId"/> is blank, too long or contains invalid characters.</exception>
    internal IDeviceChannel CreateDeviceChannel(string deviceId)
        => new DeviceChannel(this, this._transport, this._options, this._requestIds, deviceId);

    /// <summary>Runs a session-bound call, renewing the session once and replaying the call once on <c>TOKEN_INVALID</c>.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="call">The call; it must be safe to replay once, because <c>TOKEN_INVALID</c> means the request was rejected before execution.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the call.</returns>
    internal async Task<T> ExecuteAsync<T>(Func<MyJdSession, CancellationToken, Task<T>> call, CancellationToken cancellationToken)
    {
        var snapshot = Volatile.Read(ref this._session) ?? throw new InvalidOperationException(NotConnectedMessage);
        ExceptionDispatchInfo original;
        try
        {
            return await call(snapshot, cancellationToken).ConfigureAwait(false);
        }
        catch (MyJdException ex) when (ex.ErrorSource == MyJdErrorSource.Server
            && string.Equals(ex.ErrorType, TokenInvalidType, StringComparison.Ordinal))
        {
            original = ExceptionDispatchInfo.Capture(ex);
        }

        var (renewed, handlerFailure) = await this.RenewAsync(snapshot, cancellationToken).ConfigureAwait(false);
        if (renewed is null)
        {
            original.Throw();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var result = await call(renewed, cancellationToken).ConfigureAwait(false);
        if (handlerFailure is not null)
        {
            ExceptionDispatchInfo.Capture(handlerFailure).Throw();
        }

        return result;
    }

    private static MyJDownloaderOptions CopyAndValidate(MyJDownloaderOptions options)
    {
        var address = options.BaseAddress;
        if (address is null
            || !address.IsAbsoluteUri
            || address.Scheme != Uri.UriSchemeHttps
            || address.UserInfo.Length > 0
            || address.Query.Length > 0
            || address.Fragment.Length > 0)
        {
            throw new ArgumentException("BaseAddress must be an absolute https URI without user info, query or fragment.", nameof(options));
        }

        var appKey = options.AppKey;
        if (string.IsNullOrWhiteSpace(appKey)
            || appKey.Length > MaxAppKeyLength
            || appKey.Any(char.IsControl))
        {
            throw new ArgumentException("AppKey must be a non-blank string of at most 256 characters without control characters.", nameof(options));
        }

        if (options.ApiVersion < 1)
        {
            throw new ArgumentException("ApiVersion must be greater than or equal to 1.", nameof(options));
        }

        return new MyJDownloaderOptions { AppKey = appKey, BaseAddress = address, ApiVersion = options.ApiVersion };
    }

    private async Task<MyJdSession> ConnectCoreAsync(ConnectRequest request, CancellationToken cancellationToken)
    {
        MyJdSession newSession;
        long v;
        try
        {
            await this._gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                newSession = await this._serverChannel.ConnectAsync(request, cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref this._session, newSession);
                v = Interlocked.Increment(ref this._version);
            }
            finally
            {
                this._gate.Release();
            }
        }
        finally
        {
            request.Dispose();
        }

        this.OnSessionChanged(newSession, v);
        return newSession;
    }

    private void OnSessionChanged(MyJdSession? changed, long expectedVersion)
    {
        var failure = this.CollectSessionChanged(changed, expectedVersion);
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static (MyJdSession? Session, Exception? HandlerFailure) ReplayTarget(MyJdSession? current, MyJdSession failed)
    {
        if (current is null)
        {
            throw new InvalidOperationException(NotConnectedMessage);
        }

        var sameAccount = string.Equals(current.Email, failed.Email, StringComparison.OrdinalIgnoreCase);
        return (sameAccount ? current : null, null);
    }

    private async Task<(MyJdSession? Session, Exception? HandlerFailure)> RenewAsync(MyJdSession failed, CancellationToken cancellationToken)
    {
        MyJdSession renewed;
        long v = 0;
        ExceptionDispatchInfo? rejected = null;
        var cleared = false;
        await this._gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = Volatile.Read(ref this._session);
            if (!ReferenceEquals(current, failed))
            {
                return ReplayTarget(current, failed);
            }

            try
            {
                // Bounded by HttpClient.Timeout: a cancelled caller must not abort a renewal other callers may rely on.
                renewed = await this._serverChannel.ReconnectAsync(failed, CancellationToken.None).ConfigureAwait(false);
            }
            catch (MyJdAuthenticationException ex)
            {
                // Clear only if the failed session is still current: a concurrent RestoreSession must survive.
                rejected = ExceptionDispatchInfo.Capture(ex);
                cleared = ReferenceEquals(Interlocked.CompareExchange(ref this._session, null, failed), failed);
                v = cleared ? Interlocked.Increment(ref this._version) : 0;
                renewed = failed;
            }

            if (rejected is null)
            {
                // A concurrent RestoreSession wins: the renewed tokens are discarded (valid server-side until they expire).
                if (!ReferenceEquals(Interlocked.CompareExchange(ref this._session, renewed, failed), failed))
                {
                    return ReplayTarget(Volatile.Read(ref this._session), failed);
                }

                v = Interlocked.Increment(ref this._version);
            }
        }
        finally
        {
            this._gate.Release();
        }

        if (rejected is not null)
        {
            if (cleared)
            {
                _ = this.CollectSessionChanged(null, v);
            }

            rejected.Throw();
        }

        var handlerFailure = this.CollectSessionChanged(renewed, v);
        return (renewed, handlerFailure);
    }

    [SuppressMessage("Design", "CA1031", Justification = "Handler exceptions are collected so every handler runs, then rethrown")]
    private Exception? CollectSessionChanged(MyJdSession? changed, long expectedVersion)
    {
        if (Volatile.Read(ref this._version) != expectedVersion)
        {
            return null;
        }

        var handlers = this.SessionChanged;
        if (handlers is null)
        {
            return null;
        }

        var args = new MyJdSessionChangedEventArgs(changed);
        List<Exception>? failures = null;
        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((EventHandler<MyJdSessionChangedEventArgs>)handler)(this, args);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }

        if (failures is null)
        {
            return null;
        }

        return failures.Count == 1 ? failures[0] : new AggregateException(failures);
    }
}
