# Sessions and authentication

The client authenticates with the account email and password, then keeps an
immutable `MyJdSession` that holds the tokens. This guide covers connecting,
exporting and restoring the session, automatic renewal, and how to store the
session safely.

## Connect with email and password

```csharp
using MyJdownloader;

MyJdSession session = await client.ConnectAsync(email, password);

Console.WriteLine(session);        // MyJdSession { Email = e***@example.com }
Console.WriteLine(client.IsConnected); // True
```

- `ConnectAsync` replaces any session the client already holds.
- The password is never stored, never exposed by the library and never logged.
  The client derives the keys it needs from it during the call. Keep the
  password in your own secret store, and drop your references to it as soon as
  the call returns.
- `ArgumentException` and `ArgumentNullException` for a blank or null email or
  password are thrown synchronously, before the returned task exists.
- Rejected credentials surface as `MyJdAuthenticationException`. See
  [Error handling](error-handling.md).
- `MyJdSession.ToString()` shows only a masked email. It never shows a token or a
  key.

## Export the session

`ExportSession()` returns the current session, or `null` when the client is not
connected. `ToJson()` serializes it as versioned JSON.

```csharp
string? json = client.ExportSession()?.ToJson();
```

The JSON contains keys derived from the password and the session tokens. Treat
it as a secret. See [Storing the session](#storing-the-session).

## Restore the session

`RestoreSession` installs a session without sending any request and without
raising `SessionChanged`. Use it instead of `ConnectAsync` when you already hold
a session from an earlier run.

```csharp
MyJdSession restored = MyJdSession.FromJson(json);
client.RestoreSession(restored);

// Device facades obtained before the restore use the restored session.
JDownloaderDevice device = client.GetDevice(deviceId);
long version = await device.Jd.GetVersionAsync();
```

- `FromJson` throws `FormatException` for invalid or unsupported data. The
  message never contains a value from the input.
- `FromJson` throws `ArgumentNullException` when `json` is `null`.
- A restored session whose regain token has expired fails on its first call with
  `MyJdAuthenticationException`. Connect again with the password in that case.

## Automatic reconnection

The client renews the session on its own when the relay answers
`TOKEN_INVALID`:

1. The first caller to hit `TOKEN_INVALID` renews the session with the regain
   token (`ReconnectAsync`). Concurrent callers wait for the same renewal
   (single-flight), so only one renewal request is sent.
2. The failed call is replayed once with the new session.
3. If the renewal is rejected, the client disconnects, raises `SessionChanged`
   with a `null` session, and the call throws `MyJdAuthenticationException`.
   Only a fresh `ConnectAsync` with the password can recover from this state.

You can also renew explicitly:

```csharp
MyJdSession renewed = await client.ReconnectAsync(cancellationToken);
```

`ReconnectAsync` throws `InvalidOperationException` when the client is not
connected. The renewal that runs automatically inside another call is not
cancellable by the caller: it is bounded by `HttpClient.Timeout`.

If you restore a session while a renewal is in flight, the restored session
wins and the renewed tokens are discarded (they stay valid on the server until
they expire).

Renewal does not retry network errors. See [Error handling](error-handling.md).

## Session-changed event

`SessionChanged` is raised when the client establishes, renews or clears its
session. It is not raised by `RestoreSession`.

```csharp
client.SessionChanged += (_, e) =>
{
    // Persist the latest state, not e.Session: it can be outdated when the handler runs.
    var json = client.ExportSession()?.ToJson();
    if (json is null)
    {
        secretStore.Delete("myjd-session");   // disconnected
    }
    else
    {
        secretStore.Save("myjd-session", json); // secretStore is your own abstraction
    }
};
```

- Handlers run on an arbitrary thread. Marshal to a UI thread yourself.
- All handlers run even if one throws. Their exceptions reach the caller of the
  operation after the session was updated, except when the operation was a
  rejected renewal: then the authentication exception wins.
- `DisconnectAsync` raises the event with a `null` session, even if the server
  call fails.

## Storing the session

The exported JSON is a bearer credential for your account, and it contains keys
derived from the password. Store it protected:

- Use a platform protection API (for example DPAPI on Windows) or an encrypted
  store, or a secret vault such as Azure Key Vault or HashiCorp Vault.
- Never write it in plain text, in source control, in configuration files that
  are committed, or in logs and telemetry.
- Save it again after every `SessionChanged` event, since renewal rotates the
  tokens.
- Delete it on disconnect.

The library cannot clear the string from memory (managed strings are not
clearable), so keep its lifetime short. See [Logging and security](logging-and-security.md).

## Disconnect

```csharp
await client.DisconnectAsync();
```

`DisconnectAsync` clears the local session before calling the server. If the
server call fails, the tokens may remain valid on the server until they expire.
