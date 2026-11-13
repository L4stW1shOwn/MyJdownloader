namespace MyJdownloader.Config;

using System.Text.Json;
using MyJdownloader.Facades;
using MyJdownloader.Transport;

/// <summary>Typed facade over the <c>/config</c> namespace of one device (advanced configuration entries).</summary>
/// <remarks>
/// Values are untyped on the device and are exposed as <see cref="JsonElement"/> (ADR-008). They may hold secrets (passwords, tokens): never log them.
/// </remarks>
public sealed class ConfigApi
{
    private const string Base = "/config/";

    private static readonly JsonElement _nullElement = JsonDocument.Parse("null").RootElement.Clone();

    private readonly IDeviceChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="ConfigApi"/> class over a device channel.</summary>
    /// <remarks>
    /// <see cref="JDownloaderDevice"/> creates the facade for you; this constructor lets you build one over your own
    /// <see cref="IDeviceChannel"/>, for example a test double.
    /// </remarks>
    /// <param name="channel">The channel the device actions are sent through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channel"/> is <see langword="null"/>.</exception>
    public ConfigApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Gets the current value of a configuration entry (<c>/config/get</c>).</summary>
    /// <param name="interfaceName">The configuration interface name.</param>
    /// <param name="storage">The storage of the interface, or <see langword="null"/> for the default storage.</param>
    /// <param name="key">The entry key.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The value of the entry; an element of kind <see cref="JsonValueKind.Null"/> when the device returns none.</returns>
    /// <remarks>The value may hold a secret: never log it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An identifier is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<JsonElement> GetAsync(string interfaceName, string? storage, string key, CancellationToken cancellationToken = default)
        => GetValueAsync("get", interfaceName, storage, key, cancellationToken);

    /// <summary>Gets the default value of a configuration entry (<c>/config/getDefault</c>).</summary>
    /// <param name="interfaceName">The configuration interface name.</param>
    /// <param name="storage">The storage of the interface, or <see langword="null"/> for the default storage.</param>
    /// <param name="key">The entry key.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The default value of the entry; an element of kind <see cref="JsonValueKind.Null"/> when the device returns none.</returns>
    /// <remarks>The value may hold a secret: never log it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An identifier is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<JsonElement> GetDefaultAsync(string interfaceName, string? storage, string key, CancellationToken cancellationToken = default)
        => GetValueAsync("getDefault", interfaceName, storage, key, cancellationToken);

    /// <summary>Sets the value of a configuration entry (<c>/config/set</c>).</summary>
    /// <param name="interfaceName">The configuration interface name.</param>
    /// <param name="storage">The storage of the interface, or <see langword="null"/> for the default storage.</param>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The new value.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device accepted the value.</returns>
    /// <remarks>
    /// The change is applied to the device configuration immediately, without confirmation. The value is sent as its JSON text (strings are quoted), see ADR-008;
    /// this wire behaviour is to be confirmed by live tests. The value may hold a secret: never log it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An identifier is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates, or <paramref name="value"/> is an undefined (default) element or is longer than 1 048 576 characters of JSON text.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SetAsync(string interfaceName, string? storage, string key, JsonElement value, CancellationToken cancellationToken = default)
    {
        var args = IdentifierArgs(interfaceName, storage, key);
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
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "set", FacadeJsonContext.Default.NullableBoolean, [.. args, valueArg], cancellationToken));
    }

    /// <summary>Resets a configuration entry to its default value (<c>/config/reset</c>).</summary>
    /// <param name="interfaceName">The configuration interface name.</param>
    /// <param name="storage">The storage of the interface, or <see langword="null"/> for the default storage.</param>
    /// <param name="key">The entry key.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device reset the entry.</returns>
    /// <remarks>The change is applied to the device configuration immediately, without confirmation.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceName"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An identifier is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> ResetAsync(string interfaceName, string? storage, string key, CancellationToken cancellationToken = default)
    {
        var args = IdentifierArgs(interfaceName, storage, key);
        return FacadeResults.RequireAsync(_channel.CallAsync(Base + "reset", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Lists configuration entries (<c>/config/query</c>).</summary>
    /// <param name="query">The entries and fields to return, or <see langword="null"/> for all entries without their current values.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The entries; empty when the device returns no data.</returns>
    /// <remarks>
    /// Current values are not requested unless <see cref="ConfigQuery.Values"/> is <see langword="true"/>; they may hold secrets, never log whole entries.
    /// <see cref="ConfigQuery.Pattern"/> is a regular expression evaluated on the device.
    /// The encrypted response body is limited by <see cref="MyJDownloaderOptions.MaxResponseBodyLength"/> (1 MiB by default, about 768 KiB of JSON): on a large configuration, restrict the query with <see cref="ConfigQuery.ConfigInterface"/> or <see cref="ConfigQuery.Pattern"/>,
    /// otherwise the call fails with a <see cref="MyJdProtocolException"/>.
    /// </remarks>
    /// <exception cref="ArgumentException"><see cref="ConfigQuery.ConfigInterface"/> or <see cref="ConfigQuery.Pattern"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<ConfigEntry>> QueryAsync(ConfigQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new ConfigQuery();
        FacadeGuard.ValidateOptionalText(query.ConfigInterface, nameof(query), FacadeGuard.MaxIdentifierLength);
        FacadeGuard.ValidateOptionalText(query.Pattern, nameof(query), FacadeGuard.MaxLongTextLength);
        var args = new[] { JsonSerializer.SerializeToElement(query, ConfigJsonContext.Default.ConfigQuery) };
        return FacadeResults.ListAsync(_channel.CallAsync(Base + "query", ConfigJsonContext.Default.ListConfigEntry, args, cancellationToken));
    }

    /// <summary>Lists the options of an enumeration type (<c>/config/listEnum</c>).</summary>
    /// <param name="type">The Java type name of the enumeration, as reported in <see cref="ConfigEntry.Type"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The options; empty when the device returns no data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<ConfigEnumOption>> ListEnumAsync(string type, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(type, nameof(type), FacadeGuard.MaxIdentifierLength);
        var args = new[] { JsonSerializer.SerializeToElement(type, FacadeJsonContext.Default.String) };
        return FacadeResults.ListAsync(_channel.CallAsync(Base + "listEnum", ConfigJsonContext.Default.ListConfigEnumOption, args, cancellationToken));
    }

    private static JsonElement[] IdentifierArgs(string interfaceName, string? storage, string key)
    {
        FacadeGuard.ValidateText(interfaceName, nameof(interfaceName), FacadeGuard.MaxIdentifierLength);
        FacadeGuard.ValidateOptionalText(storage, nameof(storage), FacadeGuard.MaxPathLength);
        FacadeGuard.ValidateText(key, nameof(key), FacadeGuard.MaxIdentifierLength);
        return
        [
            JsonSerializer.SerializeToElement(interfaceName, FacadeJsonContext.Default.String),
            storage is null ? _nullElement : JsonSerializer.SerializeToElement(storage, FacadeJsonContext.Default.String),
            JsonSerializer.SerializeToElement(key, FacadeJsonContext.Default.String),
        ];
    }

    private static async Task<JsonElement> ToValueAsync(Task<JsonElement?> call)
        => await call.ConfigureAwait(false) ?? _nullElement;

    private Task<JsonElement> GetValueAsync(string action, string interfaceName, string? storage, string key, CancellationToken cancellationToken)
    {
        var args = IdentifierArgs(interfaceName, storage, key);
        return ToValueAsync(_channel.CallAsync(Base + action, ConfigJsonContext.Default.NullableJsonElement, args, cancellationToken));
    }
}
