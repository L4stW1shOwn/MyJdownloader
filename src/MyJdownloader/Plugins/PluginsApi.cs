namespace MyJdownloader.Plugins;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MyJdownloader.Config;
using MyJdownloader.Facades;
using MyJdownloader.Transport;

/// <summary>Typed facade over the <c>/plugins</c> namespace of one device (plugin settings and URL patterns).</summary>
/// <remarks>
/// Values are untyped on the device and are exposed as <see cref="JsonElement"/> (ADR-008). Plugin settings may hold secrets (hoster credentials, tokens): never log them.
/// All text returned by the device is untrusted.
/// </remarks>
public sealed class PluginsApi
{
    private const string Base = "/plugins/";

    private static readonly JsonElement _nullElement = JsonDocument.Parse("null").RootElement.Clone();

    private readonly IDeviceChannel _channel;

    internal PluginsApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Gets the current value of a plugin setting (<c>/plugins/get</c>).</summary>
    /// <param name="interfaceName">The configuration interface name of the plugin.</param>
    /// <param name="displayName">The display name of the plugin.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The value of the setting; an element of kind <see cref="JsonValueKind.Null"/> when the device returns none.</returns>
    /// <remarks>The value may hold a secret: never log it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/>, <paramref name="displayName"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An identifier is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<JsonElement> GetAsync(string interfaceName, string displayName, string key, CancellationToken cancellationToken = default)
    {
        var args = IdentifierArgs(interfaceName, displayName, key);
        return ToValueAsync(this._channel.CallAsync(Base + "get", PluginsJsonContext.Default.NullableJsonElement, args, cancellationToken));
    }

    /// <summary>Sets the value of a plugin setting (<c>/plugins/set</c>).</summary>
    /// <param name="interfaceName">The configuration interface name of the plugin.</param>
    /// <param name="displayName">The display name of the plugin.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The new value.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the value.</returns>
    /// <remarks>
    /// The change is applied to the device configuration immediately, without confirmation. The value is sent as its JSON text (strings are quoted), see ADR-008;
    /// this wire behaviour is to be confirmed by live tests. The value may hold a secret: never log it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/>, <paramref name="displayName"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An identifier is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates, or <paramref name="value"/> is an undefined (default) element or is longer than 1 048 576 characters of JSON text.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SetAsync(string interfaceName, string displayName, string key, JsonElement value, CancellationToken cancellationToken = default)
    {
        var args = IdentifierArgs(interfaceName, displayName, key);
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("The value must not be an undefined JSON element.", nameof(value));
        }

        // The raw text becomes a string argument of the same length.
        if (RequestArgsLimit.RawTextLength(value) > RequestArgsLimit.MaxLength)
        {
            throw new ArgumentException(RequestArgsLimit.ValueMessage, nameof(value));
        }

