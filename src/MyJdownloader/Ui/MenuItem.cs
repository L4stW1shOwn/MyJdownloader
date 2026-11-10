namespace MyJdownloader.Ui;

/// <summary>One entry of a device menu, as returned by <c>/ui/getMenu</c>.</summary>
/// <remarks>All content returned by the device is untrusted. The root item returned by the device has no identifier, name or type.</remarks>
public sealed record MenuItem
{
    /// <summary>Gets the identifier of the item, to pass to <see cref="UiApi.InvokeActionAsync"/> for an action; <see langword="null"/> when not set.</summary>
    public string? Id { get; init; }

    /// <summary>Gets the display name of the item, or <see langword="null"/> when not set.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the icon key of the item (usable with the ContentV2 icons), or <see langword="null"/> when not set.</summary>
    public string? Icon { get; init; }

    /// <summary>Gets the kind of the item; <see cref="MenuItemType.Unknown"/> when not set or not recognized.</summary>
    public MenuItemType Type { get; init; }

    /// <summary>Gets the child items; empty when the item has none. Never <see langword="null"/>.</summary>
    public IReadOnlyList<MenuItem> Children
    {
        get;
        init => field = value ?? [];
    } = [];
}
