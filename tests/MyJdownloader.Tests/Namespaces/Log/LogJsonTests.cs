namespace MyJdownloader.Tests.Log;

using System.Text.Json;
using MyJdownloader.Log;

[TestFixture]
public sealed class LogJsonTests
{
    [Test]
    public void LogFolder_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            [{"created":1700000000000,"current":true,"lastModified":1700000500000},{"created":1690000000000,"current":false,"lastModified":1690000900000}]
            """;

        var items = JsonSerializer.Deserialize(payload, LogJsonContext.Default.ListLogFolder)!;

        Assert.Multiple(() =>
        {
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].Created, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000)));
            Assert.That(items[0].Current, Is.True);
            Assert.That(items[0].LastModified, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1700000500000)));
            Assert.That(items[1].Current, Is.False);
        });
    }

    [Test]
    public void LogFolder_SentinelDates_ReadAsNull()
    {
        var items = JsonSerializer.Deserialize(/*lang=json,strict*/ """[{"created":-1,"current":false,"lastModified":-1}]""", LogJsonContext.Default.ListLogFolder)!;

        Assert.Multiple(() =>
        {
            Assert.That(items[0].Created, Is.Null);
            Assert.That(items[0].LastModified, Is.Null);
        });
    }

    [Test]
    public void LogFolder_UnknownFields_AreSkipped()
    {
        var items = JsonSerializer.Deserialize(/*lang=json,strict*/ """[{"extra":1,"created":1700000000000,"current":true}]""", LogJsonContext.Default.ListLogFolder)!;

        Assert.That(items[0].Current, Is.True);
    }

    [Test]
    public void LogFolder_Write_UsesEpochMilliseconds()
    {
        var folder = new LogFolder
        {
            Created = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),
            Current = true,
            LastModified = DateTimeOffset.FromUnixTimeMilliseconds(1700000500000),
        };

        var json = JsonSerializer.Serialize([folder], LogJsonContext.Default.LogFolderArray);

        Assert.That(json, Is.EqualTo(/*lang=json,strict*/ """[{"created":1700000000000,"current":true,"lastModified":1700000500000}]"""));
    }
}
