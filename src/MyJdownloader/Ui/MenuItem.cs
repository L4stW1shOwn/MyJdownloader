namespace MyJdownloader.Ui;

using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>One entry of a device menu, as returned by <c>/ui/getMenu</c>.</summary>
/// <remarks>
/// <para>All content returned by the device is untrusted. The root item returned by the device has no identifier, name or type.</para>
/// <para><see cref="object.ToString"/> includes the device text unfiltered: do not log it as is, filter control characters first.</para>
/// <para><see cref="Children"/> compares by reference, so two items with equal content and equal but distinct non-empty child lists are not equal.</para>
/// </remarks>
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

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Type = ").Append(this.Type)
            .Append(", Id = ").Append(this.Id)
            .Append(", Name = ").Append(this.Name)
            .Append(", Icon = ").Append(this.Icon)
            .Append(", Children = ").Append(this.Children.Count);
        return true;
    }
}
