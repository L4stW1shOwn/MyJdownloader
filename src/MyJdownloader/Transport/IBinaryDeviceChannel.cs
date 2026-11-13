namespace MyJdownloader.Transport;

using System.Text.Json;
using MyJdownloader.Errors;

/// <summary>
/// Device channel whose successful answer is a raw PNG image instead of an encrypted JSON envelope (ADR-011).
/// </summary>
internal interface IBinaryDeviceChannel
{
    /// <summary>Calls a device endpoint that answers with an unencrypted PNG.</summary>
    /// <param name="path">The endpoint path, for example <c>/contentV2/getIcon</c>.</param>
    /// <param name="args">The call arguments.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The PNG bytes, signature included.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> or an argument is not valid.</exception>
    /// <exception cref="MyJdProtocolException">The answer is not a PNG within <see cref="MyJDownloaderOptions.MaxResponseBodyLength"/>.</exception>
    public Task<byte[]> CallBinaryAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default);
}
