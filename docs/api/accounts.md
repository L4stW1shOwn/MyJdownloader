# Accounts

The accounts namespace manages two kinds of third-party credentials stored on the device: premium hoster accounts (for example a paid download service) and basic authentication entries (FTP or HTTP credentials for a host mask). Use it to add, list, enable, refresh and remove them.

**Coverage:** 14/14 actions of `/accountsV2` (complete).

Reach it as `device.Accounts`, which returns an `AccountsApi`. The C# namespace is `MyJdownloader.Accounts`, and the device API path is `/accountsV2`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

This namespace handles third-party credentials. Passwords are sent as call arguments and are never retained, logged or printed by the facade; do not log them either. Basic authentication passwords come back from the device in clear text, so never log or serialize the entries returned by `ListBasicAuthAsync`.

## AccountsApi

`public sealed class AccountsApi`

Typed facade over the `/accountsV2` namespace of one device (premium hoster accounts and basic authentication entries).

| Member | Signature | Description |
|---|---|---|
| `AddAccountAsync` | `Task AddAccountAsync(string premiumHoster, string username, string password, CancellationToken cancellationToken = default)` | Adds a premium hoster account (`/accountsV2/addAccount`). The password is sent unchanged and never inspected. A timeout means the account may have been changed. |
| `ListAccountsAsync` | `Task<IReadOnlyList<Account>> ListAccountsAsync(AccountQuery? query = null, CancellationToken cancellationToken = default)` | Lists premium hoster accounts (`/accountsV2/listAccounts`). A `null` query returns all accounts with all fields. `MaxResults` below `-1` or a negative `StartAt` throws `ArgumentException`. |
| `EnableAccountsAsync` | `Task EnableAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)` | Enables accounts (`/accountsV2/enableAccounts`) by `Account.Uuid`. |
| `DisableAccountsAsync` | `Task DisableAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)` | Disables accounts (`/accountsV2/disableAccounts`) by `Account.Uuid`. |
| `RefreshAccountsAsync` | `Task RefreshAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)` | Refreshes accounts (`/accountsV2/refreshAccounts`) by `Account.Uuid`. |
| `RemoveAccountsAsync` | `Task RemoveAccountsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)` | Removes accounts (`/accountsV2/removeAccounts`) by `Account.Uuid`. Applied immediately, without confirmation. |
| `SetUserNameAndPasswordAsync` | `Task<bool> SetUserNameAndPasswordAsync(long accountId, string username, string password, CancellationToken cancellationToken = default)` | Changes the user name and password of an account (`/accountsV2/setUserNameAndPassword`) and returns `true` when the device applied the change. The password is sent unchanged. |
| `ListPremiumHostersAsync` | `Task<IReadOnlyList<string>> ListPremiumHostersAsync(CancellationToken cancellationToken = default)` | Lists the premium hosters supported by the device (`/accountsV2/listPremiumHoster`). |
| `ListPremiumHosterUrlsAsync` | `Task<IReadOnlyDictionary<string, string>> ListPremiumHosterUrlsAsync(CancellationToken cancellationToken = default)` | Lists the account page URL of each premium hoster (`/accountsV2/listPremiumHosterUrls`), keyed by hoster name. |
| `GetPremiumHosterUrlAsync` | `Task<string?> GetPremiumHosterUrlAsync(string hoster, CancellationToken cancellationToken = default)` | Returns the account page URL of one premium hoster (`/accountsV2/getPremiumHosterUrl`), or `null` when the device knows none for that hoster. |
| `AddBasicAuthAsync` | `Task<long> AddBasicAuthAsync(BasicAuthType type, string hostmask, string username, string password, CancellationToken cancellationToken = default)` | Adds a basic authentication entry (`/accountsV2/addBasicAuth`) and returns its identifier. `BasicAuthType.Unknown` is rejected. The password may be empty. |
| `ListBasicAuthAsync` | `Task<IReadOnlyList<BasicAuthentication>> ListBasicAuthAsync(CancellationToken cancellationToken = default)` | Lists the basic authentication entries (`/accountsV2/listBasicAuth`). The device returns the passwords in clear, without opt-out. |
| `UpdateBasicAuthAsync` | `Task<bool> UpdateBasicAuthAsync(BasicAuthentication updatedEntry, CancellationToken cancellationToken = default)` | Stores an updated basic authentication entry (`/accountsV2/updateBasicAuth`), identified by `BasicAuthentication.Id`. A `null` `Password` is not sent; see the remarks. |
| `RemoveBasicAuthsAsync` | `Task<bool> RemoveBasicAuthsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)` | Removes basic authentication entries (`/accountsV2/removeBasicAuths`) by `BasicAuthentication.Id` and returns `true` when the device removed them. |

Remarks:

