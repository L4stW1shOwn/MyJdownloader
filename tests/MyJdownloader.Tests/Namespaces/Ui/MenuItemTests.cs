namespace MyJdownloader.Tests.Ui;

using MyJdownloader.Ui;

[TestFixture]
public sealed class MenuItemTests
{
    [Test]
    public void ToString_TwoChildren_PrintsCountNotListType()
    {
        var item = new MenuItem { Id = "x", Children = [new MenuItem(), new MenuItem()] };

        var text = item.ToString();

        Assert.That(text, Does.Contain("Children = 2"));
        Assert.That(text, Does.Not.Contain("System.Collections"));
    }

    [Test]
    public void ToString_DefaultItem_PrintsEmptyMembers()
    {
        var text = new MenuItem().ToString();

        Assert.That(text, Is.EqualTo("MenuItem { Type = Unknown, Id = , Name = , Icon = , Children = 0 }"));
    }
}
