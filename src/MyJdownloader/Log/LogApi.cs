namespace MyJdownloader.Log;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/log</c> namespace of one device (log folders and support upload).</summary>
/// <remarks>All content returned by the device is untrusted.</remarks>
public sealed class LogApi
{
    private const string Base = "/log/";

    private readonly IDeviceChannel _channel;

    internal LogApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Lists the log folders available on the device (<c>/log/getAvailableLogs</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The log folders; an empty list when the device returns none.</returns>
    /// <remarks>
    /// The device writes a thread dump into its current log before answering. The folder of the current session (<see cref="LogFolder.Current"/> is <see langword="true"/>) is listed first. The content comes from the device and is untrusted.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<LogFolder>> GetAvailableLogsAsync(CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(this._channel.CallAsync(Base + "getAvailableLogs", LogJsonContext.Default.ListLogFolder, [], cancellationToken));

    /// <summary>Uploads the selected log folders of the device to JDownloader's support servers (<c>/log/sendLogFile</c>).</summary>
    /// <param name="logFolders">The folders to send, typically taken from <see cref="GetAvailableLogsAsync"/>; each needs its <see cref="LogFolder.Created"/> and <see cref="LogFolder.LastModified"/> dates.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The log id, or <see langword="null"/> when the device sent nothing (no folder matched or the upload failed).</returns>
    /// <remarks>
    /// <para><b>Privacy.</b> This call uploads the selected log folders of the device to JDownloader's support servers, a third party, immediately and without any confirmation. Logs can contain personal data: download URLs, file names and paths, account user names and IP addresses. The device always sends the log of the current session too, whatever the selection. Obtain the explicit consent of the end user first and never call this method unattended.</para>
    /// <para>The returned id is what JDownloader support asks for; treat it as untrusted device text.</para>
    /// <para>The device zips and uploads the folders, so the call can be long: it answers after the first folder upload and continues with the rest in the background. The device itself rejects an empty selection with 400 BAD_PARAMETERS (<see cref="MyJdApiException"/>); this method validates earlier.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="logFolders"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="logFolders"/> is empty, contains a <see langword="null"/> element, or an element without <see cref="LogFolder.Created"/> or <see cref="LogFolder.LastModified"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the upload may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the upload may have been executed.</exception>
    public Task<string?> SendLogFileAsync(IReadOnlyList<LogFolder> logFolders, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logFolders);
        if (logFolders.Count == 0)
        {
            throw new ArgumentException("At least one log folder must be selected.", nameof(logFolders));
        }

        foreach (var folder in logFolders)
        {
            if (folder is null || folder.Created is null || folder.LastModified is null)
            {
                throw new ArgumentException("Each log folder must be set and have its Created and LastModified dates.", nameof(logFolders));
            }
        }

        var args = new[] { JsonSerializer.SerializeToElement([.. logFolders], LogJsonContext.Default.LogFolderArray) };
        return this._channel.CallAsync(Base + "sendLogFile", FacadeJsonContext.Default.String, args, cancellationToken);
    }
}
