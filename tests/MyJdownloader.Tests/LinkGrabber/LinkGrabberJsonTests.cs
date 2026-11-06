namespace MyJdownloader.Tests.LinkGrabber;

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using MyJdownloader.LinkGrabber;

[TestFixture]
public sealed class LinkGrabberJsonTests
{
    private static readonly string[] _expectedHosts = ["h"];

    private static Dictionary<string, string> ToRawMap(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetRawText());
    }

    [Test]
    public void CrawledLink_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"uuid":1,"packageUUID":2,"name":"a.zip","url":"https://h/a.zip","host":"h","bytesTotal":10,"availability":"ONLINE","enabled":true,"priority":"HIGH","comment":"c","downloadPassword":"p","variants":true,"variant":{"id":"v","name":"n","iconKey":"i"},"status":"Online","statusIconKey":"ok","extraField":1}
            """;

        var link = JsonSerializer.Deserialize(payload, LinkGrabberJsonContext.Default.CrawledLink);

        Assert.That(link, Is.Not.Null);
        Assert.That(link!.Uuid, Is.EqualTo(1L));
        Assert.That(link.PackageUuid, Is.EqualTo(2L));
        Assert.That(link.Name, Is.EqualTo("a.zip"));
        Assert.That(link.Url, Is.EqualTo("https://h/a.zip"));
        Assert.That(link.Host, Is.EqualTo("h"));
        Assert.That(link.BytesTotal, Is.EqualTo(10L));
        Assert.That(link.Availability, Is.EqualTo(AvailableLinkState.Online));
        Assert.That(link.Enabled, Is.EqualTo(true));
        Assert.That(link.Priority, Is.EqualTo(Priority.High));
        Assert.That(link.Comment, Is.EqualTo("c"));
        Assert.That(link.DownloadPassword, Is.EqualTo("p"));
        Assert.That(link.Variants, Is.EqualTo(true));
        Assert.That(link.Variant, Is.EqualTo(new LinkVariant { Id = "v", Name = "n", IconKey = "i" }));
        Assert.That(link.Status, Is.EqualTo("Online"));
        Assert.That(link.StatusIconKey, Is.EqualTo("ok"));
    }

    [Test]
    public void CrawledPackage_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"uuid":2,"name":"pkg","bytesTotal":10,"childCount":1,"hosts":["h"],"onlineCount":1,"offlineCount":0,"tempUnknownCount":0,"unknownCount":0,"priority":"DEFAULT","saveTo":"/dl","enabled":true,"status":"Finished","statusIconKey":"done"}
            """;

        var package = JsonSerializer.Deserialize(payload, LinkGrabberJsonContext.Default.CrawledPackage);

        Assert.That(package, Is.Not.Null);
        Assert.That(package!.Uuid, Is.EqualTo(2L));
        Assert.That(package.Name, Is.EqualTo("pkg"));
        Assert.That(package.BytesTotal, Is.EqualTo(10L));
        Assert.That(package.ChildCount, Is.EqualTo(1));
        Assert.That(package.Hosts, Is.EqualTo(_expectedHosts));
        Assert.That(package.OnlineCount, Is.EqualTo(1));
        Assert.That(package.OfflineCount, Is.EqualTo(0));
        Assert.That(package.TempUnknownCount, Is.EqualTo(0));
        Assert.That(package.UnknownCount, Is.EqualTo(0));
        Assert.That(package.Priority, Is.EqualTo(Priority.Default));
        Assert.That(package.SaveTo, Is.EqualTo("/dl"));
        Assert.That(package.Enabled, Is.EqualTo(true));
        Assert.That(package.Status, Is.EqualTo("Finished"));
        Assert.That(package.StatusIconKey, Is.EqualTo("done"));
    }

    [Test]
    public void LinkQuery_Default_SerializesAllFlagsExceptPassword()
    {
        var json = JsonSerializer.Serialize(new LinkQuery(), LinkGrabberJsonContext.Default.LinkQuery);

        var expected = new Dictionary<string, string>
        {
            ["availability"] = "true",
            ["bytesTotal"] = "true",
            ["comment"] = "true",
            ["enabled"] = "true",
            ["host"] = "true",
            ["priority"] = "true",
            ["status"] = "true",
            ["url"] = "true",
            ["variantID"] = "true",
            ["variantIcon"] = "true",
            ["variantName"] = "true",
            ["variants"] = "true",
            ["password"] = "false",
            ["maxResults"] = "-1",
            ["startAt"] = "0",
        };
        Assert.That(ToRawMap(json), Is.EquivalentTo(expected));
    }

    [Test]
    public void PackageQuery_Default_Serializes()
    {
        var json = JsonSerializer.Serialize(new PackageQuery(), LinkGrabberJsonContext.Default.PackageQuery);

        var expected = new Dictionary<string, string>
        {
            ["availableOfflineCount"] = "true",
            ["availableOnlineCount"] = "true",
            ["availableTempUnknownCount"] = "true",
            ["availableUnknownCount"] = "true",
            ["bytesTotal"] = "true",
            ["childCount"] = "true",
            ["comment"] = "true",
            ["enabled"] = "true",
            ["hosts"] = "true",
            ["priority"] = "true",
            ["saveTo"] = "true",
            ["status"] = "true",
            ["maxResults"] = "-1",
            ["startAt"] = "0",
        };
        Assert.That(ToRawMap(json), Is.EquivalentTo(expected));
    }

    [Test]
    public void LinkQuery_WithUuids_SerializesUuidArrays()
    {
        var query = new LinkQuery { PackageUuids = [1L, 2L], JobUuids = [3L] };

        var map = ToRawMap(JsonSerializer.Serialize(query, LinkGrabberJsonContext.Default.LinkQuery));

        Assert.That(map["packageUUIDs"], Is.EqualTo("[1,2]"));
        Assert.That(map["jobUUIDs"], Is.EqualTo("[3]"));
    }

    [Test]
    public void AddLinksQueryDto_Serializes()
    {
        var request = new AddLinksRequest
        {
            Links = ["https://h/a", "https://h/b"],
            PackageName = "pkg",
            Priority = Priority.High,
            Autostart = true,
        };

        var json = JsonSerializer.Serialize(AddLinksQueryDto.From(request, [.. request.Links]), LinkGrabberJsonContext.Default.AddLinksQueryDto);

        var expected = new Dictionary<string, string>
        {
            ["links"] = "\"https://h/a\\nhttps://h/b\"",
            ["packageName"] = "\"pkg\"",
            ["priority"] = "\"HIGH\"",
            ["autostart"] = "true",
        };
        Assert.That(ToRawMap(json), Is.EquivalentTo(expected));
    }

    [Test]
    public void ListCrawledLink_Array_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """[{"uuid":1,"packageUUID":2}]""";

        var links = JsonSerializer.Deserialize(payload, LinkGrabberJsonContext.Default.ListCrawledLink);

        Assert.That(links, Has.Count.EqualTo(1));
        Assert.That(links![0].PackageUuid, Is.EqualTo(2L));
    }

    [Test]
    public void ListCrawledPackage_Array_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """[{"uuid":2,"name":"pkg"}]""";

        var packages = JsonSerializer.Deserialize(payload, LinkGrabberJsonContext.Default.ListCrawledPackage);

        Assert.That(packages, Has.Count.EqualTo(1));
        Assert.That(packages![0].Name, Is.EqualTo("pkg"));
    }

    [Test]
    public void Records_ToString_MasksPasswords()
    {
        var request = new AddLinksRequest
        {
            Links = ["https://h/a"],
            ExtractPassword = "extract-secret",
            DownloadPassword = "download-secret",
        };
        var link = new CrawledLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "link-secret" };
        var package = new CrawledPackage { Uuid = 2, DownloadPassword = "package-secret" };

        var texts = new[] { request.ToString(), link.ToString(), package.ToString() };

        Assert.That(texts, Has.All.Contain("***"));
        Assert.That(texts[0], Does.Not.Contain("extract-secret").And.Not.Contain("download-secret"));
        Assert.That(texts[1], Does.Not.Contain("link-secret"));
        Assert.That(texts[2], Does.Not.Contain("package-secret"));
    }

    [Test]
    public void CrawledLink_Equality_IgnoresToStringOverride()
    {
        var first = new CrawledLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "p" };
        var second = new CrawledLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "p" };

        Assert.That(first, Is.EqualTo(second));
    }

    [TestCase(typeof(AddLinksRequest), "ExtractPassword")]
    [TestCase(typeof(AddLinksRequest), "DownloadPassword")]
    [TestCase(typeof(CrawledLink), "DownloadPassword")]
    [TestCase(typeof(CrawledPackage), "DownloadPassword")]
    public void PasswordProperty_Declared_IsHiddenFromDebugger(Type type, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(type);
        var attribute = type.GetProperty(propertyName)!.GetCustomAttribute<DebuggerBrowsableAttribute>();

        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.State, Is.EqualTo(DebuggerBrowsableState.Never));
    }

    [Test]
    public void AddLinksQueryDto_Type_IsNotARecord()
        => Assert.That(typeof(AddLinksQueryDto).GetMethod("<Clone>$"), Is.Null);

    [Test]
    public void CrawledLink_DifferentDownloadPassword_NotEqual()
    {
        var first = new CrawledLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "p1" };
        var second = new CrawledLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "p2" };

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void AddLinksRequest_ToString_HidesSourceUrlAndShowsLinkCount()
    {
        var request = new AddLinksRequest
        {
            Links = ["https://secret-host/link-one", "https://secret-host/link-two"],
            SourceUrl = "https://source.example/private?token=abc",
        };

        var text = request.ToString();

        Assert.That(text, Does.Not.Contain("source.example").And.Not.Contain("token=abc").And.Not.Contain("secret-host"));
        Assert.That(text, Does.Contain("Links = [2 item(s)]"));
    }
}
