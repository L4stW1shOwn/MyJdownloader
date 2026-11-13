namespace MyJdownloader.Tests.Config;

using System.Text.Json;
using MyJdownloader.Config;

[TestFixture]
public sealed class ConfigJsonTests
{
    private static ConfigEntry DeserializeEntry(string objectJson)
    {
        var entries = JsonSerializer.Deserialize("[" + objectJson + "]", ConfigJsonContext.Default.ListConfigEntry);
        return entries![0];
    }

    [Test]
    public void ConfigEntry_Sample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """
            {"interfaceName":"org.jdownloader.settings.GeneralSettings","storage":null,"key":"DefaultDownloadFolder","type":"java.lang.String","abstractType":"STRING","docs":"d","enumLabel":null,"value":"/dl","defaultValue":"/home","extra":1}
            """;

        var entry = DeserializeEntry(payload);

        Assert.That(entry.InterfaceName, Is.EqualTo("org.jdownloader.settings.GeneralSettings"));
        Assert.That(entry.Storage, Is.Null);
        Assert.That(entry.Key, Is.EqualTo("DefaultDownloadFolder"));
        Assert.That(entry.Type, Is.EqualTo("java.lang.String"));
        Assert.That(entry.AbstractType, Is.EqualTo(ConfigValueType.String));
        Assert.That(entry.Docs, Is.EqualTo("d"));
        Assert.That(entry.EnumLabel, Is.Null);
        Assert.That(entry.Value!.Value.GetString(), Is.EqualTo("/dl"));
        Assert.That(entry.DefaultValue!.Value.GetString(), Is.EqualTo("/home"));
        Assert.That(entry.EnumOptions, Is.Null);
    }

    [Test]
    public void ConfigEntry_EnumOptions_ReadPairs()
    {
        const string payload = /*lang=json,strict*/ """
            {"abstractType":"ENUM","enumOptions":[["A","Label A"],["B",null],["C"]]}
            """;

        var entry = DeserializeEntry(payload);

        var options = entry.EnumOptions!;
        Assert.That(options, Has.Count.EqualTo(3));
        Assert.That(options[0], Is.EqualTo(new ConfigEnumOption { Name = "A", Label = "Label A" }));
        Assert.That(options[1], Is.EqualTo(new ConfigEnumOption { Name = "B", Label = null }));
        Assert.That(options[2], Is.EqualTo(new ConfigEnumOption { Name = "C", Label = null }));
    }

    [TestCase("""["A"]""")]
    [TestCase(/*lang=json,strict*/ """[[1,"x"]]""")]
    [TestCase("""[["A","B","C"]]""")]
    [TestCase(/*lang=json,strict*/ """{"a":1}""")]
    public void EnumOptionPairs_Malformed_ThrowsJsonException(string enumOptions)
    {
        var payload = "[{\"enumOptions\":" + enumOptions + "}]";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(payload, ConfigJsonContext.Default.ListConfigEntry));
    }

    [TestCase("SOMETHING_NEW")]
    [TestCase("UNKNOWN")]
    public void ConfigEntry_UnknownAbstractType_ReadsUnknown(string abstractType)
    {
        var entry = DeserializeEntry("{\"abstractType\":\"" + abstractType + "\"}");

        Assert.That(entry.AbstractType, Is.EqualTo(ConfigValueType.Unknown));
    }

    [Test]
    public void ConfigValueType_Names_UpperSnake()
    {
        var hexColorList = JsonSerializer.Serialize(ConfigValueType.HexColorList, ConfigJsonContext.Default.ConfigValueType);
        var objectList = JsonSerializer.Serialize(ConfigValueType.ObjectList, ConfigJsonContext.Default.ConfigValueType);
        var action = JsonSerializer.Serialize(ConfigValueType.Action, ConfigJsonContext.Default.ConfigValueType);

        Assert.That(hexColorList, Is.EqualTo("\"HEX_COLOR_LIST\""));
        Assert.That(objectList, Is.EqualTo("\"OBJECT_LIST\""));
        Assert.That(action, Is.EqualTo("\"ACTION\""));
    }

    [Test]
    public void ConfigEntry_NullValue_ReadsAsNull()
    {
        var entry = DeserializeEntry(/*lang=json,strict*/ """{"value":null}""");

        Assert.That(entry.Value, Is.Null);
    }

    [Test]
    public void ConfigEnumOption_ListEnumSample_Deserializes()
    {
        const string payload = /*lang=json,strict*/ """[{"name":"A","label":"Label A"}]""";

        var options = JsonSerializer.Deserialize(payload, ConfigJsonContext.Default.ListConfigEnumOption);

        Assert.That(options, Has.Count.EqualTo(1));
        Assert.That(options![0].Name, Is.EqualTo("A"));
        Assert.That(options[0].Label, Is.EqualTo("Label A"));
    }

    [Test]
    public void ConfigQuery_Default_Serializes()
    {
        var json = JsonSerializer.Serialize(new ConfigQuery(), ConfigJsonContext.Default.ConfigQuery);

        const string expected = /*lang=json,strict*/ """
            {"description":true,"defaultValues":true,"enumInfo":true,"includeExtensions":true,"values":false}
            """;
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void ConfigEntry_ToString_HidesValues()
    {
        var entry = new ConfigEntry
        {
            Value = JsonSerializer.Deserialize("\"secret\"", ConfigJsonContext.Default.JsonElement),
            DefaultValue = JsonSerializer.Deserialize("\"other\"", ConfigJsonContext.Default.JsonElement),
        };

        var text = entry.ToString();

        Assert.That(text, Does.Contain("<String>"));
        Assert.That(text, Does.Not.Contain("secret"));
        Assert.That(text, Does.Not.Contain("other"));
    }

    [Test]
    public void ConfigEntry_Equality_IgnoresToStringOverride()
    {
        var first = new ConfigEntry { InterfaceName = "i", Key = "k", AbstractType = ConfigValueType.Enum };
        var second = new ConfigEntry { InterfaceName = "i", Key = "k", AbstractType = ConfigValueType.Enum };

        Assert.That(first, Is.EqualTo(second));
    }
}
