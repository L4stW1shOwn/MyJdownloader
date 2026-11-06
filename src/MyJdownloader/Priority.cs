namespace MyJdownloader;

using System.Text.Json.Serialization;
using MyJdownloader.Json;

/// <summary>Download priority of a link or package, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values returned by a device are read as <see cref="Unknown"/>; <see cref="Unknown"/> cannot be sent.</remarks>
[JsonConverter(typeof(TolerantEnumConverter<Priority>))]
public enum Priority
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>Highest priority (<c>HIGHEST</c>).</summary>
    Highest,

    /// <summary>Higher priority (<c>HIGHER</c>).</summary>
    Higher,

    /// <summary>High priority (<c>HIGH</c>).</summary>
    High,

    /// <summary>Default priority (<c>DEFAULT</c>).</summary>
    Default,

    /// <summary>Low priority (<c>LOW</c>).</summary>
    Low,

    /// <summary>Lower priority (<c>LOWER</c>).</summary>
    Lower,

    /// <summary>Lowest priority (<c>LOWEST</c>).</summary>
    Lowest,
}
