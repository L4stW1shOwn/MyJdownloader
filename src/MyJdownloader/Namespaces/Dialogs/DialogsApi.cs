namespace MyJdownloader.Dialogs;

using System.Text.Json;
using MyJdownloader.Errors;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/dialogs</c> namespace of one device (dialogs JDownloader is waiting on).</summary>
/// <remarks>
/// <see cref="AnswerAsync"/> answers a dialog on the device without confirmation: JDownloader acts on it immediately.
/// Text returned by the device (<see cref="DialogInfo"/>, <see cref="DialogTypeInfo"/>) is untrusted.
/// Never answer dialogs automatically or in bulk without user review: some dialogs confirm overwrites, updates or trust decisions.
/// Dialog properties and answer values may hold secrets (for example login or password dialogs): never log them.
/// </remarks>
public sealed class DialogsApi
{
    private const string Base = "/dialogs/";

    private readonly IDeviceChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="DialogsApi"/> class over a device channel.</summary>
    /// <remarks>
    /// <see cref="JDownloaderDevice"/> creates the facade for you; this constructor lets you build one over your own
    /// <see cref="IDeviceChannel"/>, for example a test double.
    /// </remarks>
    /// <param name="channel">The channel the device actions are sent through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channel"/> is <see langword="null"/>.</exception>
    public DialogsApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Lists the identifiers of the dialogs currently open (<c>/dialogs/list</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The dialog identifiers; empty when the device returns no data.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public async Task<IReadOnlyList<long>> ListAsync(CancellationToken cancellationToken = default)
        => await _channel.CallAsync(Base + "list", FacadeJsonContext.Default.Int64Array, [], cancellationToken).ConfigureAwait(false) ?? [];

    /// <summary>Gets an open dialog (<c>/dialogs/get</c>).</summary>
    /// <param name="id">The dialog identifier, as returned by <see cref="ListAsync"/>.</param>
    /// <param name="includeIcon">Whether the device should include the dialog icon; <see langword="false"/> by default as icons are large. The icon arrives as a Base64 data URL inside <see cref="DialogInfo.Properties"/> (there is no dedicated member): it is untrusted, so check the <c>data:image/</c> prefix before rendering it.</param>
    /// <param name="includeProperties">Whether the device should include the dialog properties; <see langword="true"/> by default.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The dialog.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the id is unknown the device answers 400 "Invalid ID", which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<DialogInfo> GetAsync(long id, bool includeIcon = false, bool includeProperties = true, CancellationToken cancellationToken = default)
    {
        var args = new[] { Id(id), Flag(includeIcon), Flag(includeProperties) };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "get", DialogsJsonContext.Default.DialogInfo, args, cancellationToken));
    }

    /// <summary>Describes the properties of a dialog type (<c>/dialogs/getTypeInfo</c>).</summary>
    /// <param name="dialogType">The dialog type, as reported by <see cref="DialogInfo.Type"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The properties of the dialog type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dialogType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="dialogType"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty. When the type is unknown the device answers 400 "Requested Type not found!", which surfaces as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<DialogTypeInfo> GetTypeInfoAsync(string dialogType, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(dialogType, nameof(dialogType), FacadeGuard.MaxIdentifierLength);
        var args = new[] { JsonSerializer.SerializeToElement(dialogType, FacadeJsonContext.Default.String) };
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "getTypeInfo", DialogsJsonContext.Default.DialogTypeInfo, args, cancellationToken));
    }

    /// <summary>Answers an open dialog (<c>/dialogs/answer</c>).</summary>
    /// <param name="id">The dialog identifier, as returned by <see cref="ListAsync"/>.</param>
    /// <param name="data">The answer properties (see <see cref="DialogTypeInfo.Out"/>), sent to the device as one JSON object; values are untyped (<see cref="JsonElement"/>) and must not be <c>default</c>. The values may hold secrets (for example passwords): never log them.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered; the device result is ignored.</returns>
    /// <remarks>The answer is applied on the device immediately and without confirmation, and cannot be undone: the dialog closes and JDownloader continues with the supplied values.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/> or contains a <see langword="null"/> key.</exception>
    /// <exception cref="ArgumentException">A key of <paramref name="data"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates, or a value is <c>default</c>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid. When the id is unknown the device answers 400 "Invalid ID", and when the dialog needs another answer first 400 "Answer Dialog N first"; both surface as a <see cref="MyJdApiException"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task AnswerAsync(long id, IReadOnlyDictionary<string, JsonElement> data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        var copy = new Dictionary<string, JsonElement>(data.Count, StringComparer.Ordinal);
        foreach (var (key, value) in data)
        {
            FacadeGuard.ValidateText(key, nameof(data), FacadeGuard.MaxIdentifierLength);
            if (value.ValueKind == JsonValueKind.Undefined)
            {
                throw new ArgumentException("A value must not be a default JsonElement.", nameof(data));
            }

            copy.Add(key, value);
        }

        var args = new[] { Id(id), JsonSerializer.SerializeToElement(copy, DialogsJsonContext.Default.DictionaryStringJsonElement) };
        return _channel.CallAsync(Base + "answer", args, cancellationToken);
    }

    private static JsonElement Id(long value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.Int64);

    private static JsonElement Flag(bool value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.Boolean);
}
