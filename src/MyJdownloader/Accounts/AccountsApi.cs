namespace MyJdownloader.Accounts;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/accountsV2</c> namespace of one device (premium hoster accounts and basic authentication entries).</summary>
/// <remarks>
/// This namespace handles third-party credentials. Passwords are sent as call arguments and are never retained, logged or printed by the facade;
/// do not log them either. Basic authentication passwords are returned by the device in clear, see <see cref="ListBasicAuthAsync"/>.
/// </remarks>
public sealed class AccountsApi
{
    private const string Base = "/accountsV2/";

    private readonly IDeviceChannel _channel;

    internal AccountsApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Adds a premium hoster account (<c>/accountsV2/addAccount</c>).</summary>
    /// <param name="premiumHoster">The premium hoster name.</param>
    /// <param name="username">The account user name.</param>
    /// <param name="password">The account password; sent unchanged and never inspected.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>
    /// This method transmits credentials to the device. A timeout or a network error does not mean the action failed: the account may have been changed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="premiumHoster"/>, <paramref name="username"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="premiumHoster"/> or <paramref name="username"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task AddAccountAsync(string premiumHoster, string username, string password, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(premiumHoster, nameof(premiumHoster));
        FacadeGuard.ValidateText(username, nameof(username));
        ArgumentNullException.ThrowIfNull(password);
        return this._channel.CallAsync(Base + "addAccount", [Str(premiumHoster), Str(username), Str(password)], cancellationToken);
    }

    /// <summary>Lists the premium hoster accounts (<c>/accountsV2/listAccounts</c>).</summary>
    /// <param name="query">The accounts and fields to return, or <see langword="null"/> for all accounts with all fields.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The accounts; empty when the device returns no data.</returns>
    /// <exception cref="ArgumentException"><see cref="AccountQuery.MaxResults"/> is less than -1 or <see cref="AccountQuery.StartAt"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<Account>> ListAccountsAsync(AccountQuery? query = null, CancellationToken cancellationToken = default)
    {
        query ??= new AccountQuery();
        FacadeGuard.ValidateRange(query.MaxResults, query.StartAt, nameof(query));
        var args = new[] { JsonSerializer.SerializeToElement(query, AccountsJsonContext.Default.AccountQuery) };
        return FacadeResults.ListAsync(this._channel.CallAsync(Base + "listAccounts", AccountsJsonContext.Default.ListAccount, args, cancellationToken));
    }

    /// <summary>Enables premium hoster accounts (<c>/accountsV2/enableAccounts</c>).</summary>
    /// <param name="ids">The identifiers of the accounts (<see cref="Account.Uuid"/>); an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task EnableAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "enableAccounts", [FacadeGuard.Ids(ids, nameof(ids))], cancellationToken);

    /// <summary>Disables premium hoster accounts (<c>/accountsV2/disableAccounts</c>).</summary>
    /// <param name="ids">The identifiers of the accounts (<see cref="Account.Uuid"/>); an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task DisableAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "disableAccounts", [FacadeGuard.Ids(ids, nameof(ids))], cancellationToken);

    /// <summary>Refreshes premium hoster accounts (<c>/accountsV2/refreshAccounts</c>).</summary>
    /// <param name="ids">The identifiers of the accounts (<see cref="Account.Uuid"/>); an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RefreshAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "refreshAccounts", [FacadeGuard.Ids(ids, nameof(ids))], cancellationToken);

    /// <summary>Removes premium hoster accounts (<c>/accountsV2/removeAccounts</c>).</summary>
    /// <param name="ids">The identifiers of the accounts (<see cref="Account.Uuid"/>); an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>
    /// The change is applied immediately, without confirmation.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task RemoveAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
        => this._channel.CallAsync(Base + "removeAccounts", [FacadeGuard.Ids(ids, nameof(ids))], cancellationToken);

    /// <summary>Changes the user name and password of an account (<c>/accountsV2/setUserNameAndPassword</c>).</summary>
    /// <param name="accountId">The account identifier (<see cref="Account.Uuid"/>).</param>
    /// <param name="username">The new user name.</param>
    /// <param name="password">The new password; sent unchanged and never inspected.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device applied the change.</returns>
    /// <remarks>
    /// This method transmits credentials to the device. A timeout or a network error does not mean the action failed: the account may have been changed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="username"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="username"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> SetUserNameAndPasswordAsync(long accountId, string username, string password, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(username, nameof(username));
        ArgumentNullException.ThrowIfNull(password);
        var args = new[] { JsonSerializer.SerializeToElement(accountId, FacadeJsonContext.Default.Int64), Str(username), Str(password) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "setUserNameAndPassword", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Lists the premium hosters supported by the device (<c>/accountsV2/listPremiumHoster</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The hoster names; empty when the device returns no data.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<string>> ListPremiumHostersAsync(CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(this._channel.CallAsync(Base + "listPremiumHoster", AccountsJsonContext.Default.ListString, [], cancellationToken));

    /// <summary>Lists the account page URL of each premium hoster (<c>/accountsV2/listPremiumHosterUrls</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The URLs keyed by hoster name; empty when the device returns no data.</returns>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyDictionary<string, string>> ListPremiumHosterUrlsAsync(CancellationToken cancellationToken = default)
        => ToUrlsAsync(this._channel.CallAsync(Base + "listPremiumHosterUrls", AccountsJsonContext.Default.DictionaryStringString, [], cancellationToken));

    /// <summary>Gets the account page URL of a premium hoster (<c>/accountsV2/getPremiumHosterUrl</c>).</summary>
    /// <param name="hoster">The hoster name.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The URL, or <see langword="null"/> when the device knows none for that hoster.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="hoster"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="hoster"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<string?> GetPremiumHosterUrlAsync(string hoster, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ValidateText(hoster, nameof(hoster));
        return this._channel.CallAsync(Base + "getPremiumHosterUrl", FacadeJsonContext.Default.String, [Str(hoster)], cancellationToken);
    }

    /// <summary>Adds a basic authentication entry (<c>/accountsV2/addBasicAuth</c>).</summary>
    /// <param name="type">The protocol of the entry; <see cref="BasicAuthType.Unknown"/> is rejected.</param>
    /// <param name="hostmask">The host mask the entry applies to.</param>
    /// <param name="username">The user name.</param>
    /// <param name="password">The password; sent unchanged and never inspected (may be empty).</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The identifier of the new entry.</returns>
    /// <remarks>
    /// This method transmits credentials to the device. A timeout or a network error does not mean the action failed: the account may have been changed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="hostmask"/>, <paramref name="username"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> is not a known value, or <paramref name="hostmask"/> or <paramref name="username"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<long> AddBasicAuthAsync(BasicAuthType type, string hostmask, string username, string password, CancellationToken cancellationToken = default)
    {
        ValidateType(type, nameof(type));
        FacadeGuard.ValidateText(hostmask, nameof(hostmask));
        FacadeGuard.ValidateText(username, nameof(username));
        ArgumentNullException.ThrowIfNull(password);
        var args = new[] { JsonSerializer.SerializeToElement(type, AccountsJsonContext.Default.BasicAuthType), Str(hostmask), Str(username), Str(password) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "addBasicAuth", FacadeJsonContext.Default.NullableInt64, args, cancellationToken));
    }

    /// <summary>Lists the basic authentication entries (<c>/accountsV2/listBasicAuth</c>).</summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The entries; empty when the device returns no data.</returns>
    /// <remarks>
    /// The device returns the passwords in clear, without opt-out: never log or serialize the returned entries.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<BasicAuthentication>> ListBasicAuthAsync(CancellationToken cancellationToken = default)
        => FacadeResults.ListAsync(this._channel.CallAsync(Base + "listBasicAuth", AccountsJsonContext.Default.ListBasicAuthentication, [], cancellationToken));

    /// <summary>Updates a basic authentication entry (<c>/accountsV2/updateBasicAuth</c>).</summary>
    /// <param name="updatedEntry">The entry to store, identified by <see cref="BasicAuthentication.Id"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device updated the entry.</returns>
    /// <remarks>
    /// This method transmits credentials to the device. A timeout or a network error does not mean the action failed: the account may have been changed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="updatedEntry"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><see cref="BasicAuthentication.Type"/> is not a known value, or <see cref="BasicAuthentication.Hostmask"/> or <see cref="BasicAuthentication.Username"/> is blank or contains control characters or lone surrogates.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> UpdateBasicAuthAsync(BasicAuthentication updatedEntry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updatedEntry);
        ValidateType(updatedEntry.Type, nameof(updatedEntry));
        FacadeGuard.ValidateOptionalText(updatedEntry.Hostmask, nameof(updatedEntry));
        FacadeGuard.ValidateOptionalText(updatedEntry.Username, nameof(updatedEntry));
        var args = new[] { JsonSerializer.SerializeToElement(updatedEntry, AccountsJsonContext.Default.BasicAuthentication) };
        return FacadeResults.RequireAsync(this._channel.CallAsync(Base + "updateBasicAuth", FacadeJsonContext.Default.NullableBoolean, args, cancellationToken));
    }

    /// <summary>Removes basic authentication entries (<c>/accountsV2/removeBasicAuths</c>).</summary>
    /// <param name="ids">The identifiers of the entries (<see cref="BasicAuthentication.Id"/>); an empty list sends an empty array.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns><see langword="true"/> when the device removed the entries.</returns>
    /// <remarks>
    /// The change is applied immediately, without confirmation.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="ids"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid or empty.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<bool> RemoveBasicAuthsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
        => FacadeResults.RequireAsync(this._channel.CallAsync(Base + "removeBasicAuths", FacadeJsonContext.Default.NullableBoolean, [FacadeGuard.Ids(ids, nameof(ids))], cancellationToken));

    private static async Task<IReadOnlyDictionary<string, string>> ToUrlsAsync(Task<Dictionary<string, string>?> call)
        => await call.ConfigureAwait(false) ?? [];

    private static JsonElement Str(string value)
        => JsonSerializer.SerializeToElement(value, FacadeJsonContext.Default.String);

    private static void ValidateType(BasicAuthType type, string paramName)
    {
        if (!FacadeGuard.IsKnown(type))
        {
            throw new ArgumentException("The authentication type must be a known value.", paramName);
        }
    }
}
