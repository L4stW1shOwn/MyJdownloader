namespace MyJdownloader.Tests.Transport;

using System.Text.Json;
using MyJdownloader.Transport;

[TestFixture]
public sealed class RequestArgsLimitTests
{
    [TestCase("\"hello\"", 5, TestName = "MinLength_PlainAsciiString_NeverExceedsExactLength")]
    [TestCase("\"\"", 0, TestName = "MinLength_EmptyString_NeverExceedsExactLength")]
    [TestCase("\"\\u0041\\u0041\\u0041\\u0041\\u0041\"", 5, TestName = "MinLength_EscapedLetters_NeverExceedsExactLength")]
    [TestCase("\"\\ud83d\\ude00\"", 2, TestName = "MinLength_EscapedSurrogatePair_NeverExceedsExactLength")]
    [TestCase("\"😀\"", 2, TestName = "MinLength_RawEmoji_NeverExceedsExactLength")]
    [TestCase("\"\\n\\t\\\"\"", 3, TestName = "MinLength_ShortEscapes_NeverExceedsExactLength")]
    [TestCase("\"é\"", 1, TestName = "MinLength_NonAsciiString_NeverExceedsExactLength")]
    [TestCase("12345", 5, TestName = "MinLength_Number_NeverExceedsExactLength")]
    [TestCase("true", 4, TestName = "MinLength_Boolean_NeverExceedsExactLength")]
#pragma warning disable JSON002 // Probable JSON string detected: the argument is deliberately a raw JSON text
    [TestCase("""{"k":"é"}""", 9, TestName = "MinLength_Object_NeverExceedsExactLength")]
#pragma warning restore JSON002
    [TestCase("[1,2,3]", 7, TestName = "MinLength_Array_NeverExceedsExactLength")]
    public void MinLength_Element_NeverExceedsExactLength(string json, int exactLength)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Multiple(() =>
        {
            Assert.That(exactLength, Is.EqualTo(Exact(document.RootElement)));
            Assert.That(RequestArgsLimit.MinLength(document.RootElement), Is.LessThanOrEqualTo(exactLength));
        });
    }

    [TestCase("\"\"")]
    [TestCase("1")]
    public void MinLength_TinyElement_IsZero(string json)
    {
        using var document = JsonDocument.Parse(json);

        Assert.That(RequestArgsLimit.MinLength(document.RootElement), Is.Zero);
    }

    private static int Exact(JsonElement element)
        => element.ValueKind == JsonValueKind.String ? element.GetString()!.Length : element.GetRawText().Length;

    [Test]
    public void RawTextLength_NonAsciiObject_EqualsRawTextLength()
    {
        using var document = JsonDocument.Parse("{\"k\":\"é😀\"}");

        Assert.That(RequestArgsLimit.RawTextLength(document.RootElement), Is.EqualTo(document.RootElement.GetRawText().Length));
    }
}
