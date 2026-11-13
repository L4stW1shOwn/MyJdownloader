namespace MyJdownloader.Plugins;

/// <summary>Selects the fields returned by <c>/plugins/list</c>; both are returned by default.</summary>
public sealed record PluginsQuery
{
    /// <summary>Gets a value indicating whether the URL pattern of each plugin is returned.</summary>
    public bool Pattern { get; init; } = true;

    /// <summary>Gets a value indicating whether the version of each plugin is returned.</summary>
    public bool Version { get; init; } = true;
}
