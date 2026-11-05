namespace MyJdownloader;

using System.Diagnostics;
using MyJdownloader.Crypto;

/// <summary>
/// An immutable MyJDownloader session.
/// </summary>
/// <remarks>
/// The session holds keys derived from the account password. It is immutable. The tokens and any
/// exported JSON are managed strings that cannot be cleared, so secrets stay in memory until garbage
/// collection: store or export the session protected. <see cref="ToString"/> never reveals a secret.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MyJdSession
{
    private const int MaxTokenLength = 128;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] loginSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] deviceSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] serverEncryptionToken;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] deviceEncryptionToken;

    internal MyJdSession(
        string email,
        byte[] loginSecret,
        byte[] deviceSecret,
        string sessionToken,
        string regainToken,
        byte[] serverEncryptionToken,
        byte[] deviceEncryptionToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(regainToken);
        ValidateToken(sessionToken, nameof(sessionToken));
        ValidateToken(regainToken, nameof(regainToken));

        this.Email = email;
        this.loginSecret = CopyKey(loginSecret, nameof(loginSecret));
        this.deviceSecret = CopyKey(deviceSecret, nameof(deviceSecret));
        this.SessionToken = sessionToken;
        this.RegainToken = regainToken;
        this.serverEncryptionToken = CopyKey(serverEncryptionToken, nameof(serverEncryptionToken));
        this.deviceEncryptionToken = CopyKey(deviceEncryptionToken, nameof(deviceEncryptionToken));
    }

    /// <summary>Gets the account e-mail address.</summary>
    public string Email { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> LoginSecret => this.loginSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> DeviceSecret => this.deviceSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> ServerEncryptionToken => this.serverEncryptionToken;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> DeviceEncryptionToken => this.deviceEncryptionToken;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string SessionToken { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string RegainToken { get; }

    /// <summary>Returns a representation of the session with a masked e-mail and no secret.</summary>
    /// <returns>A string such as <c>MyJdSession { Email = u***@example.com }</c>.</returns>
    public override string ToString() => $"MyJdSession {{ Email = {MaskEmail(this.Email)} }}";

    private static byte[] CopyKey(byte[] key, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(key, parameterName);
        if (key.Length != MyJdCrypto.KeyLength)
        {
            throw new ArgumentException($"The key must be {MyJdCrypto.KeyLength} bytes long.", parameterName);
        }

        return (byte[])key.Clone();
    }

    private static void ValidateToken(string token, string parameterName)
    {
        if (token.Length > MaxTokenLength)
        {
            throw new ArgumentException("The token is too long.", parameterName);
        }

        foreach (var c in token)
        {
            var isHex = c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
            if (!isHex)
            {
                throw new ArgumentException("The token must be hexadecimal.", parameterName);
            }
        }
    }

    private static string MaskEmail(string email)
    {
        var at = email.LastIndexOf('@');
        if (at < 0)
        {
            return "***";
        }

        var domain = email[(at + 1)..];
        return at == 0 ? $"***@{domain}" : $"{email[0]}***@{domain}";
    }
}
