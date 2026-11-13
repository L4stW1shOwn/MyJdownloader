namespace MyJdownloader.Tests;

[TestFixture]
public sealed class MyJDownloaderOptionsTests
{
    [Test]
    public void Constructor_Default_AppKeyIsLibraryName()
        => Assert.That(new MyJDownloaderOptions().AppKey, Is.EqualTo("MyJdownloader.NET"));

    [Test]
    public void Constructor_Default_BaseAddressIsMyJdRelay()
        => Assert.That(new MyJDownloaderOptions().BaseAddress, Is.EqualTo(new Uri("https://api.jdownloader.org")));

    [Test]
    public void Constructor_Default_ApiVersionIsOne()
        => Assert.That(new MyJDownloaderOptions().ApiVersion, Is.EqualTo(1));

    [Test]
    public void Constructor_Default_MaxResponseBodyLengthIsOneMebibyte()
        => Assert.That(new MyJDownloaderOptions().MaxResponseBodyLength, Is.EqualTo(1_048_576));
}
