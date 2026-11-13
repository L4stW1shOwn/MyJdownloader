namespace MyJdownloader.Tests.Client;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using MyJdownloader.Errors;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class MyJDownloaderClientTests
{
    private const string OptionsParamName = "options";

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static MyJdSession CreateSession() =>
        new("user@example.com", Key(1), Key(2), "0011aabb", "ccdd2233", Key(3), Key(4));

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static MyJDownloaderClient CreateClient(FakeMyJdServer server, MyJDownloaderOptions? options = null)
        => new(new HttpClient(server), options);

    [SuppressMessage("Reliability", "CA2000", Justification = "The HttpClient lives as long as the test and wraps a disposed fake handler")]
    private static MyJDownloaderClient CreateClient(MyJDownloaderOptions? options = null)
    {
        using var server = new FakeMyJdServer(_ => new HttpResponseMessage(HttpStatusCode.OK));
        return new MyJDownloaderClient(new HttpClient(server), options);
    }

    [Test]
    public void Ctor_NullHttpClient_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new MyJDownloaderClient(null!));

        Assert.That(ex!.ParamName, Is.EqualTo("httpClient"));
    }

    [Test]
    public void Ctor_NullOptions_UsesDefaults()
    {
        MyJDownloaderClient? client = null;

        Assert.DoesNotThrow(() => client = CreateClient(options: null));

        Assert.That(client!.IsConnected, Is.False);
    }

    [Test]
    public void Ctor_RelativeBaseAddress_ThrowsArgumentException()
    {
        var options = new MyJDownloaderOptions { BaseAddress = new Uri("relay.test/api", UriKind.Relative) };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [Test]
    public void Ctor_HttpBaseAddress_ThrowsArgumentException()
    {
        var options = new MyJDownloaderOptions { BaseAddress = new Uri("http://relay.test") };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [TestCase("https://u:p@relay.test")]
    [TestCase("https://relay.test/?x=1")]
    [TestCase("https://relay.test/#f")]
    public void Ctor_BaseAddressWithUserInfoQueryOrFragment_ThrowsArgumentException(string address)
    {
        var options = new MyJDownloaderOptions { BaseAddress = new Uri(address) };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [Test]
    public void Ctor_UpperCaseHttpsWithPath_IsAccepted()
    {
        var options = new MyJDownloaderOptions { BaseAddress = new Uri("HTTPS://Relay.Test/api/") };

        Assert.DoesNotThrow(() => CreateClient(options));
    }

    [TestCase("")]
    [TestCase("  ")]
    public void Ctor_EmptyAppKey_ThrowsArgumentException(string appKey)
    {
        var options = new MyJDownloaderOptions { AppKey = appKey };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [Test]
    public void Ctor_AppKeyWithControlCharacter_ThrowsArgumentException()
    {
        var options = new MyJDownloaderOptions { AppKey = "app\nkey" };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [Test]
    public void Ctor_AppKeyLongerThan256Characters_ThrowsArgumentException()
    {
        var options = new MyJDownloaderOptions { AppKey = new string('a', 257) };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [Test]
    public void Ctor_AppKeyOf256Characters_IsAccepted()
    {
        var options = new MyJDownloaderOptions { AppKey = new string('a', 256) };

        Assert.DoesNotThrow(() => CreateClient(options));
    }

    [Test]
    public void Ctor_ApiVersionZero_ThrowsArgumentException()
    {
        var options = new MyJDownloaderOptions { ApiVersion = 0 };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [TestCase(1_023)]
    [TestCase(1)]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void Ctor_MaxResponseBodyLengthBelowMinimum_ThrowsArgumentException(int length)
    {
        var options = new MyJDownloaderOptions { MaxResponseBodyLength = length };

        var ex = Assert.Throws<ArgumentException>(() => CreateClient(options));

        Assert.That(ex!.ParamName, Is.EqualTo(OptionsParamName));
    }

    [TestCase(1_024)]
    [TestCase(int.MaxValue)]
    public void Ctor_MaxResponseBodyLengthAtMinimumOrAbove_IsAccepted(int length)
    {
        var options = new MyJDownloaderOptions { MaxResponseBodyLength = length };

        Assert.DoesNotThrow(() => CreateClient(options));
    }

    [Test]
    public async Task CallAsync_OptionsChangedAfterConstruction_KeepsConstructionTimeMaxResponseBodyLength()
    {
        var options = new MyJDownloaderOptions { MaxResponseBodyLength = 2_048 };
        using var server = new FakeMyJdServer(_ => FakeMyJdServer.Raw(new string('A', 3_000)));
        var client = CreateClient(server, options);
        client.RestoreSession(CreateSession());
        var channel = client.CreateDeviceChannel("dev1");
        options.MaxResponseBodyLength = 10_000_000;

        var thrown = await Assert.ThrowsAsync<MyJdProtocolException>(() => channel.CallAsync("/jd/version", []).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.That(thrown!.Message, Is.EqualTo("The MyJD device response exceeds the configured size limit."));
    }

    [Test]
    public void ExportSession_NewClient_ReturnsNull()
    {
        var client = CreateClient();

        Assert.That(client.ExportSession(), Is.Null);
    }

    [Test]
    public void RestoreSession_Session_IsConnectedAndExported()
    {
        var client = CreateClient();
        var session = CreateSession();

        client.RestoreSession(session);

        Assert.Multiple(() =>
        {
            Assert.That(client.IsConnected, Is.True);
            Assert.That(client.ExportSession(), Is.SameAs(session));
        });
    }

    [Test]
    public void StateField_Declared_IsHiddenFromDebugger()
    {
        var field = typeof(MyJDownloaderClient).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);

        var attribute = field?.GetCustomAttribute<DebuggerBrowsableAttribute>();

        Assert.That(attribute?.State, Is.EqualTo(DebuggerBrowsableState.Never));
    }

    [Test]
    public void RestoreSession_Session_SendsNoRequestAndRaisesNoEvent()
    {
        using var server = new FakeMyJdServer(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(server);
        var raised = 0;
        client.SessionChanged += (_, _) => raised++;

        client.RestoreSession(CreateSession());

        Assert.Multiple(() =>
        {
            Assert.That(server.Requests, Is.Empty);
            Assert.That(raised, Is.Zero);
        });
    }

    [Test]
    public void RestoreSession_Null_ThrowsArgumentNullException()
    {
        var client = CreateClient();

        var ex = Assert.Throws<ArgumentNullException>(() => client.RestoreSession(null!));

        Assert.That(ex!.ParamName, Is.EqualTo("session"));
    }
}
