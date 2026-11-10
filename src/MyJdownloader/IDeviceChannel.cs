namespace MyJdownloader;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

/// <summary>An encrypted channel to one device, reached through the MyJDownloader relay.</summary>
/// <remarks>
/// <para>
/// A call is sent twice only after a plain relay <c>TOKEN_INVALID</c> answer (the session is renewed, then the call is replayed once).
/// On <see cref="TimeoutException"/> or <see cref="HttpRequestException"/> the action may already have been executed by the device:
/// the call is never retried by the library, so the caller must decide whether replaying it is safe.
/// </para>
/// <para>
/// The total duration of a call may reach about three times <see cref="HttpClient.Timeout"/> (call, renewal, replay).
/// </para>
/// <para>
/// The session token and the device id travel in the URL path and are not redacted by <see cref="HttpClient"/> logging or tracing:
/// do not log full request URIs.
/// </para>
/// <para>
/// Each entry of <c>args</c> is sent as one string parameter: a JSON string contributes its text, any other value its raw JSON text.
/// The arguments are copied before the method returns, so their <see cref="JsonDocument"/> may be disposed right after the call.
/// </para>
/// </remarks>
public interface IDeviceChannel
{
    /// <summary>Calls a device action and deserializes its <c>data</c> payload.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="path">The action path, for example <c>/jd/version</c>: segments made of ASCII letters, digits and underscore, at most 256 characters in total.</param>
    /// <param name="responseType">The metadata used to deserialize <c>data</c>.</param>
    /// <param name="args">The action arguments.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The deserialized <c>data</c>, or <see langword="default"/> when it is absent or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">An argument is invalid or the arguments exceed 1 048 576 characters in total (thrown synchronously).</exception>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/> (thrown synchronously).</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    /// <exception cref="IOException">The response body could not be read; the action may have been executed.</exception>
    /// <remarks>An <see cref="InvalidOperationException"/> raised by a misconfigured <paramref name="responseType"/> is reported as a <see cref="MyJdProtocolException"/> (device unreadable).</remarks>
    [SuppressMessage("ApiDesign", "RS0026", Justification = "The overloads cannot be ambiguous: they differ by the mandatory responseType parameter")]
    public Task<T?> CallAsync<T>(string path, JsonTypeInfo<T> responseType, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default);

    /// <summary>Calls a device action and ignores its <c>data</c> payload.</summary>
    /// <param name="path">The action path, for example <c>/jd/version</c>: segments made of ASCII letters, digits and underscore, at most 256 characters in total.</param>
    /// <param name="args">The action arguments.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentException">An argument is invalid or the arguments exceed 1 048 576 characters in total (thrown synchronously).</exception>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/> (thrown synchronously).</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    /// <exception cref="IOException">The response body could not be read; the action may have been executed.</exception>
    [SuppressMessage("ApiDesign", "RS0026", Justification = "The overloads cannot be ambiguous: they differ by the mandatory responseType parameter")]
    public Task CallAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken = default);
}