        var valueArg = JsonSerializer.SerializeToElement(value.GetRawText(), FacadeJsonContext.Default.String);
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "set", FacadeJsonContext.Default.NullableBoolean, [.. args, valueArg], cancellationToken));
    }

    /// <summary>Resets a plugin setting to its default value (<c>/plugins/reset</c>).</summary>
    /// <param name="interfaceName">The configuration interface name of the plugin.</param>
    /// <param name="displayName">The display name of the plugin.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device reset the setting.</returns>
    /// <remarks>The change is applied to the device configuration immediately, without confirmation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/>, <paramref name="displayName"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An identifier is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> ResetAsync(string interfaceName, string displayName, string key, CancellationToken cancellationToken = default)
    {
        var args = IdentifierArgs(interfaceName, displayName, key);
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "reset", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Lists the plugins and their settings (<c>/plugins/list</c>).</summary>
    /// <param name="query">The optional fields to return, or <see langword="null"/> for the pattern and the version.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The plugins; empty when the device returns no data.</returns>
    /// <remarks>
    /// Current values are returned and may hold secrets: never log whole entries.
    /// <see cref="PluginInfo.Pattern"/> is a regular expression defined on the device and is untrusted text: do not compile it without a match timeout.
    /// The encrypted response body is limited to 1 MiB (about 768 KiB of JSON): the list of all plugins can be large, otherwise the call fails with a <see cref="MyJdProtocolException"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<PluginInfo>> ListAsync(PluginsQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PluginsQuery();
        var args = new[] { JsonSerializer.SerializeToElement(query, PluginsJsonContext.Default.PluginsQuery) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "list", PluginsJsonContext.Default.ListPluginInfo, args, cancellationToken));
    }

    /// <summary>Lists plugin configuration entries (<c>/plugins/query</c>).</summary>
    /// <param name="query">The entries and fields to return, or <see langword="null"/> for all entries without their current values.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The entries; empty when the device returns no data.</returns>
    /// <remarks>
    /// Current values are not requested unless <see cref="ConfigQuery.Values"/> is <see langword="true"/>; they may hold secrets, never log whole entries.
    /// <see cref="ConfigQuery.Pattern"/> is a regular expression evaluated on the device.
    /// The encrypted response body is limited to 1 MiB (about 768 KiB of JSON): on a large configuration, restrict the query with <see cref="ConfigQuery.ConfigInterface"/> or <see cref="ConfigQuery.Pattern"/>,
    /// otherwise the call fails with a <see cref="MyJdProtocolException"/>.
    /// </remarks>
    /// <exception cref="ArgumentException"><see cref="ConfigQuery.ConfigInterface"/> or <see cref="ConfigQuery.Pattern"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<PluginConfigEntry>> QueryAsync(ConfigQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new ConfigQuery();
        FacadeGuard.ValidateOptionalText(query.ConfigInterface, nameof(query), FacadeGuard.MaxIdentifierLength);
        FacadeGuard.ValidateOptionalText(query.Pattern, nameof(query), FacadeGuard.MaxLongTextLength);
        var args = new[] { JsonSerializer.SerializeToElement(query, PluginsJsonContext.Default.ConfigQuery) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "query", PluginsJsonContext.Default.ListPluginConfigEntry, args, cancellationToken));
    }

    /// <summary>Gets the URL patterns of all plugins (<c>/plugins/getAllPluginRegex</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The regular expressions of each plugin, keyed by plugin; empty when the device returns no data.</returns>
    /// <remarks>The patterns are regular expressions defined on the device and are untrusted text: do not compile them without a match timeout.</remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetAllPluginRegexAsync(CancellationToken cancellationToken = default)
    {
        var raw = await this._channel.CallAsync(Base + "getAllPluginRegex", PluginsJsonContext.Default.DictionaryStringListString, [], cancellationToken).ConfigureAwait(false) ?? [];
        var result = new Dictionary<string, IReadOnlyList<string>>(raw.Count);
        foreach (var (plugin, patterns) in raw)
        {
            if (patterns is null || patterns.Any(static p => p is null))
            {
                throw FacadeResults.Unreadable();
            }

            result[plugin] = patterns;
        }

        return result;
    }

    /// <summary>Gets the URL patterns of the plugins matching a URL (<c>/plugins/getPluginRegex</c>).</summary>
    /// <param name="url">The URL to look up.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The regular expressions; empty when the device returns no data.</returns>
    /// <remarks>The URL is sent to the device as is. The patterns are regular expressions defined on the device and are untrusted text: do not compile them without a match timeout.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "The device matches arbitrary link text, which is not necessarily a valid absolute URI.")]
    public Task<IReadOnlyList<string>> GetPluginRegexAsync(string url, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(url, nameof(url), FacadeGuard.MaxLongTextLength);
        var args = new[] { JsonSerializer.SerializeToElement(url, FacadeJsonContext.Default.String) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "getPluginRegex", PluginsJsonContext.Default.ListString, args, cancellationToken));
    }

    private static JsonElement[] IdentifierArgs(string interfaceName, string displayName, string key)
    {
        FacadeGuard.ValidateText(interfaceName, nameof(interfaceName), FacadeGuard.MaxIdentifierLength);
        FacadeGuard.ValidateText(displayName, nameof(displayName), FacadeGuard.MaxIdentifierLength);
        FacadeGuard.ValidateText(key, nameof(key), FacadeGuard.MaxIdentifierLength);
        return
        [
            JsonSerializer.SerializeToElement(interfaceName, FacadeJsonContext.Default.String),
            JsonSerializer.SerializeToElement(displayName, FacadeJsonContext.Default.String),
            JsonSerializer.SerializeToElement(key, FacadeJsonContext.Default.String),
        ];
    }

    private static async Task<JsonElement> ToValueAsync(Task<JsonElement?> call)
        => await call.ConfigureAwait(false) ?? _nullElement;
}
