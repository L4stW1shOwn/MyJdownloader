namespace MyJdownloader.Tests.Sessions;

[TestFixture]
public sealed class MyJdSessionTests
{
    private const string Email = "user@example.com";
    private const string SessionToken = "0011aabb";
    private const string RegainToken = "ccdd2233";

    private const string FixedMessage = "The session data is not a valid MyJdownloader session.";
    private const string ShortLoginSecretBase64 = "AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ==";

#pragma warning disable JSON002 // Probable JSON string: intentional session payloads used as plain string vectors
    private const string ReferenceJson =
        """{"version":1,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"0011aabb","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string UnknownPropertyJson =
        """{"version":1,"extra":1,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"0011aabb","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string DuplicatePropertyJson =
        """{"version":1,"email":"user@example.com","email":"other@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"0011aabb","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string MissingRegainTokenJson =
        """{"version":1,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"0011aabb","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string VersionTwoJson =
        """{"version":2,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"0011aabb","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string ShortLoginSecretJson =
        """{"version":1,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ==","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"0011aabb","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string InvalidBase64DeviceSecretJson =
        """{"version":1,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"%%%","sessionToken":"0011aabb","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string EmptySessionTokenJson =
        """{"version":1,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string NonHexTokenJson =
        """{"version":1,"email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"ab?cd","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string PascalCaseJson =
        """{"Version":1,"Email":"user@example.com","LoginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","DeviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","SessionToken":"0011aabb","RegainToken":"ccdd2233","ServerEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","DeviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";

    private const string VersionAsStringJson =
        """{"version":"1","email":"user@example.com","loginSecret":"AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE=","deviceSecret":"AgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgI=","sessionToken":"0011aabb","regainToken":"ccdd2233","serverEncryptionToken":"AwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwM=","deviceEncryptionToken":"BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQ="}""";
#pragma warning restore JSON002

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

    private static void AssertEveryFixtureField(MyJdSession session) =>
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

    [Test]
    public void ToJson_FixtureSession_EqualsReferenceJson() =>
        Assert.That(CreateFixtureSession().ToJson(), Is.EqualTo(ReferenceJson));

    [Test]
    public void FromJson_ReferenceJson_RestoresEveryField() =>
        AssertEveryFixtureField(MyJdSession.FromJson(ReferenceJson));

    [Test]
    public void FromJson_RoundTrip_RestoresEveryField() =>
        AssertEveryFixtureField(MyJdSession.FromJson(CreateFixtureSession().ToJson()));

    [Test]
    public void FromJson_Null_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => MyJdSession.FromJson(null!));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.ParamName, Is.EqualTo("json"));
    }

    [TestCase("not json", TestName = "FromJson_NotJson_ThrowsFormatException")]
    [TestCase("null", TestName = "FromJson_JsonNull_ThrowsFormatException")]
    [TestCase("", TestName = "FromJson_EmptyString_ThrowsFormatException")]
    [TestCase("{}", TestName = "FromJson_EmptyObject_ThrowsFormatException")]
    [TestCase(MissingRegainTokenJson, TestName = "FromJson_MissingRegainToken_ThrowsFormatException")]
    [TestCase(VersionTwoJson, TestName = "FromJson_UnsupportedVersion_ThrowsFormatException")]
    [TestCase(ShortLoginSecretJson, TestName = "FromJson_LoginSecretOf31Bytes_ThrowsFormatException")]
    [TestCase(InvalidBase64DeviceSecretJson, TestName = "FromJson_InvalidBase64DeviceSecret_ThrowsFormatException")]
    [TestCase(EmptySessionTokenJson, TestName = "FromJson_EmptySessionToken_ThrowsFormatException")]
    public void FromJson_InvalidInput_ThrowsFormatException(string json)
    {
        var ex = Assert.Throws<FormatException>(() => MyJdSession.FromJson(json));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.Message, Is.EqualTo(FixedMessage));
        Assert.That(ex.InnerException, Is.Null);
    }

    [Test]
    public void FromJson_InvalidInput_MessageContainsNoValue()
    {
        var ex = Assert.Throws<FormatException>(() => MyJdSession.FromJson(ShortLoginSecretJson));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Does.Not.Contain(ShortLoginSecretBase64));
            Assert.That(ex.Message, Does.Not.Contain(Email));
        });
    }

    [Test]
    public void FromJson_PascalCaseNames_ThrowsFormatException() =>
        AssertFixedFormatException(PascalCaseJson);

    [Test]
    public void FromJson_VersionAsString_ThrowsFormatException() =>
        AssertFixedFormatException(VersionAsStringJson);

    [Test]
    public void FromJson_UnknownProperty_ThrowsFormatException() =>
        AssertFixedFormatException(UnknownPropertyJson);

    [Test]
    public void FromJson_DuplicateProperty_ThrowsFormatException() =>
        AssertFixedFormatException(DuplicatePropertyJson);

    [Test]
    public void FromJson_NonHexToken_ThrowsFormatException() =>
        AssertFixedFormatException(NonHexTokenJson);

    private static void AssertFixedFormatException(string json)
    {
        var ex = Assert.Throws<FormatException>(() => MyJdSession.FromJson(json));

        Assert.That(ex, Is.Not.Null);
        ArgumentNullException.ThrowIfNull(ex);
        Assert.That(ex.Message, Is.EqualTo(FixedMessage));
        Assert.That(ex.InnerException, Is.Null);
    }
}
