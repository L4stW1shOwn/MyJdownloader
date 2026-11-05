namespace MyJdownloader.Transport;

using System.Diagnostics;
using System.Security.Cryptography;

/// <summary>
/// Single-use carrier of the secrets derived for a <c>/my/connect</c> call, so the password never reaches async code.
/// </summary>
internal sealed class ConnectRequest : IDisposable
{
    private int used;

    public ConnectRequest(string email, byte[] loginSecret, byte[] deviceSecret)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(loginSecret);
        ArgumentNullException.ThrowIfNull(deviceSecret);
        this.Email = email;
        this.LoginSecret = [.. loginSecret];
        this.DeviceSecret = [.. deviceSecret];
    }

    /// <summary>Gets the account email.</summary>
    public string Email { get; }

    /// <summary>Gets the login secret; cleared once the request is consumed.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public byte[] LoginSecret { get; }

    /// <summary>Gets the device secret; cleared once the request is consumed.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public byte[] DeviceSecret { get; }

    /// <summary>Marks the request as used.</summary>
    /// <exception cref="InvalidOperationException">The request has already been used.</exception>
    public void MarkUsed()
    {
        if (Interlocked.Exchange(ref this.used, 1) != 0)
        {
            throw new InvalidOperationException("The connect request has already been used.");
        }
    }

    /// <summary>Marks the request as used and zeroes the secrets; safe to call several times.</summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref this.used, 1);
        CryptographicOperations.ZeroMemory(this.LoginSecret);
        CryptographicOperations.ZeroMemory(this.DeviceSecret);
    }
}
