namespace MyJdownloader.Plugins;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using MyJdownloader.Config;

/// <summary>A plugin and one of its settings, as listed by <c>/plugins/list</c>; fields the device does not return are <see langword="null"/>.</summary>
/// <remarks>
/// <see cref="Value"/> and <see cref="DefaultValue"/> may hold secrets (hoster credentials, tokens): do not log them.
/// <see cref="object.ToString"/> prints only their JSON kind, never their content. All text comes from the device and is untrusted.
/// </remarks>
public sealed record PluginInfo
{
    /// <summary>Gets the name of the configuration interface of the plugin.</summary>
    public string? InterfaceName { get; init; }

    /// <summary>Gets the display name of the plugin.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets the setting key.</summary>
    public string? Key { get; init; }

    /// <summary>Gets the Java class name of the plugin.</summary>
    public string? ClassName { get; init; }

    /// <summary>Gets the storage the setting belongs to.</summary>
    public string? Storage { get; init; }

    /// <summary>Gets the Java type name of the setting.</summary>
    public string? Type { get; init; }

    /// <summary>Gets the abstract type of the setting.</summary>
    public ConfigValueType? AbstractType { get; init; }

    /// <summary>Gets the documentation text.</summary>
    public string? Docs { get; init; }

    /// <summary>Gets the label of the enumeration type, when the setting is an enumeration.</summary>
    public string? EnumLabel { get; init; }

    /// <summary>Gets the URL pattern of the plugin, a regular expression defined on the device; <see langword="null"/> when not requested.</summary>
    public string? Pattern { get; init; }

    /// <summary>Gets the version of the plugin, or <see langword="null"/> when not requested.</summary>
    public string? Version { get; init; }

    /// <summary>Gets the current value; a <see langword="null"/> value is also read as <see langword="null"/>. May hold a secret.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public JsonElement? Value { get; init; }

    /// <summary>Gets the default value. May hold a secret.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public JsonElement? DefaultValue { get; init; }

    /// <summary>Gets the options of an enumeration setting, or <see langword="null"/> when not applicable.</summary>
    public IReadOnlyList<ConfigEnumOption>? EnumOptions { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("InterfaceName = ").Append(this.InterfaceName)
            .Append(", DisplayName = ").Append(this.DisplayName)
            .Append(", Key = ").Append(this.Key)
            .Append(", ClassName = ").Append(this.ClassName)
            .Append(", Storage = ").Append(this.Storage)
            .Append(", Type = ").Append(this.Type)
            .Append(", AbstractType = ").Append(this.AbstractType)
            .Append(", Docs = ").Append(this.Docs)
            .Append(", EnumLabel = ").Append(this.EnumLabel)
            .Append(", Pattern = ").Append(this.Pattern)
            .Append(", Version = ").Append(this.Version)
            .Append(", Value = ").Append(KindOf(this.Value))
            .Append(", DefaultValue = ").Append(KindOf(this.DefaultValue))
            .Append(", EnumOptions = ").Append(this.EnumOptions?.Count);
        return true;
    }

    private static string? KindOf(JsonElement? element) =>
        element is null ? null : "<" + element.Value.ValueKind + ">";
}
