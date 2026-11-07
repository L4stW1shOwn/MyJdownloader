namespace MyJdownloader.Sessions;

using System.Diagnostics;

/// <summary>Serialization shape of a <see cref="MyJdSession"/>; property order defines the JSON order.</summary>
internal sealed class MyJdSessionDocument
{
    public int Version { get; set; }

    public string? Email { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public byte[]? LoginSecret { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public byte[]? DeviceSecret { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? SessionToken { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? RegainToken { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public byte[]? ServerEncryptionToken { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public byte[]? DeviceEncryptionToken { get; set; }
}
