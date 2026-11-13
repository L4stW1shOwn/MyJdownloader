namespace MyJdownloader.Tests.ContentV2;

using System.Globalization;
using System.Text.Json;
using MyJdownloader.ContentV2;
using MyJdownloader.Errors;
using MyJdownloader.Tests.Transport;

[TestFixture]
public sealed class ContentV2JsonTests
{
    private static readonly TimeSpan _waitTimeout = TimeSpan.FromSeconds(5);

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    private static IconDescriptor Read(string json) =>
        JsonSerializer.Deserialize(json, ContentV2JsonContext.Default.IconDescriptor)!;

    [Test]
    public void IconDescriptor_Read_MapsNestedRscAndPrps()
    {
        var icon = Read(/*lang=json,strict*/ """{"cls":"Badge","rsc":[{"key":"a"},{"key":"b","prps":{"width":16}}]}""");

        Assert.Multiple(() =>
        {
            Assert.That(icon.Cls, Is.EqualTo("Badge"));
            Assert.That(icon.Rsc, Has.Count.EqualTo(2));
            Assert.That(icon.Rsc![0].Key, Is.EqualTo("a"));
            Assert.That(icon.Rsc[1].Prps!.Value.GetProperty("width").GetInt32(), Is.EqualTo(16));
        });
    }

    [Test]
    public void IconDescriptor_Read_SkipsUnknownFields()
    {
        var icon = Read(/*lang=json,strict*/ """{"cls":"Badge","extra":{"x":1},"key":"k"}""");

        Assert.Multiple(() =>
        {
            Assert.That(icon.Cls, Is.EqualTo("Badge"));
            Assert.That(icon.Key, Is.EqualTo("k"));
        });
    }

    [Test]
    public void IconDescriptor_ToString_HidesPrps()
    {
        var icon = Read(/*lang=json,strict*/ """{"key":"k","prps":{"width":16}}""");

        var text = icon.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("k"));
            Assert.That(text, Does.Not.Contain("width"));
        });
    }

    [Test]
    public async Task IconDescriptor_TooDeep_ThrowsProtocolException()
    {
        const int depth = 100;
        var nested = string.Concat(Enumerable.Repeat("{\"rsc\":[", depth)) + "{}" + string.Concat(Enumerable.Repeat("]}", depth));
#pragma warning disable CA2000 // The fake handler is owned by the HttpClient of the test
        var server = new FakeMyJdServer(request => FakeMyJdServer.EncryptedJson(
            Key(4),
            string.Create(CultureInfo.InvariantCulture, $"{{\"data\":{nested},\"rid\":{FakeMyJdServer.BodyRidOf(request, Key(4))}}}")));
        var client = new MyJDownloaderClient(new HttpClient(server));
#pragma warning restore CA2000
        client.RestoreSession(new MyJdSession("user@example.com", Key(1), Key(2), "0011aabb", "ccdd2233", Key(3), Key(4)));
        var channel = client.CreateDeviceChannel("dev1");

        await Assert.ThrowsAsync<MyJdProtocolException>(
            () => channel.CallAsync("/contentV2/getIcon", ContentV2JsonContext.Default.IconDescriptor, []).WaitAsync(_waitTimeout));
    }
}
