namespace MyJdownloader.Tests.Sessions;

[TestFixture]
public sealed class MyJdSessionTests
{
    private const string Email = "user@example.com";
    private const string SessionToken = "0011aabb";
    private const string RegainToken = "ccdd2233";

    private static byte[] Key(byte value, int length = 32) => [.. Enumerable.Repeat(value, length)];

    private static MyJdSession CreateFixtureSession() =>
        new(Email, Key(1), Key(2), SessionToken, RegainToken, Key(3), Key(4));

    private static MyJdSession CreateSession(
        string email = Email,
        byte[]? loginSecret = null,
        byte[]? deviceSecret = null,
        string sessionToken = SessionToken,
        string regainToken = RegainToken,
        byte[]? serverEncryptionToken = null,
        byte[]? deviceEncryptionToken = null) =>
        new(
            email,
            loginSecret ?? Key(1),
            deviceSecret ?? Key(2),
            sessionToken,
            regainToken,
            serverEncryptionToken ?? Key(3),
            deviceEncryptionToken ?? Key(4));

    [Test]
    public void Ctor_ValidArguments_ExposesEveryField()
    {
        var session = CreateFixtureSession();

        Assert.Multiple(() =>
        {
            Assert.That(session.Email, Is.EqualTo(Email));
            Assert.That(session.SessionToken, Is.EqualTo(SessionToken));
            Assert.That(session.RegainToken, Is.EqualTo(RegainToken));
            Assert.That(session.LoginSecret.ToArray(), Is.EqualTo(Key(1)));
            Assert.That(session.DeviceSecret.ToArray(), Is.EqualTo(Key(2)));
            Assert.That(session.ServerEncryptionToken.ToArray(), Is.EqualTo(Key(3)));
            Assert.That(session.DeviceEncryptionToken.ToArray(), Is.EqualTo(Key(4)));
        });
    }

    [Test]
    public void Ctor_CallerMutatesArrays_SessionUnchanged()
    {
        var login = Key(1);
        var device = Key(2);
        var serverToken = Key(3);
        var deviceToken = Key(4);
        var session = new MyJdSession(Email, login, device, SessionToken, RegainToken, serverToken, deviceToken);

        Array.Fill(login, (byte)0xFF);
        Array.Fill(device, (byte)0xFF);
        Array.Fill(serverToken, (byte)0xFF);
        Array.Fill(deviceToken, (byte)0xFF);

        Assert.Multiple(() =>
        {
            Assert.That(session.LoginSecret.ToArray(), Is.EqualTo(Key(1)));
            Assert.That(session.DeviceSecret.ToArray(), Is.EqualTo(Key(2)));
            Assert.That(session.ServerEncryptionToken.ToArray(), Is.EqualTo(Key(3)));
            Assert.That(session.DeviceEncryptionToken.ToArray(), Is.EqualTo(Key(4)));
        });
    }

    [TestCase("loginSecret")]
    [TestCase("deviceSecret")]
    [TestCase("serverEncryptionToken")]
    [TestCase("deviceEncryptionToken")]
    public void Ctor_KeyOf31Bytes_ThrowsArgumentException(string parameterName)
    {
        var shortKey = Key(9, 31);

        var ex = Assert.Throws<ArgumentException>(() => _ = parameterName switch
        {
            "loginSecret" => CreateSession(loginSecret: shortKey),
            "deviceSecret" => CreateSession(deviceSecret: shortKey),
            "serverEncryptionToken" => CreateSession(serverEncryptionToken: shortKey),
            _ => CreateSession(deviceEncryptionToken: shortKey),
        });

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.ParamName, Is.EqualTo(parameterName));
    }

    [TestCase("")]
    [TestCase("  ")]
    public void Ctor_EmptyEmail_ThrowsArgumentException(string email)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateSession(email: email));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.ParamName, Is.EqualTo("email"));
    }

    [TestCase("")]
    [TestCase("  ")]
    public void Ctor_EmptySessionToken_ThrowsArgumentException(string token)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateSession(sessionToken: token));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.ParamName, Is.EqualTo("sessionToken"));
    }

    [TestCase("")]
    [TestCase("  ")]
    public void Ctor_EmptyRegainToken_ThrowsArgumentException(string token)
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateSession(regainToken: token));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.ParamName, Is.EqualTo("regainToken"));
    }

    [Test]
    public void Ctor_NonHexSessionToken_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateSession(sessionToken: "ab?cd"));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.GetType(), Is.EqualTo(typeof(ArgumentException)));
        Assert.That(ex.ParamName, Is.EqualTo("sessionToken"));
    }

    [Test]
    public void Ctor_SessionTokenLongerThan128_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateSession(sessionToken: new string('a', 129)));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.GetType(), Is.EqualTo(typeof(ArgumentException)));
        Assert.That(ex.ParamName, Is.EqualTo("sessionToken"));
    }

    [Test]
    public void Ctor_NullKey_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new MyJdSession(Email, null!, Key(2), SessionToken, RegainToken, Key(3), Key(4)));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.ParamName, Is.EqualTo("loginSecret"));
    }

    [Test]
    public void ToString_Always_ReturnsMaskedEmail() =>
        Assert.That(CreateFixtureSession().ToString(), Is.EqualTo("MyJdSession { Email = u***@example.com }"));

    [Test]
    public void ToString_Always_ContainsNoSecretOrToken()
    {
        var text = CreateFixtureSession().ToString();
        var forbidden = new List<string> { SessionToken, RegainToken, Email };
        foreach (var key in new[] { Key(1), Key(2), Key(3), Key(4) })
        {
            forbidden.Add(Convert.ToBase64String(key));
            forbidden.Add(Convert.ToHexStringLower(key));
        }

        Assert.Multiple(() =>
        {
            foreach (var value in forbidden)
            {
                Assert.That(text, Does.Not.Contain(value));
            }
        });
    }

    [TestCase("nomail", "MyJdSession { Email = *** }", TestName = "ToString_EmailWithoutAt_MasksAsStars")]
    [TestCase("a@b@example.com", "MyJdSession { Email = a***@example.com }", TestName = "ToString_SeveralAt_MasksAtLastAt")]
    [TestCase("@example.com", "MyJdSession { Email = ***@example.com }", TestName = "ToString_EmptyLocalPart_MasksAsStars")]
    public void ToString_UnusualEmail_ReturnsExpectedMask(string email, string expected) =>
        Assert.That(CreateSession(email: email).ToString(), Is.EqualTo(expected));
}
