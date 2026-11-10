namespace MyJdownloader.Tests.Ui;

using System.Text.Json;
using MyJdownloader.Ui;

[TestFixture]
public sealed class UiJsonTests
{
    [Test]
    public void MenuItem_NestedChildren_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"children":[{"children":[{"icon":"search","id":"checkonline","name":"Check online","type":"ACTION"}],"icon":"settings","id":"settings","name":"Settings","type":"CONTAINER"},{"icon":"enabled","id":"enable","name":"Enable","type":"LINK"}]}
            """;

        var root = JsonSerializer.Deserialize(payload, UiJsonContext.Default.MenuItem)!;

        Assert.Multiple(() =>
        {
            Assert.That(root.Id, Is.Null);
            Assert.That(root.Type, Is.EqualTo(MenuItemType.Unknown));
            Assert.That(root.Children, Has.Count.EqualTo(2));
            Assert.That(root.Children[0].Type, Is.EqualTo(MenuItemType.Container));
            Assert.That(root.Children[0].Children.Single().Id, Is.EqualTo("checkonline"));
            Assert.That(root.Children[0].Children.Single().Icon, Is.EqualTo("search"));
            Assert.That(root.Children[0].Children.Single().Name, Is.EqualTo("Check online"));
            Assert.That(root.Children[0].Children.Single().Type, Is.EqualTo(MenuItemType.Action));
            Assert.That(root.Children[1].Type, Is.EqualTo(MenuItemType.Link));
        });
    }

    [Test]
    public void MenuItem_UnknownType_ReadsAsUnknown()
    {
        var item = JsonSerializer.Deserialize(/*lang=json,strict*/ """{"id":"x","type":"SOMETHING_NEW"}""", UiJsonContext.Default.MenuItem)!;

        Assert.That(item.Type, Is.EqualTo(MenuItemType.Unknown));
    }

    [Test]
    public void MenuItem_NullChildren_ReadsAsEmpty()
    {
        var item = JsonSerializer.Deserialize(/*lang=json,strict*/ """{"id":"x","children":null}""", UiJsonContext.Default.MenuItem)!;

        Assert.That(item.Children, Is.Empty);
    }

    [Test]
    public void MenuItem_AbsentChildren_ReadsAsEmpty()
    {
        var item = JsonSerializer.Deserialize(/*lang=json,strict*/ """{"id":"x"}""", UiJsonContext.Default.MenuItem)!;

        Assert.That(item.Children, Is.Empty);
    }

    [Test]
    public void MenuContext_Value_SerializesAsWireName()
        => Assert.Multiple(() =>
        {
            Assert.That(JsonSerializer.Serialize(MenuContext.Dlc, UiJsonContext.Default.MenuContext), Is.EqualTo("\"DLC\""));
            Assert.That(JsonSerializer.Serialize(MenuContext.Lgc, UiJsonContext.Default.MenuContext), Is.EqualTo("\"LGC\""));
        });
}
