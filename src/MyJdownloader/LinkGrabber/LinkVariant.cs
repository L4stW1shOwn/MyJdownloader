namespace MyJdownloader.LinkGrabber;

/// <summary>A selectable variant (for example a quality) of a crawled link.</summary>
public sealed record LinkVariant
{
    /// <summary>Gets the variant identifier.</summary>
    public string? Id { get; init; }

    /// <summary>Gets the variant display name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the key of the variant icon.</summary>
    public string? IconKey { get; init; }
}
