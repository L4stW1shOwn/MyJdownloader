namespace MyJdownloader.Tests.Dialogs;

using System.Text.Json;
using MyJdownloader.Dialogs;

[TestFixture]
public sealed class DialogsJsonTests
{
    [Test]
    public void DialogInfo_Sample_Deserializes()
    {
        var info = JsonSerializer.Deserialize(
            /*lang=json,strict*/ """{"type":"ConfirmDialog","properties":{"title":"Hello","message":"Sure?"},"extra":1}""",
            DialogsJsonContext.Default.DialogInfo);

        Assert.Multiple(() =>
        {
            Assert.That(info!.Type, Is.EqualTo("ConfirmDialog"));
            Assert.That(info.Properties!["title"], Is.EqualTo("Hello"));
            Assert.That(info.Properties["message"], Is.EqualTo("Sure?"));
        });
    }

    [Test]
    public void DialogInfo_MissingFields_ReadAsNull()
    {
        var info = JsonSerializer.Deserialize("{}", DialogsJsonContext.Default.DialogInfo);

        Assert.Multiple(() =>
        {
            Assert.That(info!.Type, Is.Null);
            Assert.That(info.Properties, Is.Null);
        });
    }

    [Test]
    public void DialogTypeInfo_Sample_Deserializes()
    {
        var info = JsonSerializer.Deserialize(
            /*lang=json,strict*/ """{"in":{"title":"String"},"out":{"ok":"boolean","text":"String"},"extra":1}""",
            DialogsJsonContext.Default.DialogTypeInfo);

        Assert.Multiple(() =>
        {
            Assert.That(info!.In!["title"], Is.EqualTo("String"));
            Assert.That(info.Out!.Count, Is.EqualTo(2));
            Assert.That(info.Out["ok"], Is.EqualTo("boolean"));
        });
    }

    [Test]
    public void DialogTypeInfo_MissingFields_ReadAsNull()
    {
        var info = JsonSerializer.Deserialize("{}", DialogsJsonContext.Default.DialogTypeInfo);

        Assert.Multiple(() =>
        {
            Assert.That(info!.In, Is.Null);
            Assert.That(info.Out, Is.Null);
        });
    }
}
