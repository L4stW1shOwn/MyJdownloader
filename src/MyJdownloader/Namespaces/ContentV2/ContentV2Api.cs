namespace MyJdownloader.ContentV2;

using System.Text.Json;
using MyJdownloader.Errors;
using MyJdownloader.Facades;
using MyJdownloader.Transport;

/// <summary>Typed facade over the <c>/contentV2</c> namespace of one device (icons of hosters, files and icon keys).</summary>
/// <remarks>
/// <para>The icon bytes are untrusted and unauthenticated: the device answers a PNG in clear, outside the encrypted envelope (ADR-011). The client only checks the HTTP status, the <c>image/png</c> content type, the <see cref="MyJDownloaderOptions.MaxResponseBodyLength"/> limit (1 MiB by default) and the PNG signature; decode them in a sandboxed image decoder, never serve the bytes under another content type and never base a security decision on them.</para>
/// <para>The <see cref="IconDescriptor"/> returned by <see cref="GetIconDescriptionAsync"/> is untrusted device data.</para>
/// </remarks>
public sealed class ContentV2Api
{
    private const string Base = "/contentV2/";
    private const int MinIconSize = 1;
    private const int MaxIconSize = 128;

    private readonly IDeviceChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="ContentV2Api"/> class over a device channel.</summary>
    /// <remarks>
    /// <see cref="JDownloaderDevice"/> creates the facade for you; this constructor lets you build one over your own
    /// <see cref="IDeviceChannel"/>, for example a test double.
    /// <see cref="GetFavIconAsync"/>, <see cref="GetFileIconAsync"/> and <see cref="GetIconAsync"/> need the channel provided
    /// by the library, because the device answers a raw PNG (ADR-011); they throw <see cref="NotSupportedException"/> over any
    /// other channel. <see cref="GetIconDescriptionAsync"/> works over any channel.
    /// </remarks>
    /// <param name="channel">The channel the device actions are sent through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channel"/> is <see langword="null"/>.</exception>
    public ContentV2Api(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Gets the favicon of a hoster (<c>/contentV2/getFavIcon</c>).</summary>
    /// <param name="hosterName">The hoster domain, for example <c>rapidgator.net</c>; neither blank nor containing control characters.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The PNG image, signature included. The bytes are untrusted and unauthenticated (ADR-011): never serve them under another content type and never base a security decision on them.</returns>
    /// <remarks>The device answers 404 FILE_NOT_FOUND (<see cref="MyJdApiException"/>) for a hoster without favicon.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="hosterName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="hosterName"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters.</exception>
    /// <exception cref="NotSupportedException">The channel of this facade is not the one provided by the library.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the answer is not a PNG within <see cref="MyJDownloaderOptions.MaxResponseBodyLength"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<byte[]> GetFavIconAsync(string hosterName, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(hosterName, nameof(hosterName), FacadeGuard.MaxIdentifierLength);
        return CallBinaryAsync("getFavIcon", [Str(hosterName)], cancellationToken);
    }

    /// <summary>Gets the icon of a file type, derived from the file name (<c>/contentV2/getFileIcon</c>).</summary>
    /// <param name="fileName">The file name, for example <c>movie.mkv</c>; neither blank nor containing control characters.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The PNG image, signature included. The bytes are untrusted and unauthenticated (ADR-011): never serve them under another content type and never base a security decision on them.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="fileName"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters.</exception>
    /// <exception cref="NotSupportedException">The channel of this facade is not the one provided by the library.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the answer is not a PNG within <see cref="MyJDownloaderOptions.MaxResponseBodyLength"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<byte[]> GetFileIconAsync(string fileName, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(fileName, nameof(fileName), FacadeGuard.MaxIdentifierLength);
        return CallBinaryAsync("getFileIcon", [Str(fileName)], cancellationToken);
    }

    /// <summary>Gets an icon by its key at a given size (<c>/contentV2/getIcon</c>).</summary>
    /// <param name="key">The icon key, for example <c>kc.abc</c>; neither blank nor containing control characters.</param>
    /// <param name="size">The edge of the icon in pixels, from 1 to 128.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The PNG image, signature included. The bytes are untrusted and unauthenticated (ADR-011): never serve them under another content type and never base a security decision on them.</returns>
    /// <remarks>The device rejects an invalid key or size with 400 BAD_PARAMETERS (<see cref="MyJdApiException"/>); this method validates earlier.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not between 1 and 128.</exception>
    /// <exception cref="NotSupportedException">The channel of this facade is not the one provided by the library.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the answer is not a PNG within <see cref="MyJDownloaderOptions.MaxResponseBodyLength"/>.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<byte[]> GetIconAsync(string key, int size, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(key, nameof(key), FacadeGuard.MaxIdentifierLength);
        if (size is < MinIconSize or > MaxIconSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "The size must be between 1 and 128.");
        }

        // The facade context has no Int32 metadata, so the size goes through its Int64 one.
        var sizeArg = JsonSerializer.SerializeToElement(size, FacadeJsonContext.Default.Int64);
        return CallBinaryAsync("getIcon", [Str(key), sizeArg], cancellationToken);
    }

    /// <summary>Gets the description of an icon (<c>/contentV2/getIconDescription</c>).</summary>
    /// <param name="key">The icon key; neither blank nor containing control characters.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The description, or <see langword="null"/> when the device sent nothing.</returns>
    /// <remarks>The <see cref="IconDescriptor"/> is untrusted device data.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IconDescriptor?> GetIconDescriptionAsync(string key, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(key, nameof(key), FacadeGuard.MaxIdentifierLength);
        return _channel.CallAsync(Base + "getIconDescription", ContentV2JsonContext.Default.IconDescriptor, [Str(key)], cancellationToken);
    }

    private static JsonElement Str(string value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.String);

    private Task<byte[]> CallBinaryAsync(string action, JsonElement[] args, CancellationToken cancellationToken)
    {
        if (_channel is not IBinaryDeviceChannel binary)
        {
            throw new NotSupportedException("The channel of this facade is not the one provided by the library, which the icon actions require.");
        }

        return binary.CallBinaryAsync(Base + action, args, cancellationToken);
    }
}
