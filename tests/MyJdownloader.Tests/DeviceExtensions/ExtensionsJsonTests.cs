namespace MyJdownloader.Tests.DeviceExtensions;

using System.Text.Json;
using MyJdownloader.DeviceExtensions;

[TestFixture]
public sealed class ExtensionsJsonTests
{
    private const string Sample = /*lang=json,strict*/ """
        [{"id":"org.jdownloader.extensions.extraction.ExtractionExtension","name":"Archive Extractor","description":"d","configInterface":"org.x.Config","iconKey":"archive","enabled":true,"installed":true,"extra":1}]
        """;

    [Test]
    public void Extension_Sample_Deserializes()
    {
        var list = JsonSerializer.Deserialize(Sample, ExtensionsJsonContext.Default.ListExtension);

        var extension = list!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(extension.Id, Is.EqualTo("org.jdownloader.extensions.extraction.ExtractionExtension"));
            Assert.That(extension.Name, Is.EqualTo("Archive Extractor"));
            Assert.That(extension.Description, Is.EqualTo("d"));
            Assert.That(extension.ConfigInterface, Is.EqualTo("org.x.Config"));
            Assert.That(extension.IconKey, Is.EqualTo("archive"));
            Assert.That(extension.Enabled, Is.True);
            Assert.That(extension.Installed, Is.True);
        });
    }

    [Test]
    public void Extension_MissingFlags_ReadAsNull()
    {
        var list = JsonSerializer.Deserialize(/*lang=json,strict*/ """[{"id":"a"}]""", ExtensionsJsonContext.Default.ListExtension);

        var extension = list!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(extension.Enabled, Is.Null);
            Assert.That(extension.Installed, Is.Null);
            Assert.That(extension.Name, Is.Null);
        });
    }

    [Test]
    public void ExtensionQuery_Default_SerializesAllFlagsWithoutPattern()
    {
        var json = JsonSerializer.Serialize(new ExtensionQuery(), ExtensionsJsonContext.Default.ExtensionQuery);

        Assert.That(
            json,
            Is.EqualTo(/*lang=json,strict*/ """{"configInterface":true,"description":true,"enabled":true,"iconKey":true,"installed":true,"name":true}"""));
    }

    [Test]
    public void ExtensionQuery_Pattern_SerializesPattern()
    {
        var json = JsonSerializer.Serialize(new ExtensionQuery { Pattern = ".*extract.*" }, ExtensionsJsonContext.Default.ExtensionQuery);

        Assert.That(json, Does.Contain("\"pattern\":\".*extract.*\""));
    }
}
