namespace MyJdownloader.LinkGrabber;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>Parameters of <c>/linkgrabberv2/addLinks</c>; null members are not sent.</summary>
/// <remarks>
/// Do not destructure or serialize this record into logs. <see cref="object.ToString"/> masks the passwords as
/// <c>***</c>, hides <see cref="SourceUrl"/> and shows only the number of links, but links and URLs may embed credentials.
/// </remarks>
public sealed record AddLinksRequest
{
    /// <summary>Gets the links to add; they are sent as one string joined with line feeds.</summary>
    public required IReadOnlyList<string> Links { get; init; }

    /// <summary>Gets the name of the package to create.</summary>
    public string? PackageName { get; init; }

    /// <summary>Gets the destination folder on the device.</summary>
    public string? DestinationFolder { get; init; }

    /// <summary>Gets the archive extraction password.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? ExtractPassword { get; init; }

    /// <summary>Gets the download password.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? DownloadPassword { get; init; }

    /// <summary>Gets the URL of the page the links come from.</summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Raw API value forwarded as is.")]
    public string? SourceUrl { get; init; }

    /// <summary>Gets the priority of the added links; <c>Unknown</c> is rejected.</summary>
    public Priority? Priority { get; init; }

    /// <summary>Gets a value indicating whether the downloads start automatically.</summary>
    public bool? Autostart { get; init; }

    /// <summary>Gets a value indicating whether archives are extracted automatically.</summary>
    public bool? AutoExtract { get; init; }

    /// <summary>Gets a value indicating whether deep link decryption is used.</summary>
    public bool? DeepDecrypt { get; init; }

    /// <summary>Gets a value indicating whether packagizer rules are overwritten.</summary>
    public bool? OverwritePackagizerRules { get; init; }

    /// <summary>Gets a value indicating whether the device returns a job identifier.</summary>
    public bool? AssignJobId { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Links = [").Append(Links?.Count ?? 0).Append(" item(s)]")
            .Append(", PackageName = ").Append(PackageName)
            .Append(", DestinationFolder = ").Append(DestinationFolder)
            .Append(", ExtractPassword = ").Append(ExtractPassword is null ? null : "***")
            .Append(", DownloadPassword = ").Append(DownloadPassword is null ? null : "***")
            .Append(", Priority = ").Append(Priority)
            .Append(", Autostart = ").Append(Autostart)
            .Append(", AutoExtract = ").Append(AutoExtract)
            .Append(", DeepDecrypt = ").Append(DeepDecrypt)
            .Append(", OverwritePackagizerRules = ").Append(OverwritePackagizerRules)
            .Append(", AssignJobId = ").Append(AssignJobId);
        return true;
    }
}
