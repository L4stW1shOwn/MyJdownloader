namespace MyJdownloader.SystemControl;

using System.Text.Json.Serialization;

/// <summary>System and JVM information of a device, as returned by <c>/system/getSystemInfos</c>.</summary>
/// <remarks>All content returned by the device is untrusted. The startup time is epoch milliseconds on the wire; a value of zero or less is read as <see langword="null"/>.</remarks>
public sealed record SystemInfo
{
    /// <summary>Gets the CPU architecture family (for example <c>X86</c>).</summary>
    public string? ArchFamily { get; init; }

    /// <summary>Gets the CPU architecture as reported by the JVM (for example <c>amd64</c>).</summary>
    public string? ArchString { get; init; }

    /// <summary>Gets the hardware description of the device.</summary>
    public string? Hardware { get; init; }

    /// <summary>Gets the name of the Java virtual machine.</summary>
    public string? JavaName { get; init; }

    /// <summary>Gets the vendor of the Java virtual machine.</summary>
    public string? JavaVendor { get; init; }

    /// <summary>Gets the Java version as text (for example <c>11.0.25+11</c>).</summary>
    public string? JavaVersionString { get; init; }

    /// <summary>Gets the operating system identifier (for example <c>LINUX</c>).</summary>
    public string? OperatingSystem { get; init; }

    /// <summary>Gets the operating system family (for example <c>LINUX</c>).</summary>
    public string? OsFamily { get; init; }

    /// <summary>Gets the operating system name and version as text.</summary>
    public string? OsString { get; init; }

    /// <summary>Gets the committed JVM heap size, in bytes.</summary>
    public long HeapCommitted { get; init; }

    /// <summary>Gets the maximum JVM heap size, in bytes.</summary>
    public long HeapMax { get; init; }

    /// <summary>Gets the used JVM heap size, in bytes.</summary>
    public long HeapUsed { get; init; }

    /// <summary>Gets the encoded Java version (for example <c>11000025000011</c> for 11.0.25+11); prefer <see cref="JavaVersionString"/> for display.</summary>
    public long JavaVersion { get; init; }

    /// <summary>Gets the time JDownloader was started, or <see langword="null"/> when not set.</summary>
    [JsonPropertyName("startupTimeStamp")]
    public DateTimeOffset? StartupTime { get; init; }

    /// <summary>Gets a value indicating whether the CPU architecture is 64-bit.</summary>
    public bool Arch64Bit { get; init; }

    /// <summary>Gets a value indicating whether the device runs inside a Docker container.</summary>
    public bool Docker { get; init; }

    /// <summary>Gets a value indicating whether the JVM runs headless (without a graphical interface).</summary>
    public bool Headless { get; init; }

    /// <summary>Gets a value indicating whether the JVM is 64-bit.</summary>
    public bool Jvm64Bit { get; init; }

    /// <summary>Gets a value indicating whether the operating system is 64-bit.</summary>
    public bool Os64Bit { get; init; }

    /// <summary>Gets a value indicating whether JDownloader runs as a Snap package.</summary>
    public bool Snap { get; init; }
}
