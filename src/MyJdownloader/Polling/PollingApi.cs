namespace MyJdownloader.Polling;

using System.Buffers;
using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/polling</c> namespace of one device (state snapshot events).</summary>
/// <remarks>All content returned by the device is untrusted.</remarks>
public sealed class PollingApi
{
    private const string Base = "/polling/";

    private readonly IDeviceChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="PollingApi"/> class over a device channel.</summary>
    /// <remarks>
    /// <see cref="JDownloaderDevice"/> creates the facade for you; this constructor lets you build one over your own
    /// <see cref="IDeviceChannel"/>, for example a test double.
    /// </remarks>
    /// <param name="channel">The channel the device actions are sent through.</param>
    /// <exception cref="ArgumentNullException"><paramref name="channel"/> is <see langword="null"/>.</exception>
    public PollingApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        _channel = channel;
    }

    /// <summary>Polls the requested state events of the device (<c>/polling/poll</c>).</summary>
    /// <param name="query">The events to request; <see langword="null"/> requests all of them.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>One entry per requested event, in the order downloadProgress, jdState, linkGrabberState, captchasWaiting, aggregatedNumbers; an empty list when the device returns none.</returns>
    /// <remarks>The event data comes from the device and is untrusted.</remarks>
    /// <exception cref="ArgumentException">No event is enabled in <paramref name="query"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in a filter of <paramref name="query"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<PollingResult>> PollAsync(PollingQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new PollingQuery();
        if (!(query.DownloadProgress || query.JdState || query.LinkGrabberState || query.CaptchasWaiting || query.AggregatedNumbers))
        {
            throw new ArgumentException("At least one event must be enabled.", nameof(query));
        }

        var packageUuids = FacadeGuard.OptionalIds(query.DownloadProgressPackageUuids, nameof(query));
        var args = new[] { BuildArgument(query with { DownloadProgressPackageUuids = packageUuids }) };
        return FacadeResults.ListAsync(_channel.CallAsync(Base + "poll", PollingJsonContext.Default.ListPollingResult, args, cancellationToken));
    }

    private static JsonElement BuildArgument(PollingQuery query)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            if (query.DownloadProgress)
            {
                writer.WritePropertyName("downloadProgress");
                if (query.DownloadProgressPackageUuids is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStartArray();
                    foreach (var uuid in query.DownloadProgressPackageUuids)
                    {
                        writer.WriteNumberValue(uuid);
                    }

                    writer.WriteEndArray();
                }
            }

            WriteFlag(writer, "jdState", query.JdState);
            WriteFlag(writer, "linkGrabberState", query.LinkGrabberState);
            WriteFlag(writer, "captchasWaiting", query.CaptchasWaiting);
            WriteFlag(writer, "aggregatedNumbers", query.AggregatedNumbers);
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(buffer.WrittenMemory);
        return document.RootElement.Clone();
    }

    private static void WriteFlag(Utf8JsonWriter writer, string name, bool enabled)
    {
        if (enabled)
        {
            writer.WriteBoolean(name, true);
        }
    }
}
