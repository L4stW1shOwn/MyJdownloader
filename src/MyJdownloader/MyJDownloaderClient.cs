namespace MyJdownloader;

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

    private readonly MyJDownloaderOptions options;
    private readonly RequestIdGenerator requestIds;

    private readonly ServerChannel serverChannel;

    private readonly SemaphoreSlim gate = new(1, 1);
    private MyJdSession? session;
    private long version;

    /// <summary>Initializes a new instance of the <see cref="MyJDownloaderClient"/> class.</summary>
    /// <param name="httpClient">The HTTP client used for every request; it is not disposed by the client.</param>
    /// <param name="options">The client options, or <see langword="null"/> for the defaults. The values are copied.</param>
    /// <param name="timeProvider">The time provider, or <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The options are invalid.</exception>
    public MyJDownloaderClient(HttpClient httpClient, MyJDownloaderOptions? options = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this.options = CopyAndValidate(options ?? new MyJDownloaderOptions());
        var time = timeProvider ?? TimeProvider.System;
        this.requestIds = new RequestIdGenerator(time);
        this.serverChannel = new ServerChannel(httpClient, this.options, this.requestIds, time);
    }

    /// <summary>Occurs when the session is established, refreshed or cleared by the client.</summary>
    /// <remarks>
    /// <see cref="RestoreSession"/> does not raise this event. All handlers are invoked even if one throws; their exceptions
    /// propagate to the caller of the operation after the session was updated. <see cref="MyJdSessionChangedEventArgs.Session"/>
    /// may already be outdated: persist <see cref="ExportSession"/>.
    /// </remarks>
    public event EventHandler<MyJdSessionChangedEventArgs>? SessionChanged;

    /// <summary>Gets a value indicating whether the client currently holds a session.</summary>
    public bool IsConnected => Volatile.Read(ref this.session) is not null;

    /// <summary>Returns the current session, or <see langword="null"/> when the client is not connected.</summary>
    /// <returns>The current session, or <see langword="null"/>.</returns>
    public MyJdSession? ExportSession() => Volatile.Read(ref this.session);

    /// <summary>Restores a previously exported session without sending any request or raising <see cref="SessionChanged"/>.</summary>
    /// <param name="session">The session to restore.</param>
    /// <remarks>A restore concurrent with a connect or reconnect may be overwritten by it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public void RestoreSession(MyJdSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Interlocked.Increment(ref this.version);
        Volatile.Write(ref this.session, session);
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
        var request = this.serverChannel.CreateConnectRequest(email, password);
        return this.ConnectCoreAsync(request, cancellationToken);
    }

    /// <summary>Refreshes the current session using its regain token.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new session.</returns>
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
        await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = Volatile.Read(ref this.session) ?? throw new InvalidOperationException(NotConnectedMessage);
            newSession = await this.serverChannel.ReconnectAsync(current, cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref this.session, newSession);
            v = Interlocked.Increment(ref this.version);
        }
        finally
        {
            this.gate.Release();
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
        await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            old = Interlocked.Exchange(ref this.session, null);
            if (old is null)
            {
                return;
            }

            v = Interlocked.Increment(ref this.version);
        }
        finally
        {
            this.gate.Release();
        }

        try
        {
            await this.serverChannel.DisconnectAsync(old, cancellationToken).ConfigureAwait(false);
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
    /// Uses a lock-free snapshot of the current session. There is no automatic reconnection yet
    /// (a later version adds a single reconnect-and-replay on <c>TOKEN_INVALID</c>).
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The server rejected the request or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var current = Volatile.Read(ref this.session) ?? throw new InvalidOperationException(NotConnectedMessage);
        return await this.serverChannel.ListDevicesAsync(current, cancellationToken).ConfigureAwait(false);
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
            await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                newSession = await this.serverChannel.ConnectAsync(request, cancellationToken).ConfigureAwait(false);
                Volatile.Write(ref this.session, newSession);
                v = Interlocked.Increment(ref this.version);
            }
            finally
            {
                this.gate.Release();
            }
        }
        finally
        {
            request.Dispose();
        }

        this.OnSessionChanged(newSession, v);
        return newSession;
    }

    [SuppressMessage("Design", "CA1031", Justification = "Handler exceptions are collected so every handler runs, then rethrown")]
    private void OnSessionChanged(MyJdSession? changed, long expectedVersion)
    {
        if (Volatile.Read(ref this.version) != expectedVersion)
        {
            return;
        }

        var handlers = this.SessionChanged;
        if (handlers is null)
        {
            return;
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
            return;
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        throw new AggregateException(failures);
    }
}
