namespace MyJdownloader.DeviceExtensions;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/extensions</c> namespace of one device (JDownloader extensions).</summary>
/// <remarks>Text returned by the device is untrusted: encode it before displaying it.</remarks>
public sealed class ExtensionsApi
{
    private const string Base = "/extensions/";

    private readonly IDeviceChannel _channel;

    internal ExtensionsApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Lists the extensions (<c>/extensions/list</c>).</summary>
    /// <param name="query">The extensions and fields to return, or <see langword="null"/> for all extensions with all fields.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The extensions; empty when the device returns no data.</returns>
    /// <remarks><see cref="ExtensionQuery.Pattern"/> is a regular expression evaluated on the device.</remarks>
    /// <exception cref="ArgumentException"><see cref="ExtensionQuery.Pattern"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<Extension>> ListAsync(ExtensionQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new ExtensionQuery();
        FacadeGuard.ValidateOptionalText(query.Pattern, nameof(query));
        var args = new[] { JsonSerializer.SerializeToElement(query, ExtensionsJsonContext.Default.ExtensionQuery) };
        return FacadeResults.ListAsync(
            this._channel.CallAsync(Base + "list", ExtensionsJsonContext.Default.ListExtension, args, cancellationToken),
            static item => item is null);
    }

    /// <summary>Installs an extension (<c>/extensions/install</c>).</summary>
    /// <param name="id">The extension identifier (<see cref="Extension.Id"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the installation.</returns>
    /// <remarks>
    /// The device downloads and installs the extension immediately, without confirmation, from its own JDownloader update source (not from the caller).
    /// Take <paramref name="id"/> from <see cref="Extension.Id"/> or an allow-list, never from end-user input. A timeout does not mean the action failed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> InstallAsync(string id, CancellationToken cancellationToken = default)
        => this.CallBoolAsync("install", id, nameof(id), null, cancellationToken);

    /// <summary>Tells whether an extension is installed (<c>/extensions/isInstalled</c>).</summary>
    /// <param name="id">The extension identifier (<see cref="Extension.Id"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the extension is installed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> IsInstalledAsync(string id, CancellationToken cancellationToken = default)
        => this.CallBoolAsync("isInstalled", id, nameof(id), null, cancellationToken);

    /// <summary>Tells whether an extension is enabled (<c>/extensions/isEnabled</c>).</summary>
    /// <param name="className">The extension class name (<see cref="Extension.Id"/>).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the extension is enabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="className"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="className"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<bool> IsEnabledAsync(string className, CancellationToken cancellationToken = default)
        => this.CallBoolAsync("isEnabled", className, nameof(className), null, cancellationToken);

    /// <summary>Enables or disables an extension (<c>/extensions/setEnabled</c>).</summary>
    /// <param name="className">The extension class name (<see cref="Extension.Id"/>).</param>
    /// <param name="enabled"><see langword="true"/> to enable the extension, <see langword="false"/> to disable it.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device applied the change.</returns>
    /// <remarks>The change is applied to the device configuration immediately, without confirmation. A timeout does not mean the action failed.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="className"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="className"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SetEnabledAsync(string className, bool enabled, CancellationToken cancellationToken = default)
        => this.CallBoolAsync("setEnabled", className, nameof(className), enabled, cancellationToken);

    private Task<bool> CallBoolAsync(string action, string text, string paramName, bool? flag, CancellationToken cancellationToken)
    {
        FacadeGuard.ValidateText(text, paramName);
        var first = JsonSerializer.SerializeToElement(text, FacadeJsonContext.Default.String);
        JsonElement[] args = flag is { } value
            ? [first, JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.Boolean)]
            : [first];
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + action, FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }
}
