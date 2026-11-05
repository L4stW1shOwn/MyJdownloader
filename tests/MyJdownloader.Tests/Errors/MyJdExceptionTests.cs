namespace MyJdownloader.Tests.Errors;

using System.Net;

[TestFixture]
public sealed class MyJdExceptionTests
{
    private const string Message = "m";
    private const string ErrorType = "X";

    private static IEnumerable<TestCaseData> FullFactories()
    {
        yield return new TestCaseData(
            (Func<MyJdException>)(() => new MyJdAuthenticationException(Message, MyJdErrorSource.Device, ErrorType, HttpStatusCode.Forbidden)))
            .SetArgDisplayNames(nameof(MyJdAuthenticationException));
        yield return new TestCaseData(
            (Func<MyJdException>)(() => new MyJdDeviceOfflineException(Message, MyJdErrorSource.Device, ErrorType, HttpStatusCode.Forbidden)))
            .SetArgDisplayNames(nameof(MyJdDeviceOfflineException));
        yield return new TestCaseData(
            (Func<MyJdException>)(() => new MyJdApiException(Message, MyJdErrorSource.Device, ErrorType, HttpStatusCode.Forbidden)))
            .SetArgDisplayNames(nameof(MyJdApiException));
    }

    private static IEnumerable<TestCaseData> DefaultFactories()
    {
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdAuthenticationException()))
            .SetArgDisplayNames(nameof(MyJdAuthenticationException));
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdDeviceOfflineException()))
            .SetArgDisplayNames(nameof(MyJdDeviceOfflineException));
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdApiException()))
            .SetArgDisplayNames(nameof(MyJdApiException));
    }

    private static IEnumerable<TestCaseData> InnerFactories()
    {
        var inner = new InvalidOperationException("inner");
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdAuthenticationException(Message, inner)), inner)
            .SetArgDisplayNames(nameof(MyJdAuthenticationException));
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdDeviceOfflineException(Message, inner)), inner)
            .SetArgDisplayNames(nameof(MyJdDeviceOfflineException));
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdApiException(Message, inner)), inner)
            .SetArgDisplayNames(nameof(MyJdApiException));
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdRateLimitException(Message, inner)), inner)
            .SetArgDisplayNames(nameof(MyJdRateLimitException));
        yield return new TestCaseData((Func<MyJdException>)(() => new MyJdProtocolException(Message, inner)), inner)
            .SetArgDisplayNames(nameof(MyJdProtocolException));
    }

    private static IEnumerable<TestCaseData> AllTypeFactories()
    {
        yield return new TestCaseData((Func<object>)(() => new MyJdAuthenticationException()))
            .SetArgDisplayNames(nameof(MyJdAuthenticationException));
        yield return new TestCaseData((Func<object>)(() => new MyJdDeviceOfflineException()))
            .SetArgDisplayNames(nameof(MyJdDeviceOfflineException));
        yield return new TestCaseData((Func<object>)(() => new MyJdApiException()))
            .SetArgDisplayNames(nameof(MyJdApiException));
        yield return new TestCaseData((Func<object>)(() => new MyJdRateLimitException()))
            .SetArgDisplayNames(nameof(MyJdRateLimitException));
        yield return new TestCaseData((Func<object>)(() => new MyJdProtocolException()))
            .SetArgDisplayNames(nameof(MyJdProtocolException));
    }

    [TestCaseSource(nameof(FullFactories))]
    public void Ctor_Full_SetsProperties(Func<MyJdException> create)
    {
        ArgumentNullException.ThrowIfNull(create);

        var exception = create();

        Assert.That(exception.Message, Is.EqualTo(Message));
        Assert.That(exception.ErrorSource, Is.EqualTo(MyJdErrorSource.Device));
        Assert.That(exception.ErrorType, Is.EqualTo(ErrorType));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [TestCaseSource(nameof(DefaultFactories))]
    public void Ctor_Default_LeavesErrorPropertiesNull(Func<MyJdException> create)
    {
        ArgumentNullException.ThrowIfNull(create);

        var exception = create();

        Assert.That(exception.ErrorSource, Is.Null);
        Assert.That(exception.ErrorType, Is.Null);
        Assert.That(exception.StatusCode, Is.Null);
    }

    [TestCaseSource(nameof(InnerFactories))]
    public void Ctor_MessageAndInner_SetsInnerException(Func<MyJdException> create, Exception inner)
    {
        ArgumentNullException.ThrowIfNull(create);

        var exception = create();

        Assert.That(exception.Message, Is.EqualTo(Message));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void MyJdRateLimitException_Ctor_Full_SetsRetryAfter()
    {
        var exception = new MyJdRateLimitException(
            Message, MyJdErrorSource.Server, ErrorType, HttpStatusCode.TooManyRequests, TimeSpan.FromSeconds(30));

        Assert.That(exception.RetryAfter, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(exception.ErrorSource, Is.EqualTo(MyJdErrorSource.Server));
        Assert.That(exception.ErrorType, Is.EqualTo(ErrorType));
        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
    }

    [Test]
    public void MyJdRateLimitException_Ctor_NullRetryAfter_RetryAfterIsNull()
    {
        var exception = new MyJdRateLimitException(
            Message, MyJdErrorSource.Server, ErrorType, HttpStatusCode.TooManyRequests, null);

        Assert.That(exception.RetryAfter, Is.Null);
    }

    [Test]
    public void MyJdProtocolException_Ctor_MessageAndSource_SetsSourceOnly()
    {
        var exception = new MyJdProtocolException(Message, MyJdErrorSource.Server);

        Assert.That(exception.Message, Is.EqualTo(Message));
        Assert.That(exception.ErrorSource, Is.EqualTo(MyJdErrorSource.Server));
        Assert.That(exception.ErrorType, Is.Null);
        Assert.That(exception.StatusCode, Is.Null);
        Assert.That(exception.InnerException, Is.Null);
    }

    [TestCaseSource(nameof(AllTypeFactories))]
    public void AllTypes_DeriveFromMyJdException(Func<object> create)
    {
        ArgumentNullException.ThrowIfNull(create);

        Assert.That(create(), Is.InstanceOf<MyJdException>());
    }
}
