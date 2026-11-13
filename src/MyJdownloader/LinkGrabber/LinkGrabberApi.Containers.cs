namespace MyJdownloader.LinkGrabber;

using System.Text.Json;
using MyJdownloader.Facades;

public sealed partial class LinkGrabberApi
{
    internal const int MaxContainerSize = 524_288;

    private const string ContainerDataUrlPrefix = "data:application/octet-stream;base64,";

    /// <summary>Adds the links of a container file to the link grabber (<c>/linkgrabberv2/addContainer</c>).</summary>
    /// <param name="type">The format of the container.</param>
    /// <param name="content">The raw bytes of the container: not empty and at most 524288 bytes.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The link collecting job, or <see langword="null"/> when the device could not store the container.</returns>
    /// <remarks>
    /// The bytes are sent as is and parsed by the device. A container may hold credentials (links of a DLC or CCF, servers of an NZB): never log it.
    /// The links are added without confirmation. A corrupt container still starts a job, which yields no links.
    /// The links are collected under other link collecting jobs: do not use the returned job identifier to find them (live-confirmed).
    /// Only send containers from a trusted source: a crawljob can set the destination folder, extraction passwords and autostart on the device.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="type"/> is <see cref="ContainerType.Unknown"/> or not a defined value, or <paramref name="content"/> is empty or larger than 524288 bytes.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<LinkCollectingJob?> AddContainerAsync(ContainerType type, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        var extension = type switch
        {
            ContainerType.Dlc => "dlc",
            ContainerType.Ccf => "ccf",
            ContainerType.Rsdf => "rsdf",
            ContainerType.Nzb => "nzb",
            ContainerType.CrawlJob => "crawljob",
            ContainerType.Sft => "sft",
            ContainerType.Sfdl => "sfdl",
            ContainerType.Metalink => "metalink",
            ContainerType.Meta4 => "meta4",
            ContainerType.Unknown or _ => throw new ArgumentException("The container type must be a known value.", nameof(type)),
        };

        if (content.IsEmpty || content.Length > MaxContainerSize)
        {
            throw new ArgumentException($"The content must not be empty and must not exceed {MaxContainerSize} bytes.", nameof(content));
        }

        var args = new[]
        {
            JsonSerializer.SerializeToElement(extension, FacadeJsonContext.Default.String),
            JsonSerializer.SerializeToElement(ContainerDataUrlPrefix + Convert.ToBase64String(content.Span), FacadeJsonContext.Default.String),
        };
        return _channel.CallAsync(Base + "addContainer", LinkGrabberJsonContext.Default.LinkCollectingJob, args, cancellationToken);
    }
}
