namespace MyJdownloader.Tests.Crypto;

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using MyJdownloader.Crypto;

[TestFixture]
public sealed class MyJdCryptoTests
{
    private const string Email = "User.Name@Example.COM";
    private const string Password = "pässwörd€";
    private const string SessionTokenConnect = "00112233445566778899aabbccddeeff00112233445566778899aabbccddeeff";
    private const string SessionTokenReconnect = "ffeeddccbbaa99887766554433221100ffeeddccbbaa99887766554433221100";
    private const string LoginSecret = "c8c796d5d87190f50c13eec5cb882a00c271ca9fabf3a89d9e8731546d5b36f7";
    private const string DeviceSecret = "b5308a5a4c46fa83cc741aa06fd0e56949b72515317e87d8184cfb4b02d7f047";
    private const string ServerTokenConnect = "386965725d8b155501c7cc8736c20e1eb62c1ffa39925751476a8fcd07b8ecd0";
    private const string ServerTokenReconnect = "b6652756bd20866592e2cfd0c91eccffaac487563bdde82d05d041068ef121aa";
    private const string DeviceToken = "2a28bcf3fbac0688ebd99edb81ca0da2def9dd23f800f4486fd36182405fe9be";
    private const string SignData = "/my/connect?email=user.name%40example.com&appkey=MyJdownloader.NET&rid=1730800000000";
    private const string Signature = "8bfcb74ca9b3e1a2a2aaa6e38227a28c304631ceb820e09386398434a54bbff5";

    private const string CipherEmpty = "05WQXgCsic4gkXP8rtxeOA==";
    private const string CipherBlock = "10d+8YkBu/8KZze0cwPlcbkmxuov4ELxV35nhhgi9Uo=";
    private const string CipherJson = "8rnBAvI+d0efcV7rTAQjCaS526J5WgXk5MULFMmLfQFV071hBTnsEBW4G2xTDTZ9";
    private const string CipherBadPadding = "q/tiO6asVsqr6GhkRMwarw==";
#pragma warning disable JSON002 // Probable JSON string: intentional protocol payload used as a plain string vector
    private const string JsonPlaintext = "{\"data\":\"élève ✓\",\"rid\":1730800000000}";
#pragma warning restore JSON002

    private static byte[] FromHex(string hex) => Convert.FromHexString(hex);

    [Test]
    public void DeriveLoginSecret_ReferenceInput_MatchesVector()
        => Assert.That(MyJdCrypto.DeriveLoginSecret(Email, Password), Is.EqualTo(FromHex(LoginSecret)));

    [Test]
    [SuppressMessage("Globalization", "CA1308", Justification = "MyJD protocol requires the lowercase email")]
    public void DeriveLoginSecret_LowercasedEmail_MatchesVector()
        => Assert.That(MyJdCrypto.DeriveLoginSecret(Email.ToLowerInvariant(), Password), Is.EqualTo(FromHex(LoginSecret)));

    [Test]
    public void DeriveDeviceSecret_ReferenceInput_MatchesVector()
        => Assert.That(MyJdCrypto.DeriveDeviceSecret(Email, Password), Is.EqualTo(FromHex(DeviceSecret)));

    [Test]
    public void DeriveToken_ConnectChain_MatchesServerTokenConnect()
        => Assert.That(MyJdCrypto.DeriveToken(FromHex(LoginSecret), SessionTokenConnect), Is.EqualTo(FromHex(ServerTokenConnect)));

    [Test]
    public void DeriveToken_ReconnectChain_MatchesServerTokenReconnect()
        => Assert.That(MyJdCrypto.DeriveToken(FromHex(ServerTokenConnect), SessionTokenReconnect), Is.EqualTo(FromHex(ServerTokenReconnect)));

    [Test]
    public void DeriveToken_DeviceSecret_MatchesDeviceToken()
        => Assert.That(MyJdCrypto.DeriveToken(FromHex(DeviceSecret), SessionTokenReconnect), Is.EqualTo(FromHex(DeviceToken)));

    [Test]
    public void DeriveToken_UppercaseHexToken_MatchesLowercase()
        => Assert.That(
            MyJdCrypto.DeriveToken(FromHex(DeviceSecret), SessionTokenReconnect.ToUpperInvariant()),
            Is.EqualTo(FromHex(DeviceToken)));

    [Test]
    public void DeriveToken_Always_LeavesBaseKeyUnchanged()
    {
        var baseKey = FromHex(LoginSecret);
        var copy = (byte[])baseKey.Clone();

        MyJdCrypto.DeriveToken(baseKey, SessionTokenConnect);

        Assert.That(baseKey, Is.EqualTo(copy));
    }

    [Test]
    public void Sign_ConnectQuery_MatchesVector()
        => Assert.That(MyJdCrypto.Sign(FromHex(LoginSecret), SignData), Is.EqualTo(Signature));

