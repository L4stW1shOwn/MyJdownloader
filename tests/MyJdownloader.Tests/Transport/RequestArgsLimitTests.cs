namespace MyJdownloader.Tests.Transport;

using System.Text.Json;
using MyJdownloader.Transport;

[TestFixture]
public sealed class RequestArgsLimitTests
{
    [Test]
    public void RawTextLength_NonAsciiObject_EqualsRawTextLength()
    {
        using var document = JsonDocument.Parse("{\"k\":\"é😀\"}");

        Assert.That(RequestArgsLimit.RawTextLength(document.RootElement), Is.EqualTo(document.RootElement.GetRawText().Length));
    }
}
