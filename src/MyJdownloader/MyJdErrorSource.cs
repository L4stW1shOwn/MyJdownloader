namespace MyJdownloader;

/// <summary>Identifies which side of the MyJDownloader relay reported an error.</summary>
public enum MyJdErrorSource
{
    /// <summary>The error was reported by the MyJDownloader server.</summary>
    Server,

    /// <summary>The error was reported by the target JDownloader device.</summary>
    Device,
}
