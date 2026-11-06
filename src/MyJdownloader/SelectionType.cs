namespace MyJdownloader;

/// <summary>Selects which of the given items a cleanup applies to, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum SelectionType
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>Only the given items (<c>SELECTED</c>).</summary>
    Selected,

    /// <summary>Every item except the given ones (<c>UNSELECTED</c>).</summary>
    Unselected,

    /// <summary>All items (<c>ALL</c>).</summary>
    All,

    /// <summary>No item (<c>NONE</c>).</summary>
    None,
}
