namespace MyJdownloader.Tests.Extraction;

using System.Text.Json;
using MyJdownloader.Extraction;

[TestFixture]
public sealed class ExtractionJsonTests
{
    private static readonly string[] _expectedPasswords = ["p1", "p2"];

    private static ArchiveStatus DeserializeStatus(string objectJson)
    {
        var list = JsonSerializer.Deserialize("[" + objectJson + "]", ExtractionJsonContext.Default.ListArchiveStatus);
        return list![0];
    }

    private static ArchiveSettings DeserializeSettings(string objectJson)
    {
        var list = JsonSerializer.Deserialize("[" + objectJson + "]", ExtractionJsonContext.Default.ListArchiveSettings);
        return list![0];
    }

    [Test]
    public void ArchiveStatus_SampleWithUnknownFields_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"archiveId":"a1","archiveName":"movie","controllerId":12,"controllerStatus":"RUNNING","states":{"movie.part1.rar":"COMPLETE","movie.part2.rar":"INCOMPLETE","movie.part3.rar":"MISSING"},"type":"RAR_MULTI","extra":1,"nested":{"x":[1,2]}}
            """;

        var status = DeserializeStatus(payload);

        Assert.Multiple(() =>
        {
            Assert.That(status.ArchiveId, Is.EqualTo("a1"));
            Assert.That(status.ArchiveName, Is.EqualTo("movie"));
            Assert.That(status.ControllerId, Is.EqualTo(12L));
            Assert.That(status.ControllerStatus, Is.EqualTo(ExtractionControllerStatus.Running));
            Assert.That(status.Type, Is.EqualTo("RAR_MULTI"));
            Assert.That(status.States!["movie.part1.rar"], Is.EqualTo(ArchiveFileStatus.Complete));
            Assert.That(status.States["movie.part2.rar"], Is.EqualTo(ArchiveFileStatus.Incomplete));
            Assert.That(status.States["movie.part3.rar"], Is.EqualTo(ArchiveFileStatus.Missing));
        });
    }

    [Test]
    public void ArchiveStatus_UnknownFileStatus_ReadsUnknown()
    {
        var status = DeserializeStatus(/*lang=json,strict*/ """{"states":{"a.rar":"CORRUPTED"}}""");

        Assert.That(status.States!["a.rar"], Is.EqualTo(ArchiveFileStatus.Unknown));
    }

    [TestCase("RUNNING", ExtractionControllerStatus.Running)]
    [TestCase("QUEUED", ExtractionControllerStatus.Queued)]
    [TestCase("NA", ExtractionControllerStatus.Na)]
    [TestCase("PAUSED", ExtractionControllerStatus.Unknown)]
    public void ArchiveStatus_ControllerStatus_MapsWireValue(string wire, ExtractionControllerStatus expected)
    {
        var status = DeserializeStatus("{\"controllerStatus\":\"" + wire + "\"}");

        Assert.That(status.ControllerStatus, Is.EqualTo(expected));
    }

    [Test]
    public void ArchiveStatus_NoController_ReadsMinusOneAndNa()
    {
        var status = DeserializeStatus(/*lang=json,strict*/ """{"controllerId":-1,"controllerStatus":"NA"}""");

        Assert.Multiple(() =>
        {
            Assert.That(status.ControllerId, Is.EqualTo(-1L));
            Assert.That(status.ControllerStatus, Is.EqualTo(ExtractionControllerStatus.Na));
        });
    }

    [Test]
    public void ArchiveStatus_MissingFields_ReadAsNullOrUnknown()
    {
        var status = DeserializeStatus(/*lang=json,strict*/ """{"archiveId":"a"}""");

        Assert.Multiple(() =>
        {
            Assert.That(status.ControllerId, Is.Null);
            Assert.That(status.States, Is.Null);
            Assert.That(status.Type, Is.Null);
            Assert.That(status.ControllerStatus, Is.EqualTo(ExtractionControllerStatus.Unknown));
        });
    }

    [Test]
    public void ArchiveSettings_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"archiveId":"a1","autoExtract":true,"extractPath":"/data/out","finalPassword":"fp","passwords":["p1","p2"],"removeDownloadLinksAfterExtraction":false,"removeFilesAfterExtraction":true,"extra":1}
            """;

        var settings = DeserializeSettings(payload);

        Assert.Multiple(() =>
        {
            Assert.That(settings.ArchiveId, Is.EqualTo("a1"));
            Assert.That(settings.AutoExtract, Is.True);
            Assert.That(settings.ExtractPath, Is.EqualTo("/data/out"));
            Assert.That(settings.FinalPassword, Is.EqualTo("fp"));
            Assert.That(settings.Passwords, Is.EqualTo(_expectedPasswords));
            Assert.That(settings.RemoveDownloadLinksAfterExtraction, Is.False);
            Assert.That(settings.RemoveFilesAfterExtraction, Is.True);
        });
    }

    [Test]
    public void ArchiveSettings_Serialize_OmitsNullMembers()
    {
        var settings = new ArchiveSettings { AutoExtract = false, ExtractPath = "/out" };

        var json = JsonSerializer.Serialize(settings, ExtractionJsonContext.Default.ArchiveSettings);

        Assert.That(json, Is.EqualTo(/*lang=json,strict*/ """{"autoExtract":false,"extractPath":"/out"}"""));
    }

    [Test]
    public void DictionaryStringNullableBoolean_NullValue_Deserializes()
    {
        var map = JsonSerializer.Deserialize(
            /*lang=json,strict*/ """{"a":true,"b":false,"c":null}""",
            ExtractionJsonContext.Default.DictionaryStringNullableBoolean);

        Assert.Multiple(() =>
        {
            Assert.That(map!["a"], Is.True);
            Assert.That(map["b"], Is.False);
            Assert.That(map["c"], Is.Null);
        });
    }

    [Test]
    public void ArchiveSettings_ToString_MasksSecrets()
    {
        var text = new ArchiveSettings
        {
            ArchiveId = "a1",
            FinalPassword = "s3cretFinal",
            Passwords = ["s3cretOne", "s3cretTwo"],
        }.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("FinalPassword = ***"));
            Assert.That(text, Does.Not.Contain("s3cretFinal"));
            Assert.That(text, Does.Not.Contain("s3cretOne"));
            Assert.That(text, Does.Not.Contain("s3cretTwo"));
            Assert.That(text, Does.Contain("Passwords = 2"));
        });
    }

    [Test]
    public void ArchiveSettings_ToStringWithoutSecrets_ShowsNoMask()
    {
        var text = new ArchiveSettings { ArchiveId = "a1", FinalPassword = null, Passwords = null }.ToString();

        Assert.That(text, Does.Not.Contain("***"));
    }
}
