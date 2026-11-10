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

        Assert.That(ex!.Message, Does.StartWith("A text value must not exceed 10 characters."));
    }

    [Test]
    public void ValidateText_SurrogatePairsCountTwoUnits()
    {
        var text = string.Concat(Enumerable.Repeat("\U0001F600", 6));

        Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateText(text, "p", 11));
        Assert.DoesNotThrow(() => FacadeGuard.ValidateText(text, "p", 12));
    }

    [Test]
    public void ValidateOptionalText_Null_DoesNotThrow()
        => Assert.DoesNotThrow(() => FacadeGuard.ValidateOptionalText(null, "p", 10));

    [Test]
    public void ValidateOptionalText_OverMax_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateOptionalText(new string('a', 11), "p", 10));

        Assert.That(ex!.Message, Does.StartWith("A text value must not exceed 10 characters."));
    }

    [Test]
    public void ValidateUrlSafeToken_OverMax_ThrowsLengthMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateUrlSafeToken(new string('a', 11), "p", 10));

        Assert.That(ex!.Message, Does.StartWith("A text value must not exceed 10 characters."));
    }

    [Test]
    public void ValidateHostName_OverIdentifierMax_ThrowsLengthMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => FacadeGuard.ValidateHostName(new string('a', 1_025), "p"));

        Assert.That(ex!.Message, Does.StartWith("A text value must not exceed 1024 characters."));
    }
}
