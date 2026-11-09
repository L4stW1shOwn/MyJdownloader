namespace MyJdownloader.ContentV2;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

/// <summary>A device-side icon description, as returned by the <c>/contentV2</c> namespace; absent fields are <see langword="null"/>.</summary>
/// <remarks>
/// <see cref="Prps"/> is untyped device data: <see cref="object.ToString"/> prints only its JSON kind, never its content.
/// All text comes from the device and is untrusted.
/// </remarks>
public sealed record IconDescriptor
{
    /// <summary>Gets the icon class name.</summary>
    public string? Cls { get; init; }

    /// <summary>Gets the icon key.</summary>
    public string? Key { get; init; }

    /// <summary>Gets the icon properties, as untyped JSON.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public JsonElement? Prps { get; init; }

    /// <summary>Gets the nested icon resources, or <see langword="null"/> when absent.</summary>
    public IReadOnlyList<IconDescriptor>? Rsc { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Cls = ").Append(this.Cls)
            .Append(", Key = ").Append(this.Key)
            .Append(", Prps = ").Append(this.Prps is null ? null : "<" + this.Prps.Value.ValueKind + ">")
            .Append(", Rsc = ").Append(this.Rsc?.Count);
        return true;
    }
}
