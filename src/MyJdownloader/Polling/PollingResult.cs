namespace MyJdownloader.Polling;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

/// <summary>One event returned by <c>/polling/poll</c>.</summary>
/// <remarks>All content comes from the device and is untrusted. <see cref="object.ToString"/> prints only the JSON kind of <see cref="EventData"/>, never its content.</remarks>
public sealed record PollingResult
{
    /// <summary>Gets the event name: <c>downloadProgress</c>, <c>jdState</c>, <c>linkGrabberState</c>, <c>captchasWaiting</c> or <c>aggregatedNumbers</c>.</summary>
    public string? EventName { get; init; }

    /// <summary>Gets the raw event data object; the payload is under its <c>data</c> property.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public JsonElement? EventData { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("EventName = ").Append(this.EventName)
            .Append(", EventData = ").Append(this.EventData is null ? null : "<" + this.EventData.Value.ValueKind + ">");
        return true;
    }
}
