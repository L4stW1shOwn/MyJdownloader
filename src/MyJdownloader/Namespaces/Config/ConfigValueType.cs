namespace MyJdownloader.Config;

using System.Diagnostics.CodeAnalysis;

/// <summary>The abstract type of an advanced configuration entry, as used by the JDownloader API.</summary>
/// <remarks>Unrecognized values, including the API's own <c>UNKNOWN</c>, are read as <see cref="Unknown"/>.</remarks>
[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Names mirror the API's abstract types.")]
public enum ConfigValueType
{
    /// <summary>The value is missing or not recognized by this library.</summary>
    Unknown = 0,

    /// <summary>A boolean (<c>BOOLEAN</c>).</summary>
    Boolean,

    /// <summary>A 32-bit integer (<c>INT</c>).</summary>
    Int,

    /// <summary>A 64-bit integer (<c>LONG</c>).</summary>
    Long,

    /// <summary>A string (<c>STRING</c>).</summary>
    String,

    /// <summary>An arbitrary object (<c>OBJECT</c>).</summary>
    Object,

    /// <summary>A list of objects (<c>OBJECT_LIST</c>).</summary>
    ObjectList,

    /// <summary>A list of strings (<c>STRING_LIST</c>).</summary>
    StringList,

    /// <summary>An enumeration value (<c>ENUM</c>).</summary>
    Enum,

    /// <summary>A byte (<c>BYTE</c>).</summary>
    Byte,

    /// <summary>A character (<c>CHAR</c>).</summary>
    Char,

    /// <summary>A double-precision number (<c>DOUBLE</c>).</summary>
    Double,

    /// <summary>A single-precision number (<c>FLOAT</c>).</summary>
    Float,

    /// <summary>A 16-bit integer (<c>SHORT</c>).</summary>
    Short,

    /// <summary>A list of booleans (<c>BOOLEAN_LIST</c>).</summary>
    BooleanList,

    /// <summary>A list of bytes (<c>BYTE_LIST</c>).</summary>
    ByteList,

    /// <summary>A list of 16-bit integers (<c>SHORT_LIST</c>).</summary>
    ShortList,

    /// <summary>A list of 64-bit integers (<c>LONG_LIST</c>).</summary>
    LongList,

    /// <summary>A list of 32-bit integers (<c>INT_LIST</c>).</summary>
    IntList,

    /// <summary>A list of single-precision numbers (<c>FLOAT_LIST</c>).</summary>
    FloatList,

    /// <summary>A list of enumeration values (<c>ENUM_LIST</c>).</summary>
    EnumList,

    /// <summary>A list of double-precision numbers (<c>DOUBLE_LIST</c>).</summary>
    DoubleList,

    /// <summary>A list of characters (<c>CHAR_LIST</c>).</summary>
    CharList,

    /// <summary>A hexadecimal color (<c>HEX_COLOR</c>).</summary>
    HexColor,

    /// <summary>A list of hexadecimal colors (<c>HEX_COLOR_LIST</c>).</summary>
    HexColorList,

    /// <summary>An action, triggered rather than read (<c>ACTION</c>).</summary>
    Action,
}
