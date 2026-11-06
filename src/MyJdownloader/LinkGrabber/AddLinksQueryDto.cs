namespace MyJdownloader.LinkGrabber;

using System.Text.Json.Serialization;

/// <summary>Wire form of <see cref="AddLinksRequest"/>: links joined with line feeds, other members copied.</summary>
/// <remarks>A class rather than a record so that it has no generated <c>ToString</c> exposing the passwords.</remarks>
internal sealed class AddLinksQueryDto
{
    public string? Links { get; init; }

    public string? PackageName { get; init; }

    public string? DestinationFolder { get; init; }

    public string? ExtractPassword { get; init; }

    public string? DownloadPassword { get; init; }

    public string? SourceUrl { get; init; }

    public Priority? Priority { get; init; }

    public bool? Autostart { get; init; }

    public bool? AutoExtract { get; init; }

    public bool? DeepDecrypt { get; init; }

    public bool? OverwritePackagizerRules { get; init; }

    [JsonPropertyName("assignJobID")]
    public bool? AssignJobId { get; init; }

    public static AddLinksQueryDto From(AddLinksRequest request, string[] links)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(links);
        return new AddLinksQueryDto
        {
            Links = string.Join('\n', links),
            PackageName = request.PackageName,
            DestinationFolder = request.DestinationFolder,
            ExtractPassword = request.ExtractPassword,
            DownloadPassword = request.DownloadPassword,
            SourceUrl = request.SourceUrl,
            Priority = request.Priority,
            Autostart = request.Autostart,
            AutoExtract = request.AutoExtract,
            DeepDecrypt = request.DeepDecrypt,
            OverwritePackagizerRules = request.OverwritePackagizerRules,
            AssignJobId = request.AssignJobId,
        };
    }
}
