namespace MyJdownloader.Tests.Plugins;

using System.Text.Json;
using MyJdownloader.Config;
using MyJdownloader.Plugins;

[TestFixture]
public sealed class PluginsJsonTests
{
    private static PluginInfo DeserializeInfo(string objectJson)
    {
        var items = JsonSerializer.Deserialize("[" + objectJson + "]", PluginsJsonContext.Default.ListPluginInfo);
        return items![0];
    }

    private static PluginConfigEntry DeserializeEntry(string objectJson)
    {
        var items = JsonSerializer.Deserialize("[" + objectJson + "]", PluginsJsonContext.Default.ListPluginConfigEntry);
        return items![0];
    }

    [Test]
    public void PluginInfo_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"abstractType":"STRING","className":"jd.plugins.hoster.Foo","defaultValue":"d","displayName":"foo.com","docs":"doc","enumLabel":null,"enumOptions":null,"interfaceName":"jd.plugins.hoster.Foo$FooConfig","key":"Login","pattern":"https?://foo\\.com/.+","storage":"cfg/foo","type":"java.lang.String","value":"v","version":"42","extra":1}
            """;

        var info = DeserializeInfo(payload);

        Assert.Multiple(() =>
        {
            Assert.That(info.AbstractType, Is.EqualTo(ConfigValueType.String));
            Assert.That(info.ClassName, Is.EqualTo("jd.plugins.hoster.Foo"));
            Assert.That(info.DefaultValue!.Value.GetString(), Is.EqualTo("d"));
            Assert.That(info.DisplayName, Is.EqualTo("foo.com"));
            Assert.That(info.Docs, Is.EqualTo("doc"));
            Assert.That(info.EnumLabel, Is.Null);
            Assert.That(info.EnumOptions, Is.Null);
            Assert.That(info.InterfaceName, Is.EqualTo("jd.plugins.hoster.Foo$FooConfig"));
            Assert.That(info.Key, Is.EqualTo("Login"));
            Assert.That(info.Pattern, Is.EqualTo("https?://foo\\.com/.+"));
            Assert.That(info.Storage, Is.EqualTo("cfg/foo"));
            Assert.That(info.Type, Is.EqualTo("java.lang.String"));
            Assert.That(info.Value!.Value.GetString(), Is.EqualTo("v"));
            Assert.That(info.Version, Is.EqualTo("42"));
        });
    }

    [Test]
    public void PluginInfo_EnumOptions_ReadPairs()
    {
        const string payload = /*lang=json,strict*/ """
            {"abstractType":"ENUM","enumOptions":[["A","Label A"],["B",null]]}
            """;

        var options = DeserializeInfo(payload).EnumOptions!;

        Assert.That(options, Is.EqualTo(new List<ConfigEnumOption>
        {
            new() { Name = "A", Label = "Label A" },
            new() { Name = "B", Label = null },
        }));
    }

    [Test]
    public void PluginInfo_UnknownAbstractType_ReadsUnknown()
    {
        var info = DeserializeInfo(/*lang=json,strict*/ """{"abstractType":"SOMETHING_NEW"}""");

        Assert.That(info.AbstractType, Is.EqualTo(ConfigValueType.Unknown));
    }

    [Test]
    public void PluginInfo_NullValue_ReadsAsNull()
    {
        var info = DeserializeInfo(/*lang=json,strict*/ """{"value":null}""");

        Assert.That(info.Value, Is.Null);
    }

    [Test]
    public void PluginInfo_MalformedEnumOptions_ThrowsJsonException() =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(/*lang=json,strict*/ """[{"enumOptions":["A"]}]""", PluginsJsonContext.Default.ListPluginInfo));

    [Test]
    public void PluginInfo_ToString_HidesValues()
    {
        var info = new PluginInfo
        {
            Value = JsonSerializer.Deserialize("\"secret\"", PluginsJsonContext.Default.NullableJsonElement),
            DefaultValue = JsonSerializer.Deserialize("\"other\"", PluginsJsonContext.Default.NullableJsonElement),
        };

        var text = info.ToString();

        Assert.That(text, Does.Contain("<String>"));
        Assert.That(text, Does.Not.Contain("secret"));
        Assert.That(text, Does.Not.Contain("other"));
    }

    [Test]
    public void PluginInfo_Equality_IgnoresToStringOverride()
    {
        var first = new PluginInfo { InterfaceName = "i", DisplayName = "d", Key = "k" };
        var second = new PluginInfo { InterfaceName = "i", DisplayName = "d", Key = "k" };

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void PluginConfigEntry_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"abstractType":"BOOLEAN","defaultValue":false,"docs":"d","enumLabel":null,"enumOptions":null,"interfaceName":"org.Iface","key":"Key","storage":"cfg/x","type":"boolean","value":true,"extra":1}
            """;

        var entry = DeserializeEntry(payload);

        Assert.Multiple(() =>
        {
            Assert.That(entry.AbstractType, Is.EqualTo(ConfigValueType.Boolean));
            Assert.That(entry.DefaultValue!.Value.GetBoolean(), Is.False);
            Assert.That(entry.Docs, Is.EqualTo("d"));
            Assert.That(entry.InterfaceName, Is.EqualTo("org.Iface"));
            Assert.That(entry.Key, Is.EqualTo("Key"));
            Assert.That(entry.Storage, Is.EqualTo("cfg/x"));
            Assert.That(entry.Type, Is.EqualTo("boolean"));
            Assert.That(entry.Value!.Value.GetBoolean(), Is.True);
        });
    }

    [Test]
    public void PluginConfigEntry_EnumOptions_ReadPairs()
    {
        var entry = DeserializeEntry(/*lang=json,strict*/ """{"enumOptions":[["A","Label A"]]}""");

        Assert.That(entry.EnumOptions, Is.EqualTo(new List<ConfigEnumOption> { new() { Name = "A", Label = "Label A" } }));
    }

    [Test]
    public void PluginConfigEntry_ToString_HidesValues()
    {
        var entry = new PluginConfigEntry
        {
            Value = JsonSerializer.Deserialize("\"secret\"", PluginsJsonContext.Default.NullableJsonElement),
            DefaultValue = JsonSerializer.Deserialize("\"other\"", PluginsJsonContext.Default.NullableJsonElement),
        };

        var text = entry.ToString();

        Assert.That(text, Does.Contain("<String>"));
        Assert.That(text, Does.Not.Contain("secret"));
        Assert.That(text, Does.Not.Contain("other"));
    }

    [Test]
    public void PluginsQuery_Default_Serializes()
    {
        var json = JsonSerializer.Serialize(new PluginsQuery(), PluginsJsonContext.Default.PluginsQuery);

        Assert.That(json, Is.EqualTo(/*lang=json,strict*/ """{"pattern":true,"version":true}"""));
    }

    [Test]
    public void ConfigQuery_Default_SerializesLikeConfigNamespace()
    {
        var json = JsonSerializer.Serialize(new ConfigQuery(), PluginsJsonContext.Default.ConfigQuery);

        Assert.That(json, Is.EqualTo(/*lang=json,strict*/ """{"description":true,"defaultValues":true,"enumInfo":true,"includeExtensions":true,"values":false}"""));
    }

    [Test]
    public void RegexMap_Sample_Deserializes()
    {
        var map = JsonSerializer.Deserialize(/*lang=json,strict*/ """{"foo":["a","b"],"bar":[]}""", PluginsJsonContext.Default.DictionaryStringListString);

        Assert.That(map!["foo"], Is.EqualTo(new List<string> { "a", "b" }));
    }
}