- Methods that transmit credentials (`AddAccountAsync`, `SetUserNameAndPasswordAsync`, `AddBasicAuthAsync`, `UpdateBasicAuthAsync`) do not treat a timeout or network error as a failure: the account may have been changed.
- `UpdateBasicAuthAsync`: a `null` `BasicAuthentication.Password` is not sent. The device behaviour in that case (keep or clear the stored password) is unconfirmed until live tests. To keep the password, send the entry as read from `ListBasicAuthAsync`, password included.
- For `EnableAccountsAsync`, `DisableAccountsAsync`, `RefreshAccountsAsync`, `RemoveAccountsAsync` and `RemoveBasicAuthsAsync`, an empty identifier list sends an empty array; `null` throws `ArgumentNullException`.

### Example

```csharp
using MyJdownloader.Accounts;

// Read the password from your secret store (environment variable, vault, ...); never hard-code it.
string password = Environment.GetEnvironmentVariable("HOSTER_PASSWORD")
    ?? throw new InvalidOperationException("HOSTER_PASSWORD is not set.");

await device.Accounts.AddAccountAsync("example-hoster.com", "my-user", password);

var accounts = await device.Accounts.ListAccountsAsync();
await device.Accounts.RefreshAccountsAsync([accounts[0].Uuid]);

long authId = await device.Accounts.AddBasicAuthAsync(BasicAuthType.Http, "*.example.com", "my-user", password);
```

## Account

`public sealed record Account`

A premium hoster account stored on the device. Fields that the query did not request are `null`.

The device never returns the account password, so this record holds no secret. `Hostname`, `Username`, `ErrorType` and `ErrorString` are raw device values: they are untrusted and unbounded, so do not log or display them unfiltered.

| Property | Type | Description |
|---|---|---|
| `Uuid` | `long` | Account identifier, used by the other `AccountsApi` methods. |
| `Hostname` | `string?` | Hoster the account belongs to. |
| `Username` | `string?` | User name. |
| `Enabled` | `bool?` | Whether the account is enabled. |
| `Valid` | `bool?` | Whether the account is valid. |
| `ErrorType` | `string?` | Error type. Raw device value; its set of values is not documented. |
| `ErrorString` | `string?` | Error message. |
| `TrafficLeft` | `long?` | Remaining traffic in bytes, as reported by the device (raw value). |
| `TrafficMax` | `long?` | Maximum traffic in bytes, as reported by the device (raw value). |
| `ValidUntil` | `DateTimeOffset?` | Expiry date, or `null` when not set. |

## AccountQuery

`public sealed record AccountQuery`

Selects the accounts and fields returned by `ListAccountsAsync`. Every field flag is `true` by default.

| Property | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Returns the enabled flag. |
| `Error` | `bool` | `true` | Returns the error type and message. |
| `TrafficLeft` | `bool` | `true` | Returns the remaining traffic. |
| `TrafficMax` | `bool` | `true` | Returns the maximum traffic. |
| `UserName` | `bool` | `true` | Returns the user name. |
| `Valid` | `bool` | `true` | Returns the validity flag. |
| `ValidUntil` | `bool` | `true` | Returns the expiry date. |
| `MaxResults` | `int` | `-1` | Maximum number of accounts to return. `-1` means no limit. Values below `-1` throw `ArgumentException`. |
| `StartAt` | `int` | `0` | Zero-based index of the first account. Negative values throw `ArgumentException`. |
| `Uuids` | `IReadOnlyList<long>?` | `null` | Identifiers of the accounts to return. `null` means all accounts. |

### Example

```csharp
var accounts = await device.Accounts.ListAccountsAsync(new AccountQuery { MaxResults = 20, TrafficMax = false });
```

## BasicAuthentication

`public sealed record BasicAuthentication`

A basic authentication entry (FTP or HTTP) stored on the device, used for reading and updating.

The device returns `Password` in clear, without any opt-out. Do not log or serialize this record: `ToString()` masks the password as `***`, but value equality includes it.

| Property | Type | Description |
|---|---|---|
| `Id` | `long` | Entry identifier. |
| `Type` | `BasicAuthType` | Protocol the entry applies to. |
| `Hostmask` | `string?` | Host mask the entry applies to. |
| `Username` | `string?` | User name. |
| `Password` | `string?` | Password, returned in clear by the device. A `null` value is not sent on update. |
| `Enabled` | `bool` | Whether the entry is enabled. |
| `Created` | `DateTimeOffset?` | Creation date, or `null` when not set. |
| `LastValidated` | `DateTimeOffset?` | Date of the last validation, or `null` when not set. |

## BasicAuthType

`public enum BasicAuthType`

Protocol a basic authentication entry applies to, as used by the JDownloader API. Unrecognized values are read as `Unknown`, and `Unknown` cannot be sent.

| Member | Value | Description |
|---|---|---|
| `Unknown` | `0` | The value is missing or not recognized by this library. |
| `Ftp` | `1` | FTP (`FTP`). |
| `Http` | `2` | HTTP (`HTTP`). |

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
