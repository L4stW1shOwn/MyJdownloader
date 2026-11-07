namespace MyJdownloader.Events;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>An event received from the device by listening to a subscription.</summary>
/// <remarks>
/// The shape of the <c>listen</c> items is undocumented (assumption to confirm by live tests); property names are read
/// case-insensitively. <see cref="Data"/> is untyped and may hold anything, including secrets: do not log it.
/// <see cref="Publisher"/> and <see cref="EventId"/> are device-controlled strings: sanitize or encode them before logging or rendering.
/// <see cref="object.ToString"/> prints only its JSON kind, never its content.
/// </remarks>
public sealed record DeviceEvent
{
    /// <summary>Gets the name of the publisher that emitted the event.</summary>
    public string? Publisher { get; init; }

    /// <summary>Gets the event identifier.</summary>
    [JsonPropertyName("eventid")]
    public string? EventId { get; init; }

    /// <summary>Gets the event payload, or <see langword="null"/> when absent or JSON <c>null</c>. May hold a secret.</summary>
    [JsonPropertyName("eventData")]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public JsonElement? Data { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Publisher = ").Append(this.Publisher)
            .Append(", EventId = ").Append(this.EventId)
            .Append(", Data = ").Append(this.Data is null ? null : "<" + this.Data.Value.ValueKind + ">");
        return true;
    }
}
