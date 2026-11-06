namespace MyJdownloader.LinkGrabber;

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

    public bool? AssignJobId { get; init; }

    public static AddLinksQueryDto From(AddLinksRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new AddLinksQueryDto
        {
            Links = string.Join('\n', request.Links),
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
