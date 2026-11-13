namespace MyJdownloader.Toolbar;

using System.Text.Json.Serialization;

/// <summary>The state of the device toolbar, as returned by <c>/toolbar/getStatus</c>.</summary>
/// <remarks>All content returned by the device is untrusted. A member absent from the response keeps its default value.</remarks>
public sealed record ToolbarStatus
{
    /// <summary>Gets the state label of the download watchdog of the device, or <see langword="null"/> when not set. It is a free text of the device: untrusted.</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>Gets a value indicating whether downloads are running.</summary>
    [JsonPropertyName("running")]
    public bool Running { get; init; }

    /// <summary>Gets a value indicating whether the download speed limit is enabled.</summary>
    [JsonPropertyName("limit")]
    public bool SpeedLimitEnabled { get; init; }

    /// <summary>Gets the download speed limit in bytes per second; 0 when the limit is off.</summary>
    [JsonPropertyName("limitspeed")]
    public long SpeedLimit { get; init; }

    /// <summary>Gets a value indicating whether the automatic reconnect is enabled.</summary>
    [JsonPropertyName("reconnect")]
    public bool AutoReconnectEnabled { get; init; }

    /// <summary>Gets a value indicating whether the clipboard is monitored for links.</summary>
    [JsonPropertyName("clipboard")]
    public bool ClipboardMonitored { get; init; }

    /// <summary>Gets a value indicating whether downloads stop after the current download.</summary>
    [JsonPropertyName("stopafter")]
    public bool StopAfterCurrentDownload { get; init; }

    /// <summary>Gets a value indicating whether premium accounts are used.</summary>
    [JsonPropertyName("premium")]
    public bool PremiumEnabled { get; init; }

    /// <summary>Gets the current download speed in bytes per second; 0 when idle.</summary>
    [JsonPropertyName("speed")]
    public long Speed { get; init; }

    /// <summary>Gets a value indicating whether downloads are paused.</summary>
    [JsonPropertyName("pause")]
    public bool Paused { get; init; }

    /// <summary>Gets the number of bytes loaded so far.</summary>
    [JsonPropertyName("download_current")]
    public long BytesLoaded { get; init; }

    /// <summary>Gets the estimated total size in bytes of the enabled links.</summary>
    [JsonPropertyName("download_complete")]
    public long BytesTotal { get; init; }
}
