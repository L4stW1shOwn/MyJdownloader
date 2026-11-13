namespace MyJdownloader.Ui;

/// <summary>Selects the user interface context of a device menu, as used by the JDownloader <c>/ui</c> namespace.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
public enum MenuContext
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>The right-click menu of the download list (<c>DLC</c>). The only context implemented by the device.</summary>
    Dlc,

    /// <summary>The right-click menu of the link grabber (<c>LGC</c>).</summary>
    /// <remarks>The device does not implement it: <c>getMenu</c> answers 404 and <c>invokeAction</c> does nothing.</remarks>
    Lgc,
}
