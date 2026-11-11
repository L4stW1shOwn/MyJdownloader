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

        Assert.That(result, Does.Contain("\"jobId\": 4"));
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

    [TestCase("sessiontoken")]
    [TestCase("regaintoken")]
    [TestCase("email")]
    [TestCase("user")]
    [TestCase("username")]
    [TestCase("apiKey")]
    [TestCase("cookieValue")]
    [TestCase("authHeader")]
    [TestCase("loginName")]
    public void Mask_SecretKey_IsMasked(string key)
    {
        var result = Mask($$"""{"{{key}}":"distinctvalue"}""");

        Assert.That(result, Does.Not.Contain("distinctvalue"));
        Assert.That(result, Does.Contain("\"***\""));
    }

    [Test]
    public void Mask_EscapedKey_IsMasked()
    {
        var result = Mask("""{"pa\u0073s":"xval"}""");

        Assert.That(result, Does.Not.Contain("xval"));
    }

    [Test]
    public void Mask_ArrayRoot_IsMasked()
    {
        var result = Mask("""[{"token":"xval"}]""");

        Assert.That(result, Does.Not.Contain("xval"));
    }

    [Test]
    public void Mask_StringRootUrlWithQuery_IsStripped()
    {
        var result = Mask("\"https://h/a?t=1\"");

        Assert.That(result, Does.Contain("https://h/a?***"));
        Assert.That(result, Does.Not.Contain("t=1"));
    }

    [Test]
    public void Mask_UrlWithNonDefaultPort_KeepsPort()
    {
        var result = Mask("""{"link":"https://h:8443/a?q=1"}""");

        Assert.That(result, Does.Contain("h:8443"));
        Assert.That(result, Does.Not.Contain("q=1"));
    }

    [Test]
    public void Mask_UnparseableHttpUrl_IsReplaced()
    {
        var result = Mask("""{"link":"https://u:p@h:99999/x?t=1"}""");

        Assert.That(result, Does.Not.Contain("u:p"));
        Assert.That(result, Does.Not.Contain("t=1"));
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

    [Test]
    public void Mask_UrlAsPropertyName_IsStripped()
    {
        var result = Mask("""{"https://u:p@h/x?t=1#k":[1]}""");

        Assert.That(result, Does.Not.Contain("u:p"));
        Assert.That(result, Does.Not.Contain("t=1"));
        Assert.That(result, Does.Not.Contain("#k"));
    }

    [Test]
    public void Mask_UrlFragment_IsDropped()
    {
        var result = Mask("\"https://h/x#secret\"");

        Assert.That(result, Does.Not.Contain("secret"));
    }
}
