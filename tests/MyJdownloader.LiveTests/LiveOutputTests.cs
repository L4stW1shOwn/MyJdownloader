namespace MyJdownloader.LiveTests;

using System.Text.Json;

#pragma warning disable JSON002 // Probable JSON string: intentional payloads used as plain string vectors

[TestFixture]
public sealed class LiveOutputTests
{
    private static string Mask(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return LiveOutput.Mask(doc.RootElement);
    }

    [Test]
    public void Mask_TopLevelPassword_IsMasked()
    {
        var result = Mask("""{"password":"x","name":"n"}""");

        Assert.That(result, Does.Contain("\"***\""));
        Assert.That(result, Does.Not.Contain("\"x\""));
        Assert.That(result, Does.Contain("\"n\""));
    }

    [Test]
    public void Mask_NestedAndArray_AreMasked()
    {
        var result = Mask("""{"a":[{"downloadPassword":"x1"}],"b":{"token":"x2"}}""");

        Assert.That(result, Does.Not.Contain("x1"));
        Assert.That(result, Does.Not.Contain("x2"));
    }

    [Test]
    public void Mask_CaseInsensitiveKey_IsMasked()
    {
        var result = Mask("""{"Password":"xval","DOWNLOADPASSWORD":"yval"}""");

        Assert.That(result, Does.Not.Contain("xval"));
        Assert.That(result, Does.Not.Contain("yval"));
    }

    [Test]
    public void Mask_NonStringSecret_IsMasked()
    {
        var result = Mask("""{"pass":12345}""");

        Assert.That(result, Does.Not.Contain("12345"));
    }

    [Test]
    public void Mask_OtherKeys_AreKept()
    {
        var result = Mask("""{"jobId":4,"name":"n"}""");

        Assert.That(result, Does.Contain("4"));
        Assert.That(result, Does.Contain("\"n\""));
    }

    [Test]
    public void Mask_ArrayValueOfSecretKey_IsReplaced()
    {
        var result = Mask("""{"password":["a"]}""");

        Assert.That(result, Does.Not.Contain("\"a\""));
        Assert.That(result, Does.Contain("\"password\": \"***\""));
    }

    [Test]
    public void Mask_ObjectValueOfSecretKey_IsReplaced()
    {
        var result = Mask("""{"token":{"x":"y"}}""");

        Assert.That(result, Does.Not.Contain("\"y\""));
        Assert.That(result, Does.Contain("\"token\": \"***\""));
    }

    [Test]
    public void Mask_SessionKeys_AreMasked()
    {
        var result = Mask("""
            {"sessiontoken":"v1s","regaintoken":"v2r","email":"v3e","user":"v4u","username":"v5n"}
            """);

        foreach (var value in new[] { "v1s", "v2r", "v3e", "v4u", "v5n" })
        {
            Assert.That(result, Does.Not.Contain(value));
        }
    }

    [Test]
    public void Mask_SubstringKeys_AreMasked()
    {
        var result = Mask("""
            {"apiKey":"w1a","cookieValue":"w2c","authHeader":"w3h","loginName":"w4l"}
            """);

        foreach (var value in new[] { "w1a", "w2c", "w3h", "w4l" })
        {
            Assert.That(result, Does.Not.Contain(value));
        }
    }

    [Test]
    public void Mask_UrlWithUserInfoAndQuery_IsStripped()
    {
        var result = Mask("""{"link":"https://u:p@h/x?t=1"}""");

        Assert.That(result, Does.Contain("https://***@h/x?***"));
        Assert.That(result, Does.Not.Contain("u:p"));
        Assert.That(result, Does.Not.Contain("t=1"));
    }

    [Test]
    public void Mask_PlainUrl_IsKept()
    {
        var result = Mask("""{"link":"https://example.com/a"}""");

        Assert.That(result, Does.Contain("https://example.com/a"));
    }
}
