namespace MyJdownloader;

using System.Diagnostics.CodeAnalysis;
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
    /// <remarks><see cref="RestoreSession"/> does not raise this event.</remarks>
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
        Volatile.Write(ref this.session, session);
        Interlocked.Increment(ref this.version);
    }

    /// <summary>Connects to the MyJDownloader server and replaces any current session.</summary>
    /// <param name="email">The account email.</param>
    /// <param name="password">The account password; it is never stored.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new session.</returns>
    /// <exception cref="ArgumentException">The email or password is invalid (thrown synchronously).</exception>
    public Task<MyJdSession> ConnectAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var request = this.serverChannel.CreateConnectRequest(email, password);
        return this.ConnectCoreAsync(request, cancellationToken);
    }

    /// <summary>Refreshes the current session using its regain token.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The new session.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
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
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
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

    private void OnSessionChanged(MyJdSession? changed, long expectedVersion)
    {
        if (Volatile.Read(ref this.version) == expectedVersion)
        {
            this.SessionChanged?.Invoke(this, new MyJdSessionChangedEventArgs(changed));
        }
    }
}
