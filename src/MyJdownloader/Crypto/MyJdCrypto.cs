namespace MyJdownloader.Crypto;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Key derivation and request signing primitives of the native MyJDownloader protocol.
/// </summary>
/// <remarks>
/// Intermediate buffers holding secret material are cleared as soon as they are hashed.
/// The caller's <c>password</c> string is immutable and therefore cannot be cleared.
/// Likewise, the string returned by <c>Decrypt</c> and the lowercased email string are managed strings and cannot be cleared.
/// </remarks>
internal static class MyJdCrypto
{
    /// <summary>Length in bytes of every secret, token and key (SHA-256 output).</summary>
    public const int KeyLength = 32;

    // Each half of a key: the IV (first) and the AES-128 key (second).
    private const int HalfKeyLength = KeyLength / 2;

    // AES block size in bytes: also the CBC IV length and the ciphertext length granularity.
    private const int AesBlockSize = 16;

    /// <summary>Derives the login secret: <c>SHA256(lower(email) + password + "server")</c>.</summary>
    /// <param name="email">Account email; lowercased before hashing.</param>
    /// <param name="password">Account password.</param>
    /// <returns>The 32-byte login secret.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An argument is empty.</exception>
    public static byte[] DeriveLoginSecret(string email, string password)
        => DeriveSecret(email, password, "server");

    /// <summary>Derives the device secret: <c>SHA256(lower(email) + password + "device")</c>.</summary>
    /// <param name="email">Account email; lowercased before hashing.</param>
    /// <param name="password">Account password.</param>
    /// <returns>The 32-byte device secret.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">An argument is empty.</exception>
    public static byte[] DeriveDeviceSecret(string email, string password)
        => DeriveSecret(email, password, "device");

    /// <summary>Derives a token: <c>SHA256(baseKey ‖ fromHex(sessionToken))</c>.</summary>
    /// <param name="baseKey">The 32-byte base key; left unchanged.</param>
    /// <param name="sessionToken">The hexadecimal session token.</param>
    /// <returns>The 32-byte token.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The key is not <see cref="KeyLength"/> bytes long, or the token is empty.</exception>
    /// <exception cref="FormatException">The session token is not valid hexadecimal.</exception>
    public static byte[] DeriveToken(byte[] baseKey, string sessionToken)
    {
        ArgumentNullException.ThrowIfNull(baseKey);
        ArgumentException.ThrowIfNullOrEmpty(sessionToken);
        EnsureKeyLength(baseKey);

        var buffer = new byte[KeyLength + ((sessionToken.Length + 1) / 2)];
        try
        {
            baseKey.CopyTo(buffer, 0);
            var status = Convert.FromHexString(sessionToken, buffer.AsSpan(KeyLength), out _, out _);
            if (status != OperationStatus.Done)
            {
                throw new FormatException("The session token is not a valid hexadecimal string.");
            }

            return SHA256.HashData(buffer);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    /// <summary>Signs <paramref name="data"/> with HMAC-SHA256.</summary>
    /// <param name="key">The 32-byte signing key.</param>
    /// <param name="data">The text to sign, encoded as UTF-8.</param>
    /// <returns>The signature as lowercase hexadecimal.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The key is not <see cref="KeyLength"/> bytes long.</exception>
    public static string Sign(byte[] key, string data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(data);
        EnsureKeyLength(key);

        return Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data)));
    }

    /// <summary>Encrypts <paramref name="plaintext"/> with AES-128-CBC (PKCS7).</summary>
    /// <param name="key">The 32-byte key: first half is the IV, second half the AES key; left unchanged.</param>
    /// <param name="plaintext">The text to encrypt, encoded as UTF-8.</param>
    /// <returns>The ciphertext as Base64.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The key is not <see cref="KeyLength"/> bytes long.</exception>
    public static string Encrypt(byte[] key, string plaintext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(plaintext);
        EnsureKeyLength(key);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        try
        {
            return Encrypt(key, plainBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    /// <summary>Encrypts UTF-8 <paramref name="plaintext"/> bytes with AES-128-CBC (PKCS7).</summary>
    /// <param name="key">The 32-byte key: first half is the IV, second half the AES key; left unchanged.</param>
    /// <param name="plaintext">The UTF-8 bytes to encrypt; left unchanged, the caller clears them.</param>
    /// <returns>The ciphertext as Base64.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The key is not <see cref="KeyLength"/> bytes long.</exception>
    public static string Encrypt(byte[] key, ReadOnlySpan<byte> plaintext)
    {
        ArgumentNullException.ThrowIfNull(key);
        EnsureKeyLength(key);

        using var aes = Aes.Create();
        aes.SetKey(key.AsSpan(HalfKeyLength, HalfKeyLength));
        return Convert.ToBase64String(aes.EncryptCbc(plaintext, key.AsSpan(0, AesBlockSize), PaddingMode.PKCS7));
    }

    /// <summary>Decrypts a Base64 AES-128-CBC (PKCS7) ciphertext.</summary>
    /// <param name="key">The 32-byte key: first half is the IV, second half the AES key; left unchanged.</param>
    /// <param name="ciphertext">The Base64 ciphertext.</param>
    /// <returns>The UTF-8 decoded plaintext.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The key is not <see cref="KeyLength"/> bytes long.</exception>
    /// <exception cref="FormatException">The ciphertext is not valid Base64.</exception>
    /// <exception cref="CryptographicException">The ciphertext length or padding is invalid.</exception>
    public static string Decrypt(byte[] key, string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(ciphertext);
        EnsureKeyLength(key);

        var cipherBytes = Convert.FromBase64String(ciphertext);
        if (cipherBytes.Length == 0 || cipherBytes.Length % AesBlockSize != 0)
        {
            throw new CryptographicException("The ciphertext length is invalid.");
        }

        byte[]? plainBytes = null;
        try
        {
            using var aes = Aes.Create();
            aes.SetKey(key.AsSpan(HalfKeyLength, HalfKeyLength));
            plainBytes = aes.DecryptCbc(cipherBytes, key.AsSpan(0, AesBlockSize), PaddingMode.PKCS7);
            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            if (plainBytes is not null)
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
    }

    // Shared with Encrypt/Decrypt. Never includes the key value in the message.
    private static void EnsureKeyLength(byte[] key, [CallerArgumentExpression(nameof(key))] string? paramName = null)
    {
        if (key.Length != KeyLength)
        {
            throw new ArgumentException($"The key must be exactly {KeyLength} bytes long.", paramName);
        }
    }

    [SuppressMessage("Globalization", "CA1308", Justification = "MyJD protocol requires the lowercase email")]
    private static byte[] DeriveSecret(string email, string password, string domain)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        ArgumentException.ThrowIfNullOrEmpty(password);

        var lowerEmail = email.ToLowerInvariant();
        var buffer = new byte[Encoding.UTF8.GetByteCount(lowerEmail) + Encoding.UTF8.GetByteCount(password) + Encoding.UTF8.GetByteCount(domain)];
        try
        {
            var offset = Encoding.UTF8.GetBytes(lowerEmail, buffer);
            offset += Encoding.UTF8.GetBytes(password, buffer.AsSpan(offset));
            Encoding.UTF8.GetBytes(domain, buffer.AsSpan(offset));

            return SHA256.HashData(buffer);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }
}
