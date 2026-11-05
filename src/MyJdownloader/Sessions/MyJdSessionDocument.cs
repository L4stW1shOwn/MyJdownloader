namespace MyJdownloader.Sessions;

/// <summary>Serialization shape of a <see cref="MyJdSession"/>; property order defines the JSON order.</summary>
internal sealed class MyJdSessionDocument
{
    public int Version { get; set; }

    public string? Email { get; set; }

    public byte[]? LoginSecret { get; set; }

    public byte[]? DeviceSecret { get; set; }

    public string? SessionToken { get; set; }

    public string? RegainToken { get; set; }

    public byte[]? ServerEncryptionToken { get; set; }

    public byte[]? DeviceEncryptionToken { get; set; }
}
