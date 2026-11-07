namespace MyJdownloader.Tests.Errors;

using System.Net;
using MyJdownloader.Errors;

[TestFixture]
public sealed class MyJdErrorMapperTests
{
    [TestCase("AUTH_FAILED", typeof(MyJdAuthenticationException))]
    [TestCase("EMAIL_INVALID", typeof(MyJdAuthenticationException))]
    [TestCase("EMAIL_FORBIDDEN", typeof(MyJdAuthenticationException))]
    [TestCase("TOKEN_INVALID", typeof(MyJdAuthenticationException))]
    [TestCase("ERROR_EMAIL_NOT_CONFIRMED", typeof(MyJdAuthenticationException))]
    [TestCase("OFFLINE", typeof(MyJdDeviceOfflineException))]
    [TestCase("TOO_MANY_REQUESTS", typeof(MyJdRateLimitException))]
    [TestCase("OVERLOAD", typeof(MyJdRateLimitException))]
    [TestCase("MAINTENANCE", typeof(MyJdRateLimitException))]
    [TestCase("BAD_PARAMETERS", typeof(MyJdApiException))]
    [TestCase("SOMETHING_NEW", typeof(MyJdApiException))]
    public void Map_KnownCode_ReturnsExpectedType(string type, Type expected)
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result, Is.TypeOf(expected));
        Assert.That(result.ErrorType, Is.EqualTo(type));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Map_MissingType_ReturnsApiExceptionWithUnknown(string? type)
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result, Is.TypeOf<MyJdApiException>());
        Assert.That(result.ErrorType, Is.EqualTo("UNKNOWN"));
    }

    [Test]
    public void Map_PaddedLowercaseType_Normalizes()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, " auth_failed ", HttpStatusCode.Forbidden, null);

        Assert.That(result, Is.TypeOf<MyJdAuthenticationException>());
        Assert.That(result.ErrorType, Is.EqualTo("AUTH_FAILED"));
    }

    [TestCase("SERVER", MyJdErrorSource.Server, MyJdErrorSource.Device)]
    [TestCase("device", MyJdErrorSource.Device, MyJdErrorSource.Server)]
    public void Map_RecognizedSrc_ResolvesSource(string src, MyJdErrorSource expected, MyJdErrorSource channelSource)
    {
        var result = MyJdErrorMapper.Map(channelSource, src, "BAD_PARAMETERS", HttpStatusCode.BadRequest, null);

        Assert.That(result.ErrorSource, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("RELAY")]
    public void Map_MissingOrUnknownSrc_UsesChannelSource(string? src)
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Device, src, "BAD_PARAMETERS", HttpStatusCode.BadRequest, null);

        Assert.That(result.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public void Map_PaddedLowercaseSrc_ResolvesDevice()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, " device ", "BAD_PARAMETERS", HttpStatusCode.BadRequest, null);

        Assert.That(result.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
    }

    [Test]
    public void Map_Always_CarriesStatusCode()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, "BAD_PARAMETERS", HttpStatusCode.Conflict, null);

        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public void Map_RateLimitCode_CarriesRetryAfter()
    {
        var retryAfter = TimeSpan.FromSeconds(42);

        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, "TOO_MANY_REQUESTS", HttpStatusCode.TooManyRequests, retryAfter);

        Assert.That(result, Is.TypeOf<MyJdRateLimitException>());
        Assert.That(((MyJdRateLimitException)result).RetryAfter, Is.EqualTo(retryAfter));
    }

    [Test]
    public void Map_RetryAfterOnApiError_ReturnsApiException()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, "BAD_PARAMETERS", HttpStatusCode.BadRequest, TimeSpan.FromSeconds(42));

        Assert.That(result, Is.TypeOf<MyJdApiException>());
    }

    [Test]
    public void Map_KnownCode_MessageHasExactFormat()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, "AUTH_FAILED", HttpStatusCode.Forbidden, null);

        Assert.That(result.Message, Is.EqualTo("MyJD server error AUTH_FAILED (HTTP 403)."));
    }

    [Test]
    public void Map_DeviceSource_MessageSaysDevice()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Device, null, "OFFLINE", HttpStatusCode.GatewayTimeout, null);

        Assert.That(result.Message, Is.EqualTo("MyJD device error OFFLINE (HTTP 504)."));
    }

    [Test]
    public void Map_UnnamedStatusCode_MessageShowsNumericCode()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, "BAD_PARAMETERS", (HttpStatusCode)599, null);

        Assert.That(result.Message, Does.Contain("(HTTP 599)"));
    }

    [TestCase("BAD\nTYPE")]
    [TestCase("<script>")]
    public void Map_UnsafeType_MessageShowsUnknown(string type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result.Message, Is.EqualTo("MyJD server error UNKNOWN (HTTP 400)."));
        Assert.That(result.ErrorType, Is.EqualTo(type.Trim().ToUpperInvariant()));
    }

    [Test]
    public void Map_TypeOf64Chars_MessageShowsType()
    {
        var type = new string('A', 64);

        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result.Message, Is.EqualTo($"MyJD server error {type} (HTTP 400)."));
    }

    [Test]
    public void Map_TypeLongerThan64_MessageShowsUnknown()
    {
        var type = new string('A', 65);

        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result.Message, Is.EqualTo("MyJD server error UNKNOWN (HTTP 400)."));
    }

    [Test]
    public void Map_TypeOf256Chars_KeepsErrorType()
    {
        var type = new string('A', 256);

        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result.ErrorType, Is.EqualTo(type));
    }

    [Test]
    public void Map_TypeLongerThan256_TruncatesErrorType()
    {
        var type = new string('a', 10_000);

        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result.ErrorType, Is.EqualTo(new string('A', 256)));
    }

    [Test]
    public void Map_SurrogatePairAtCap_DoesNotSplitPair()
    {
        var type = new string('A', 255) + "\U0001F600" + "B";

        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, type, HttpStatusCode.BadRequest, null);

        Assert.That(result.ErrorType, Is.EqualTo(new string('A', 255)));
    }

    [Test]
    public void Map_NonAsciiType_MessageShowsUnknown()
    {
        var result = MyJdErrorMapper.Map(MyJdErrorSource.Server, null, "auth_faİled", HttpStatusCode.BadRequest, null);

        Assert.That(result.Message, Is.EqualTo("MyJD server error UNKNOWN (HTTP 400)."));
    }
}
