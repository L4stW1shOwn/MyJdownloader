namespace MyJdownloader.Tests.Accounts;

using System.Text.Json;
using MyJdownloader.Accounts;

[TestFixture]
public sealed class AccountsJsonTests
{
    private static Account DeserializeAccount(string objectJson)
    {
        var accounts = JsonSerializer.Deserialize("[" + objectJson + "]", AccountsJsonContext.Default.ListAccount);
        return accounts![0];
    }

    private static BasicAuthentication DeserializeBasicAuth(string objectJson)
    {
        var entries = JsonSerializer.Deserialize("[" + objectJson + "]", AccountsJsonContext.Default.ListBasicAuthentication);
        return entries![0];
    }

    [Test]
    public void Account_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"uuid":42,"hostname":"rapidgator.net","username":"u","enabled":true,"valid":false,"errorType":"INVALID","errorString":"bad","trafficLeft":1000,"trafficMax":2000,"validUntil":1700000000000,"extra":1}
            """;

        var account = DeserializeAccount(payload);

        Assert.That(account.Uuid, Is.EqualTo(42L));
        Assert.That(account.Hostname, Is.EqualTo("rapidgator.net"));
        Assert.That(account.Username, Is.EqualTo("u"));
        Assert.That(account.Enabled, Is.True);
        Assert.That(account.Valid, Is.False);
        Assert.That(account.ErrorType, Is.EqualTo("INVALID"));
        Assert.That(account.ErrorString, Is.EqualTo("bad"));
        Assert.That(account.TrafficLeft, Is.EqualTo(1000L));
        Assert.That(account.TrafficMax, Is.EqualTo(2000L));
        Assert.That(account.ValidUntil, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000)));
    }

    [Test]
    public void Account_MissingFields_ReadAsNull()
    {
        var account = DeserializeAccount(/*lang=json,strict*/ """{"uuid":1}""");

        Assert.That(account.Enabled, Is.Null);
        Assert.That(account.Valid, Is.Null);
        Assert.That(account.TrafficLeft, Is.Null);
        Assert.That(account.ValidUntil, Is.Null);
    }

    [TestCase(-1)]
    [TestCase(0)]
    public void Account_NonPositiveValidUntil_ReadsNull(long validUntil)
    {
        var account = DeserializeAccount("{\"uuid\":1,\"validUntil\":" + validUntil + "}");

        Assert.That(account.ValidUntil, Is.Null);
    }

    [Test]
    public void BasicAuthentication_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"id":7,"type":"FTP","hostmask":"ftp.example.com","username":"u","password":"p","enabled":true,"created":1700000000000,"lastValidated":0}
            """;

        var entry = DeserializeBasicAuth(payload);

        Assert.That(entry.Id, Is.EqualTo(7L));
        Assert.That(entry.Type, Is.EqualTo(BasicAuthType.Ftp));
        Assert.That(entry.Hostmask, Is.EqualTo("ftp.example.com"));
        Assert.That(entry.Username, Is.EqualTo("u"));
        Assert.That(entry.Password, Is.EqualTo("p"));
        Assert.That(entry.Enabled, Is.True);
        Assert.That(entry.Created, Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000)));
        Assert.That(entry.LastValidated, Is.Null);
    }

    [Test]
    public void BasicAuthentication_UnknownType_ReadsUnknown()
    {
        var entry = DeserializeBasicAuth(/*lang=json,strict*/ """{"type":"SMB"}""");

        Assert.That(entry.Type, Is.EqualTo(BasicAuthType.Unknown));
    }

    [Test]
    public void BasicAuthType_Names_Upper()
    {
        var ftp = JsonSerializer.Serialize(BasicAuthType.Ftp, AccountsJsonContext.Default.BasicAuthType);
        var http = JsonSerializer.Serialize(BasicAuthType.Http, AccountsJsonContext.Default.BasicAuthType);

        Assert.That(ftp, Is.EqualTo("\"FTP\""));
        Assert.That(http, Is.EqualTo("\"HTTP\""));
    }

    [Test]
    public void AccountQuery_Default_Serializes()
    {
        var json = JsonSerializer.Serialize(new AccountQuery(), AccountsJsonContext.Default.AccountQuery);

        const string expected = /*lang=json,strict*/ """
            {"enabled":true,"error":true,"trafficLeft":true,"trafficMax":true,"userName":true,"valid":true,"validUntil":true,"maxResults":-1,"startAt":0}
            """;
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void AccountQuery_Uuids_SerializedAsUuidlist()
    {
        var query = new AccountQuery { Uuids = [1, 2] };

        var json = JsonSerializer.Serialize(query, AccountsJsonContext.Default.AccountQuery);

        Assert.That(json, Does.Contain("\"uuidlist\":[1,2]"));
    }

    [Test]
    public void BasicAuthentication_Serialize_WritesEpochMilliseconds()
    {
        var entry = new BasicAuthentication
        {
            Type = BasicAuthType.Http,
            Created = DateTimeOffset.FromUnixTimeMilliseconds(1700000000000),
            LastValidated = null,
        };

        var json = JsonSerializer.Serialize(entry, AccountsJsonContext.Default.BasicAuthentication);

        Assert.That(json, Does.Contain("\"created\":1700000000000"));
        Assert.That(json, Does.Contain("\"type\":\"HTTP\""));
        Assert.That(json, Does.Not.Contain("lastValidated"));
    }

    [Test]
    public void BasicAuthentication_ToString_MasksPassword()
    {
        var withPassword = new BasicAuthentication { Password = "s3cret" }.ToString();
        var withoutPassword = new BasicAuthentication { Password = null }.ToString();

        Assert.That(withPassword, Does.Contain("Password = ***"));
        Assert.That(withPassword, Does.Not.Contain("s3cret"));
        Assert.That(withoutPassword, Does.Not.Contain("***"));
    }

    [Test]
    public void BasicAuthentication_Equality_IncludesPassword()
    {
        var first = new BasicAuthentication { Id = 1, Type = BasicAuthType.Ftp, Hostmask = "h", Username = "u", Password = "a" };
        var same = new BasicAuthentication { Id = 1, Type = BasicAuthType.Ftp, Hostmask = "h", Username = "u", Password = "a" };
        var different = new BasicAuthentication { Id = 1, Type = BasicAuthType.Ftp, Hostmask = "h", Username = "u", Password = "b" };

        Assert.That(first, Is.EqualTo(same));
        Assert.That(first, Is.Not.EqualTo(different));
    }
}
