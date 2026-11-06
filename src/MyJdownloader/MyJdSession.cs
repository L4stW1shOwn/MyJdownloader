namespace MyJdownloader;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MyJdownloader.Crypto;
using MyJdownloader.Json;
using MyJdownloader.Sessions;

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
    private const int MaxEmailLength = 254;
    private const int MaxJsonLength = 4096;
    private const int CurrentVersion = 1;
    private const string InvalidSessionMessage = "The session data is not a valid MyJdownloader session.";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] _loginSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] _deviceSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] _serverEncryptionToken;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte[] _deviceEncryptionToken;

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
        ValidateEmail(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(regainToken);
        ValidateToken(sessionToken, nameof(sessionToken));
        ValidateToken(regainToken, nameof(regainToken));

        this.Email = email;
        this._loginSecret = CopyKey(loginSecret, nameof(loginSecret));
        this._deviceSecret = CopyKey(deviceSecret, nameof(deviceSecret));
        this.SessionToken = sessionToken;
        this.RegainToken = regainToken;
        this._serverEncryptionToken = CopyKey(serverEncryptionToken, nameof(serverEncryptionToken));
        this._deviceEncryptionToken = CopyKey(deviceEncryptionToken, nameof(deviceEncryptionToken));
    }

    /// <summary>Gets the account e-mail address.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string Email { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> LoginSecret => this._loginSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> DeviceSecret => this._deviceSecret;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> ServerEncryptionToken => this._serverEncryptionToken;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal ReadOnlySpan<byte> DeviceEncryptionToken => this._deviceEncryptionToken;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string SessionToken { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [field: DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string RegainToken { get; }

    /// <summary>Returns a representation of the session with a masked e-mail and no secret.</summary>
    /// <returns>A string such as <c>MyJdSession { Email = u***@example.com }</c>.</returns>
    public override string ToString() => $"MyJdSession {{ Email = {MaskEmail(this.Email)} }}";

    /// <summary>Exports the session as versioned JSON.</summary>
    /// <returns>A JSON document that <see cref="FromJson(string)"/> can restore.</returns>
    /// <remarks>
    /// The string contains keys derived from the account password. Store it protected (DPAPI, a secret
    /// store, encryption). It is a managed string and cannot be cleared from memory.
    /// </remarks>
    public string ToJson()
    {
        var document = new MyJdSessionDocument
        {
            Version = CurrentVersion,
            Email = this.Email,
            LoginSecret = this._loginSecret,
            DeviceSecret = this._deviceSecret,
            SessionToken = this.SessionToken,
            RegainToken = this.RegainToken,
            ServerEncryptionToken = this._serverEncryptionToken,
            DeviceEncryptionToken = this._deviceEncryptionToken,
        };

        return JsonSerializer.Serialize(document, MyJdJsonContext.Default.MyJdSessionDocument);
    }

    /// <summary>Restores a session exported by <see cref="ToJson"/>.</summary>
    /// <param name="json">The JSON document.</param>
    /// <returns>The restored session.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The data is not a valid session; the message never contains a value.</exception>
    public static MyJdSession FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > MaxJsonLength)
        {
            throw new FormatException(InvalidSessionMessage);
        }

        MyJdSessionDocument? document = null;
        try
        {
            document = JsonSerializer.Deserialize(json, MyJdJsonContext.Default.MyJdSessionDocument);
            if (document is null || document.Version != CurrentVersion)
            {
                throw new FormatException(InvalidSessionMessage);
            }

            // Missing fields are rejected by the ctor (ArgumentNullException is an ArgumentException), mapped below.
            return new MyJdSession(
                document.Email!,
                document.LoginSecret!,
                document.DeviceSecret!,
                document.SessionToken!,
                document.RegainToken!,
                document.ServerEncryptionToken!,
                document.DeviceEncryptionToken!);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new FormatException(InvalidSessionMessage);
        }
        finally
        {
            ClearKey(document?.LoginSecret);
            ClearKey(document?.DeviceSecret);
            ClearKey(document?.ServerEncryptionToken);
            ClearKey(document?.DeviceEncryptionToken);
        }
    }

    private static void ClearKey(byte[]? key)
    {
        if (key is not null)
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] CopyKey(byte[] key, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(key, parameterName);
        if (key.Length != MyJdCrypto.KeyLength)
        {
            throw new ArgumentException($"The key must be {MyJdCrypto.KeyLength} bytes long.", parameterName);
        }

        return (byte[])key.Clone();
    }

    private static void ValidateEmail(string email)
    {
        if (email.Length > MaxEmailLength || email.Any(char.IsControl))
        {
            throw new ArgumentException("The e-mail address is not valid.", nameof(email));
        }
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
