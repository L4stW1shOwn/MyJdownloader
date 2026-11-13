namespace MyJdownloader.Config;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

/// <summary>An advanced configuration entry; fields not requested by the query are <see langword="null"/>.</summary>
/// <remarks>
/// <see cref="Value"/> and <see cref="DefaultValue"/> may hold secrets (proxy passwords, API keys): do not log them.
/// <see cref="object.ToString"/> prints only their JSON kind, never their content.
/// </remarks>
public sealed record ConfigEntry
{
    /// <summary>Gets the name of the configuration interface holding the entry.</summary>
    public string? InterfaceName { get; init; }

    /// <summary>Gets the storage the entry belongs to, or <see langword="null"/> for the default storage.</summary>
    public string? Storage { get; init; }

    /// <summary>Gets the entry key.</summary>
    public string? Key { get; init; }

    /// <summary>Gets the Java type name of the entry.</summary>
    public string? Type { get; init; }

    /// <summary>Gets the abstract type of the entry.</summary>
    public ConfigValueType? AbstractType { get; init; }

    /// <summary>Gets the documentation text.</summary>
    public string? Docs { get; init; }

    /// <summary>Gets the label of the enumeration type, when the entry is an enumeration.</summary>
    public string? EnumLabel { get; init; }

    /// <summary>Gets the current value, when requested; a <see langword="null"/> value is also read as <see langword="null"/>. May hold a secret.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public JsonElement? Value { get; init; }

    /// <summary>Gets the default value, when requested. May hold a secret.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public JsonElement? DefaultValue { get; init; }

    /// <summary>Gets the options of an enumeration entry, or <see langword="null"/> when not applicable or not requested.</summary>
    public IReadOnlyList<ConfigEnumOption>? EnumOptions { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("InterfaceName = ").Append(InterfaceName)
            .Append(", Storage = ").Append(Storage)
            .Append(", Key = ").Append(Key)
            .Append(", Type = ").Append(Type)
            .Append(", AbstractType = ").Append(AbstractType)
            .Append(", Docs = ").Append(Docs)
            .Append(", EnumLabel = ").Append(EnumLabel)
            .Append(", Value = ").Append(KindOf(Value))
            .Append(", DefaultValue = ").Append(KindOf(DefaultValue))
            .Append(", EnumOptions = ").Append(EnumOptions?.Count);
        return true;
    }

    private static string? KindOf(JsonElement? element) =>
        element is null ? null : "<" + element.Value.ValueKind + ">";
}
