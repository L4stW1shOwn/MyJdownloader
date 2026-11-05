namespace MyJdownloader;

/// <summary>Provides data for the <see cref="MyJDownloaderClient.SessionChanged"/> event.</summary>
/// <param name="session">The session at the time of the change, or <see langword="null"/> when the client is disconnected.</param>
public sealed class MyJdSessionChangedEventArgs(MyJdSession? session) : EventArgs
{
    /// <summary>Gets the session at the time of the change, or <see langword="null"/> when the client is disconnected.</summary>
    public MyJdSession? Session { get; } = session;
}
