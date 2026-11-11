namespace MyJdownloader.Tests.Facades;

using MyJdownloader.Facades;

[TestFixture]
public sealed class FacadeGuardTests
{
    [TestCase(1_024)]
    [TestCase(32_767)]
    [TestCase(65_536)]
    public void ValidateText_AtMaxLength_DoesNotThrow(int max)
        => Assert.DoesNotThrow(() => FacadeGuard.ValidateText(new string('a', max), "p", max));

    [TestCase(1_024)]
    [TestCase(32_767)]
    [TestCase(65_536)]
    public void ValidateText_OverMaxLength_ThrowsWithParamName(int max)
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateText(new string('a', max + 1), "p", max));

        Assert.That(ex!.ParamName, Is.EqualTo("p"));
        Assert.That(ex.Message, Does.StartWith($"A text value must not exceed {max} characters."));
    }

    [Test]
    public void ValidateText_OverMaxLengthWithControlChars_ReportsLength()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateText(new string('\n', 11), "p", 10));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("p"));
            Assert.That(ex.Message, Does.StartWith("A text value must not exceed 10 characters."));
        });
    }

    [Test]
    public void ValidateText_SurrogatePairsCountTwoUnits()
    {
        var text = string.Concat(Enumerable.Repeat("\U0001F600", 6));

        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateText(text, "p", 11));

        Assert.That(ex!.ParamName, Is.EqualTo("p"));
        Assert.DoesNotThrow(() => FacadeGuard.ValidateText(text, "p", 12));
    }

    [Test]
    public void ValidateOptionalText_Null_DoesNotThrow()
        => Assert.DoesNotThrow(() => FacadeGuard.ValidateOptionalText(null, "p", 10));

    [Test]
    public void ValidateOptionalText_OverMax_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateOptionalText(new string('a', 11), "p", 10));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("p"));
            Assert.That(ex.Message, Does.StartWith("A text value must not exceed 10 characters."));
        });
    }

    [Test]
    public void ValidateUrlSafeToken_OverMax_ThrowsLengthMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateUrlSafeToken(new string('a', 11), "p", 10));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("p"));
            Assert.That(ex.Message, Does.StartWith("A text value must not exceed 10 characters."));
        });
    }

    [Test]
    public void ValidateHostName_OverIdentifierMax_ThrowsLengthMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateHostName(new string('a', 1_025), "p"));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("p"));
            Assert.That(ex.Message, Does.StartWith("A text value must not exceed 1024 characters."));
        });
    }

    [Test]
    public void Caps_Constants_HaveSpecifiedValues()
        => Assert.Multiple(() =>
        {
            Assert.That(FacadeGuard.MaxIdentifierLength, Is.EqualTo(1_024));
            Assert.That(FacadeGuard.MaxPathLength, Is.EqualTo(32_767));
            Assert.That(FacadeGuard.MaxLongTextLength, Is.EqualTo(65_536));
        });

    [Test]
    public void UrlDisplayTypeArg_Duplicate_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.UrlDisplayTypeArg([UrlDisplayType.Content, UrlDisplayType.Content], "p"));

        Assert.That(ex!.ParamName, Is.EqualTo("p"));
    }

    [Test]
    public void UrlDisplayTypeArg_Valid_SerializesWireNames()
    {
        var element = FacadeGuard.UrlDisplayTypeArg([UrlDisplayType.Content, UrlDisplayType.Origin], "p");

        Assert.That(element.GetRawText(), Is.EqualTo("[\"CONTENT\",\"ORIGIN\"]"));
    }

    [Test]
    public void Ids_NegativeElement_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => FacadeGuard.Ids([1, -987654], "p"));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.ParamName, Is.EqualTo("p"));
            Assert.That(ex.Message, Does.Not.Contain("987654"));
            Assert.That(ex.ActualValue, Is.Null);
        });
    }

    [Test]
    public void Ids_ZeroElement_IsAccepted()
        => Assert.That(FacadeGuard.Ids([0], "p").GetRawText(), Is.EqualTo("[0]"));

    [Test]
    public void Ids_EmptyList_IsAccepted()
        => Assert.That(FacadeGuard.Ids([], "p").GetRawText(), Is.EqualTo("[]"));

    [Test]
    public void IdArgs_NegativePackageId_ThrowsNamingPackageIds()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => FacadeGuard.IdArgs([], [-1]));

        Assert.That(ex!.ParamName, Is.EqualTo("packageIds"));
    }

    [Test]
    public void RequireNotEmpty_Empty_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.RequireNotEmpty([], "p"));

        Assert.That(ex!.ParamName, Is.EqualTo("p"));
    }
}
