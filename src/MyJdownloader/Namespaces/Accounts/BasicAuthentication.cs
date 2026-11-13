namespace MyJdownloader.Accounts;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>A basic authentication entry (FTP or HTTP) stored on the device, used for reading and updating.</summary>
/// <remarks>
/// The device returns <see cref="Password"/> in clear, without any opt-out. Do not log or serialize this record:
/// <see cref="object.ToString"/> masks the password as <c>***</c>, but value equality includes it.
/// </remarks>
public sealed record BasicAuthentication
{
    /// <summary>Gets the entry identifier.</summary>
    public long Id { get; init; }

    /// <summary>Gets the protocol the entry applies to.</summary>
    public BasicAuthType Type { get; init; }

    /// <summary>Gets the host mask the entry applies to.</summary>
    public string? Hostmask { get; init; }

    /// <summary>Gets the user name.</summary>
    public string? Username { get; init; }

    /// <summary>Gets the password, returned in clear by the device; never log or serialize it.</summary>
    /// <remarks>A <see langword="null"/> value is not sent on update; the device behaviour in that case is unconfirmed until live tests.</remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string? Password { get; init; }

    /// <summary>Gets a value indicating whether the entry is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the creation date, or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? Created { get; init; }

    /// <summary>Gets the date of the last validation, or <see langword="null"/> when not set.</summary>
    public DateTimeOffset? LastValidated { get; init; }

    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Invoked by the compiler-generated ToString.")]
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Id = ").Append(Id)
            .Append(", Type = ").Append(Type)
            .Append(", Hostmask = ").Append(Hostmask)
            .Append(", Username = ").Append(Username)
            .Append(", Password = ").Append(Password is null ? null : "***")
            .Append(", Enabled = ").Append(Enabled)
            .Append(", Created = ").Append(Created)
            .Append(", LastValidated = ").Append(LastValidated);
        return true;
    }
}
