namespace MyJdownloader.Tests.Captcha;

using System.Text.Json;
using MyJdownloader.Captcha;

[TestFixture]
public sealed class CaptchaJsonTests
{
    private const string JobSample = /*lang=json,strict*/ """
        [{"id":42,"link":7,"hoster":"example.com","type":"RECAPTCHA_V2","captchaCategory":"cat","explain":"Type the text","created":1700000000000,"timeout":300000,"extra":1}]
        """;

    private static CaptchaJob DeserializeJob(string objectJson)
    {
        var jobs = JsonSerializer.Deserialize("[" + objectJson + "]", CaptchaJsonContext.Default.ListCaptchaJob);
        return jobs![0];
    }

    [Test]
    public void CaptchaJob_Sample_Deserializes()
    {
        var jobs = JsonSerializer.Deserialize(JobSample, CaptchaJsonContext.Default.ListCaptchaJob);

        var job = jobs!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(job.Id, Is.EqualTo(42L));
            Assert.That(job.LinkId, Is.EqualTo(7L));
            Assert.That(job.Hoster, Is.EqualTo("example.com"));
            Assert.That(job.Type, Is.EqualTo("RECAPTCHA_V2"));
            Assert.That(job.CaptchaCategory, Is.EqualTo("cat"));
            Assert.That(job.Explain, Is.EqualTo("Type the text"));
            Assert.That(job.Created, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000)));
            Assert.That(job.Timeout, Is.EqualTo(300000));
        });
    }

    [Test]
    public void CaptchaJob_MissingFields_ReadAsNull()
    {
        var job = DeserializeJob(/*lang=json,strict*/ """{"id":1}""");

        Assert.Multiple(() =>
        {
            Assert.That(job.Hoster, Is.Null);
            Assert.That(job.Type, Is.Null);
            Assert.That(job.Created, Is.Null);
            Assert.That(job.Timeout, Is.Null);
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CaptchaJob_NonPositiveCreated_ReadsNull(long created)
    {
        var job = DeserializeJob("{\"id\":1,\"created\":" + created + "}");

        Assert.That(job.Created, Is.Null);
    }

    [Test]
    public void CaptchaSkipRequest_BlockAllCaptchas_SerializesUpperSnake()
    {
        var json = JsonSerializer.Serialize(CaptchaSkipRequest.BlockAllCaptchas, CaptchaJsonContext.Default.CaptchaSkipRequest);

        Assert.That(json, Is.EqualTo("\"BLOCK_ALL_CAPTCHAS\""));
    }

    [Test]
    public void CaptchaSkipRequest_StopCurrentAction_SerializesUpperSnake()
    {
        var json = JsonSerializer.Serialize(CaptchaSkipRequest.StopCurrentAction, CaptchaJsonContext.Default.CaptchaSkipRequest);

        Assert.That(json, Is.EqualTo("\"STOP_CURRENT_ACTION\""));
    }

    [Test]
    public void CaptchaSkipRequest_UnknownName_ReadsUnknown()
    {
        var value = JsonSerializer.Deserialize("\"NEW_VALUE\"", CaptchaJsonContext.Default.CaptchaSkipRequest);

        Assert.That(value, Is.EqualTo(CaptchaSkipRequest.Unknown));
    }
}