    [Test]
    public void DeriveLoginSecret_NullEmail_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.DeriveLoginSecret(null!, Password));

    [Test]
    public void DeriveLoginSecret_NullPassword_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.DeriveLoginSecret(Email, null!));

    [Test]
    public void DeriveDeviceSecret_NullEmail_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.DeriveDeviceSecret(null!, Password));

    [Test]
    public void DeriveDeviceSecret_NullPassword_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.DeriveDeviceSecret(Email, null!));

    [Test]
    public void DeriveLoginSecret_EmptyPassword_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MyJdCrypto.DeriveLoginSecret(Email, string.Empty));

        Assert.That(ex!.GetType(), Is.EqualTo(typeof(ArgumentException)));
    }

    [Test]
    public void DeriveLoginSecret_EmptyEmail_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MyJdCrypto.DeriveLoginSecret(string.Empty, Password));

        Assert.That(ex!.GetType(), Is.EqualTo(typeof(ArgumentException)));
    }

    [Test]
    public void DeriveToken_KeyOf31Bytes_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MyJdCrypto.DeriveToken(new byte[31], SessionTokenConnect));

        Assert.That(ex!.GetType(), Is.EqualTo(typeof(ArgumentException)));
    }

    [Test]
    public void Sign_KeyOf31Bytes_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MyJdCrypto.Sign(new byte[31], SignData));

        Assert.That(ex!.GetType(), Is.EqualTo(typeof(ArgumentException)));
    }

    [Test]
    public void DeriveToken_NonHexToken_ThrowsFormatException()
        => Assert.Throws<FormatException>(
            () => MyJdCrypto.DeriveToken(FromHex(LoginSecret), "zz00112233445566778899aabbccddeeff00112233445566778899aabbccddee"));

    [Test]
    public void DeriveToken_OddLengthHex_ThrowsFormatException()
        => Assert.Throws<FormatException>(() => MyJdCrypto.DeriveToken(FromHex(LoginSecret), "abc"));

    [Test]
    public void DeriveToken_NonHexToken_MessageDoesNotContainToken()
    {
        const string token = "zz00112233445566778899aabbccddeeff00112233445566778899aabbccddee";

        var ex = Assert.Throws<FormatException>(() => MyJdCrypto.DeriveToken(FromHex(LoginSecret), token));

        Assert.That(ex!.Message, Does.Not.Contain(token));
    }

    [Test]
    public void DeriveToken_EmptyToken_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MyJdCrypto.DeriveToken(FromHex(LoginSecret), string.Empty));

        Assert.That(ex!.GetType(), Is.EqualTo(typeof(ArgumentException)));
    }

    [Test]
    public void Sign_NullData_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.Sign(FromHex(LoginSecret), null!));

    [TestCase("", CipherEmpty)]
    [TestCase("0123456789abcdef", CipherBlock)]
    [TestCase(JsonPlaintext, CipherJson)]
    public void Encrypt_ReferencePlaintext_MatchesVector(string plaintext, string expected)
        => Assert.That(MyJdCrypto.Encrypt(FromHex(DeviceToken), plaintext), Is.EqualTo(expected));

    [TestCase(CipherEmpty, "")]
    [TestCase(CipherBlock, "0123456789abcdef")]
    [TestCase(CipherJson, JsonPlaintext)]
    public void Decrypt_ReferenceCiphertext_ReturnsPlaintext(string ciphertext, string expected)
        => Assert.That(MyJdCrypto.Decrypt(FromHex(DeviceToken), ciphertext), Is.EqualTo(expected));

    [Test]
    public void Encrypt_Always_LeavesKeyUnchanged()
    {
        var key = FromHex(DeviceToken);
        var copy = (byte[])key.Clone();

        MyJdCrypto.Encrypt(key, JsonPlaintext);

        Assert.That(key, Is.EqualTo(copy));
    }

    [Test]
    public void Decrypt_InvalidPadding_ThrowsCryptographicException()
        => Assert.Throws<CryptographicException>(() => MyJdCrypto.Decrypt(FromHex(DeviceToken), CipherBadPadding));

    [Test]
    public void Decrypt_InvalidPadding_MessageDoesNotContainCiphertext()
    {
        var ex = Assert.Throws<CryptographicException>(() => MyJdCrypto.Decrypt(FromHex(DeviceToken), CipherBadPadding));

        Assert.That(ex!.Message, Does.Not.Contain(CipherBadPadding));
    }

    [Test]
    public void Decrypt_InvalidBase64_ThrowsFormatException()
        => Assert.Throws<FormatException>(() => MyJdCrypto.Decrypt(FromHex(DeviceToken), "not base64!"));

    [Test]
    public void Decrypt_LengthNotBlockMultiple_ThrowsCryptographicException()
        => Assert.Throws<CryptographicException>(
            () => MyJdCrypto.Decrypt(FromHex(DeviceToken), Convert.ToBase64String(new byte[10])));

    [Test]
    public void Decrypt_EmptyCiphertext_ThrowsCryptographicException()
        => Assert.Throws<CryptographicException>(() => MyJdCrypto.Decrypt(FromHex(DeviceToken), string.Empty));

    [Test]
    public void Encrypt_KeyOf31Bytes_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MyJdCrypto.Encrypt(new byte[31], JsonPlaintext));

        Assert.That(ex!.GetType(), Is.EqualTo(typeof(ArgumentException)));
    }

    [Test]
    public void Decrypt_KeyOf31Bytes_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MyJdCrypto.Decrypt(new byte[31], CipherBlock));

        Assert.That(ex!.GetType(), Is.EqualTo(typeof(ArgumentException)));
    }

    [Test]
    public void Encrypt_NullPlaintext_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.Encrypt(FromHex(DeviceToken), null!));

    [Test]
    public void Encrypt_NullKey_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.Encrypt(null!, JsonPlaintext));

    [Test]
    public void Decrypt_NullCiphertext_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.Decrypt(FromHex(DeviceToken), null!));

    [Test]
    public void Decrypt_NullKey_ThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => MyJdCrypto.Decrypt(null!, CipherBlock));
}
