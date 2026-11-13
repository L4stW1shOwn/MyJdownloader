namespace MyJdownloader.Ui;

/// <summary>Kind of a <see cref="MenuItem"/>, as used by the JDownloader <c>/ui</c> namespace.</summary>
/// <remarks>Unrecognized values are read as <see cref="Unknown"/>.</remarks>
public enum MenuItemType
{
    /// <summary>The value is missing or not recognized by this library (the root item has no type).</summary>
    Unknown = 0,

    /// <summary>A container of other items (<c>CONTAINER</c>).</summary>
    Container,

    /// <summary>An action that the device can run with <c>invokeAction</c> (<c>ACTION</c>).</summary>
    Action,

    /// <summary>A hint for the client user interface that does nothing on the device (<c>LINK</c>).</summary>
    Link,
}
