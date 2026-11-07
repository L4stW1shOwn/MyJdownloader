namespace MyJdownloader.Accounts;

using System.Text.Json.Serialization;

/// <summary>Selects the accounts and fields returned by <c>/accountsV2/listAccounts</c>; every field is included by default.</summary>
public sealed record AccountQuery
{
    /// <summary>Gets a value indicating whether the enabled flag is returned.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Gets a value indicating whether the error type and message are returned.</summary>
    public bool Error { get; init; } = true;

    /// <summary>Gets a value indicating whether the remaining traffic is returned.</summary>
    public bool TrafficLeft { get; init; } = true;

    /// <summary>Gets a value indicating whether the maximum traffic is returned.</summary>
    public bool TrafficMax { get; init; } = true;

    /// <summary>Gets a value indicating whether the user name is returned.</summary>
    public bool UserName { get; init; } = true;

    /// <summary>Gets a value indicating whether the validity flag is returned.</summary>
    public bool Valid { get; init; } = true;

    /// <summary>Gets a value indicating whether the expiry date is returned.</summary>
    public bool ValidUntil { get; init; } = true;

    /// <summary>Gets the maximum number of accounts to return; <c>-1</c> (the default) means no limit.</summary>
    public int MaxResults { get; init; } = -1;

    /// <summary>Gets the zero-based index of the first account to return.</summary>
    public int StartAt { get; init; }

    /// <summary>Gets the identifiers of the accounts to return, or <see langword="null"/> for all accounts.</summary>
    [JsonPropertyName("uuidlist")]
    public IReadOnlyList<long>? Uuids { get; init; }
}
