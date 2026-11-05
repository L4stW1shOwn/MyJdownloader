namespace MyJdownloader;

/// <summary>Configuration options for the MyJDownloader client.</summary>
public sealed class MyJDownloaderOptions
{
    /// <summary>Gets or sets the application identifier sent to the <c>/my/connect</c> endpoint.</summary>
    public string AppKey { get; set; } = "MyJdownloader.NET";

    /// <summary>Gets or sets the base address of the MyJDownloader relay server.</summary>
    public Uri BaseAddress { get; set; } = new("https://api.jdownloader.org");

    /// <summary>Gets or sets the <c>apiVer</c> value used for device calls.</summary>
    public int ApiVersion { get; set; } = 1;
}
