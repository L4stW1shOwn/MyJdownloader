namespace MyJdownloader.Tests.Downloads;

using System.Text.Json;
using MyJdownloader.Downloads;

[TestFixture]
public sealed class DownloadsJsonTests
{
    private static readonly string[] _expectedHosts = ["h"];

    private static DownloadLink DeserializeLink(string objectJson)
    {
        var links = JsonSerializer.Deserialize("[" + objectJson + "]", DownloadsJsonContext.Default.ListDownloadLink);
        return links![0];
    }

    [Test]
    public void DownloadLink_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"uuid":1,"packageUUID":2,"name":"a.zip","url":"https://h/a.zip","host":"h","comment":"c","downloadPassword":"p","bytesLoaded":5,"bytesTotal":10,"speed":100,"eta":90,"enabled":true,"finished":false,"running":true,"skipped":false,"priority":"HIGH","status":"Downloading","statusIconKey":"run","extractionStatus":"NA","addedDate":1730900000000,"finishedDate":1730900500000,"extra":1}
            """;

        var link = DeserializeLink(payload);

        Assert.That(link.Uuid, Is.EqualTo(1L));
        Assert.That(link.PackageUuid, Is.EqualTo(2L));
        Assert.That(link.Name, Is.EqualTo("a.zip"));
        Assert.That(link.Url, Is.EqualTo("https://h/a.zip"));
        Assert.That(link.Host, Is.EqualTo("h"));
        Assert.That(link.Comment, Is.EqualTo("c"));
        Assert.That(link.DownloadPassword, Is.EqualTo("p"));
        Assert.That(link.BytesLoaded, Is.EqualTo(5L));
        Assert.That(link.BytesTotal, Is.EqualTo(10L));
        Assert.That(link.Speed, Is.EqualTo(100L));
        Assert.That(link.Eta, Is.EqualTo(TimeSpan.FromSeconds(90)));
        Assert.That(link.Enabled, Is.EqualTo(true));
        Assert.That(link.Finished, Is.EqualTo(false));
        Assert.That(link.Running, Is.EqualTo(true));
        Assert.That(link.Skipped, Is.EqualTo(false));
        Assert.That(link.Priority, Is.EqualTo(Priority.High));
        Assert.That(link.Status, Is.EqualTo("Downloading"));
        Assert.That(link.StatusIconKey, Is.EqualTo("run"));
        Assert.That(link.ExtractionStatus, Is.EqualTo("NA"));
        Assert.That(link.AddedDate, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1730900000000)));
        Assert.That(link.FinishedDate, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1730900500000)));
    }

    [Test]
    public void DownloadLink_NotSetSentinels_ReadAsNull()
    {
        const string payload = /*lang=json,strict*/ """{"uuid":1,"finishedDate":-1,"eta":-1}""";

        var link = DeserializeLink(payload);

        Assert.That(link.FinishedDate, Is.Null);
        Assert.That(link.Eta, Is.Null);
    }

    [Test]
    public void DownloadLink_UnknownPriority_ReadsUnknown()
    {
        const string payload = /*lang=json,strict*/ """{"uuid":1,"priority":"SOMETHING_NEW"}""";

        var link = DeserializeLink(payload);

        Assert.That(link.Priority, Is.EqualTo(Priority.Unknown));
    }

    [Test]
    public void DownloadPackage_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            [{"uuid":2,"name":"pkg","activeTask":"Extracting","bytesLoaded":5,"bytesTotal":10,"speed":100,"eta":90,"childCount":1,"comment":"c","downloadPassword":"p","enabled":true,"finished":false,"running":true,"hosts":["h"],"priority":"DEFAULT","saveTo":"/dl","status":"s","statusIconKey":"k"}]
            """;

        var packages = JsonSerializer.Deserialize(payload, DownloadsJsonContext.Default.ListDownloadPackage);

        var package = packages![0];
        Assert.That(package.Uuid, Is.EqualTo(2L));
        Assert.That(package.Name, Is.EqualTo("pkg"));
        Assert.That(package.ActiveTask, Is.EqualTo("Extracting"));
        Assert.That(package.BytesLoaded, Is.EqualTo(5L));
        Assert.That(package.BytesTotal, Is.EqualTo(10L));
        Assert.That(package.Speed, Is.EqualTo(100L));
        Assert.That(package.Eta, Is.EqualTo(TimeSpan.FromSeconds(90)));
        Assert.That(package.ChildCount, Is.EqualTo(1));
        Assert.That(package.Comment, Is.EqualTo("c"));
        Assert.That(package.DownloadPassword, Is.EqualTo("p"));
        Assert.That(package.Enabled, Is.EqualTo(true));
        Assert.That(package.Finished, Is.EqualTo(false));
        Assert.That(package.Running, Is.EqualTo(true));
        Assert.That(package.Hosts, Is.EqualTo(_expectedHosts));
        Assert.That(package.Priority, Is.EqualTo(Priority.Default));
        Assert.That(package.SaveTo, Is.EqualTo("/dl"));
        Assert.That(package.Status, Is.EqualTo("s"));
        Assert.That(package.StatusIconKey, Is.EqualTo("k"));
    }

    [Test]
    public void DownloadLinkQuery_Default_SerializesAllFlagsExceptPassword()
    {
        var json = JsonSerializer.Serialize(new DownloadLinkQuery(), DownloadsJsonContext.Default.DownloadLinkQuery);

        const string expected = /*lang=json,strict*/ """
            {"addedDate":true,"bytesLoaded":true,"bytesTotal":true,"comment":true,"enabled":true,"eta":true,"extractionStatus":true,"finished":true,"finishedDate":true,"host":true,"priority":true,"running":true,"skipped":true,"speed":true,"status":true,"url":true,"password":false,"maxResults":-1,"startAt":0}
            """;
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void DownloadLinkQuery_Uuids_UseApiNames()
    {
        var query = new DownloadLinkQuery { PackageUuids = [5L], JobUuids = [6L] };

        var json = JsonSerializer.Serialize(query, DownloadsJsonContext.Default.DownloadLinkQuery);

        Assert.That(json, Does.Contain("\"packageUUIDs\":[5]"));
        Assert.That(json, Does.Contain("\"jobUUIDs\":[6]"));
    }

    [Test]
    public void DownloadPackageQuery_Default_Serializes()
    {
        var json = JsonSerializer.Serialize(new DownloadPackageQuery(), DownloadsJsonContext.Default.DownloadPackageQuery);

        const string expected = /*lang=json,strict*/ """
            {"bytesLoaded":true,"bytesTotal":true,"childCount":true,"comment":true,"enabled":true,"eta":true,"finished":true,"hosts":true,"priority":true,"running":true,"saveTo":true,"speed":true,"status":true,"maxResults":-1,"startAt":0}
            """;
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void CleanupEnums_Serialize_UpperSnake()
    {
        var action = JsonSerializer.Serialize(CleanupAction.DeleteDupe, DownloadsJsonContext.Default.CleanupAction);
        var linksOnly = JsonSerializer.Serialize(CleanupMode.RemoveLinksOnly, DownloadsJsonContext.Default.CleanupMode);
        var recycle = JsonSerializer.Serialize(CleanupMode.RemoveLinksAndRecycleFiles, DownloadsJsonContext.Default.CleanupMode);
        var selection = JsonSerializer.Serialize(SelectionType.Unselected, DownloadsJsonContext.Default.SelectionType);

        Assert.That(action, Is.EqualTo("\"DELETE_DUPE\""));
        Assert.That(linksOnly, Is.EqualTo("\"REMOVE_LINKS_ONLY\""));
        Assert.That(recycle, Is.EqualTo("\"REMOVE_LINKS_AND_RECYCLE_FILES\""));
        Assert.That(selection, Is.EqualTo("\"UNSELECTED\""));
    }

    [Test]
    public void Records_ToString_MasksPasswords()
    {
        var link = new DownloadLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "secret" };
        var package = new DownloadPackage { Uuid = 2, DownloadPassword = "secret" };

        var texts = new[] { link.ToString(), package.ToString() };

        Assert.That(texts, Has.All.Contain("***"));
        Assert.That(texts, Has.None.Contain("secret"));
    }

    [Test]
    public void DownloadLink_Equality_IgnoresToStringOverride()
    {
        var first = new DownloadLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "p" };
        var second = new DownloadLink { Uuid = 1, PackageUuid = 2, DownloadPassword = "p" };

        Assert.That(first, Is.EqualTo(second));
    }
}
