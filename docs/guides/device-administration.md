# Device administration

Besides downloads, a device exposes facades for its own administration: version
and plugins, operating-system power actions, advanced configuration, premium
hoster accounts, captchas, dialogs, plugin settings, extensions, updates and archive extraction. Each
facade is a property of `JDownloaderDevice`. This guide shows one use case per
facade.

The same rules apply to all of them:

- Actions are applied immediately on the device, without confirmation. A timeout
  or network error does not mean the action failed. Check the state before you
  replay a non-idempotent action.
- Responses are capped at 1 MiB of encrypted body. Restrict large queries.
- Values returned by the device are untrusted text: encode them before you
  display them, and do not log them when they can hold secrets.

## Jd: version, uptime, plugins

```csharp
TimeSpan uptime = await device.Jd.GetUptimeAsync(cancellationToken);
long version = await device.Jd.GetVersionAsync(cancellationToken);
int revision = await device.Jd.GetCoreRevisionAsync(cancellationToken);

bool refreshed = await device.Jd.RefreshPluginsAsync(cancellationToken);
```

## System: restart, exit, power

```csharp
await device.System.RestartAsync(cancellationToken);   // restarts JDownloader
await device.System.ExitAsync(cancellationToken);      // closes JDownloader

await device.System.ShutdownOsAsync(force: false, cancellationToken);
await device.System.StandbyOsAsync(cancellationToken);
await device.System.HibernateOsAsync(cancellationToken);
```

These actions are destructive, and the device usually goes offline before it
answers. A timeout or network error does not mean the action failed, and a replay
can run it twice.

## Config: advanced settings

Configuration values are untyped on the device and are exposed as `JsonElement`
([ADR-008](../adr/ADR-008-untyped-values-as-jsonelement.md)).

```csharp
using System.Text.Json;
using MyJdownloader.Config;

// Values come back only with Values = true. Pattern is a regular expression evaluated on the device.
IReadOnlyList<ConfigEntry> entries = await device.Config.QueryAsync(
    new ConfigQuery { Pattern = "maxsimultanedownloads", Values = true },
    cancellationToken);

ConfigEntry entry = entries[0];
JsonElement current = await device.Config.GetAsync(
    entry.InterfaceName!, entry.Storage, entry.Key!, cancellationToken);

JsonElement defaultValue = await device.Config.GetDefaultAsync(
    entry.InterfaceName!, entry.Storage, entry.Key!, cancellationToken);

// Applied immediately, without confirmation.
await device.Config.SetAsync(
    entry.InterfaceName!, entry.Storage, entry.Key!,
    JsonSerializer.SerializeToElement(3), cancellationToken);

// Back to the default value.
await device.Config.ResetAsync(entry.InterfaceName!, entry.Storage, entry.Key!, cancellationToken);
```

- Values may hold secrets (passwords, tokens). Never log them.
- `SetAsync` sends the value as its JSON text (strings are quoted). This is to be
  confirmed against a real device.
- Restrict queries with `ConfigQuery.ConfigInterface` or `ConfigQuery.Pattern` to
  stay under the response limit.
- `ListEnumAsync(type)` returns the options of an enumerated setting.

## Accounts: premium hosters and basic authentication

```csharp
using MyJdownloader.Accounts;

// The credentials are sent to the device, which stores them.
await device.Accounts.AddAccountAsync("example-hoster.com", "my-user", passwordFromYourSecretStore, cancellationToken);

IReadOnlyList<Account> accounts = await device.Accounts.ListAccountsAsync(cancellationToken: cancellationToken);
await device.Accounts.RefreshAccountsAsync([accounts[0].Uuid], cancellationToken);

// Basic authentication for a host mask (FTP or HTTP).
long authId = await device.Accounts.AddBasicAuthAsync(
    BasicAuthType.Http, "*.example.com", "my-user", passwordFromYourSecretStore, cancellationToken);
```

- Passwords are never retained or logged by the library. Do not log them either.
- `ListBasicAuthAsync` returns basic authentication passwords in clear: the device
  has no opt-out. Never log or serialize those entries.
- Account operations take the ids of the accounts: `EnableAccountsAsync`,
  `DisableAccountsAsync`, `RemoveAccountsAsync`, `SetUserNameAndPasswordAsync`,
  `UpdateBasicAuthAsync` and `RemoveBasicAuthsAsync`.
- `ListPremiumHostersAsync` lists the supported hosters.

## Captcha: solve or skip pending challenges

```csharp
using MyJdownloader.Captcha;

IReadOnlyList<CaptchaJob> jobs = await device.Captcha.ListAsync(cancellationToken);

// A Base64 data URL, as sent by the device and not decoded.
string image = await device.Captcha.GetImageAsync(jobs[0].Id, cancellationToken: cancellationToken);
bool solved = await device.Captcha.SolveAsync(jobs[0].Id, "answer", cancellationToken: cancellationToken);
bool skipped = await device.Captcha.SkipAsync(jobs[0].Id, CaptchaSkipRequest.Single, cancellationToken);
```

- When a captcha id no longer exists, the device usually answers `404 NOT_AVAILABLE`,
  which surfaces as a `MyJdApiException`.
- `CaptchaSkipRequest` sets the scope of the skip: `Single`, `BlockHoster`,
  `BlockPackage`, `BlockAllCaptchas`, `Refresh`, `StopCurrentAction` or `Timeout`.
  `Unknown` is rejected.
- The image, `Hoster`, `Explain`, `Type` and `CaptchaCategory` come from the device
  and are untrusted. Check that the image starts with `data:image/`, decode the
  Base64 before you render it, and HTML-encode the text before you embed it in a
  page.

## Dialogs: answer what JDownloader is waiting on

```csharp
using System.Text.Json;
using MyJdownloader.Dialogs;

IReadOnlyList<long> ids = await device.Dialogs.ListAsync(cancellationToken);
if (ids.Count > 0)
{
    DialogInfo dialog = await device.Dialogs.GetAsync(ids[0], cancellationToken: cancellationToken);
    if (dialog.Type is not null)
    {
        DialogTypeInfo typeInfo = await device.Dialogs.GetTypeInfoAsync(dialog.Type, cancellationToken);

        // Device text is untrusted: encode it before displaying it. Properties may hold secrets: never log them.
        Console.WriteLine($"Dialog {ids[0]}: {Encode(dialog.Type)}");
        foreach (var (key, value) in dialog.Properties ?? new Dictionary<string, string>())
        {
            Console.WriteLine($"  {Encode(key)} = {Encode(value)}");
        }

        // Answer only after an explicit user decision. The keys come from typeInfo.Out ("ok" is an example).
        if (typeInfo.Out is { } outKeys && outKeys.ContainsKey("ok") && AskUser("Answer yes to this dialog?"))
        {
            var answer = new Dictionary<string, JsonElement>
            {
                ["ok"] = JsonSerializer.SerializeToElement(true),
            };
            await device.Dialogs.AnswerAsync(ids[0], answer, cancellationToken);
        }
    }
}
```

`Encode` and `AskUser` stand for your own output encoding and user prompt.
Never answer dialogs automatically or in bulk without user review: some dialogs
confirm overwrites, updates or trust decisions. Properties and answer values may
hold secrets (login or password dialogs): never log them. With `includeIcon: true`
the icon is a Base64 data URL inside `Properties`: check the `data:image/` prefix
before you render it.

- `AnswerAsync` closes the dialog and JDownloader continues immediately, without
  confirmation. Ask the user first. A timeout means the answer may have been applied.
- An unknown id answers `400 "Invalid ID"`, an unknown type `400 "Requested Type not found!"`,
  and a dialog that needs another answer first `400 "Answer Dialog N first"`. All
  surface as a `MyJdApiException`.
- `Type`, the `Properties` and `DialogTypeInfo` text come from the device and are
  untrusted: encode them before you display them.

## Plugins: settings and URL patterns

```csharp
using System.Text.Json;
using MyJdownloader.Plugins;

// List the plugins (the list can be large, and entries may hold secrets: never log them).
IReadOnlyList<PluginInfo> plugins = await device.Plugins.ListAsync(new PluginsQuery { Pattern = false }, cancellationToken);
if (plugins.Count > 0)
{
    PluginInfo plugin = plugins[0];
    if (plugin.InterfaceName is not null && plugin.DisplayName is not null && plugin.Key is not null)
    {
        // Read the setting. The value may hold a secret: do not log it.
        JsonElement current = await device.Plugins.GetAsync(plugin.InterfaceName, plugin.DisplayName, plugin.Key, cancellationToken);

        // Set and reset apply immediately, without confirmation: ask the user first.
        if (current.ValueKind == JsonValueKind.True && AskUser("Disable this plugin setting?"))
        {
            await device.Plugins.SetAsync(
                plugin.InterfaceName,
                plugin.DisplayName,
                plugin.Key,
                JsonSerializer.SerializeToElement(false),
                cancellationToken);

            // Or go back to the default value.
            await device.Plugins.ResetAsync(plugin.InterfaceName, plugin.DisplayName, plugin.Key, cancellationToken);
        }
    }
}

// Which plugin handles a URL?
IReadOnlyList<string> patterns = await device.Plugins.GetPluginRegexAsync("https://example.com/file/abc", cancellationToken);
Console.WriteLine($"{patterns.Count} pattern(s) match.");
```

`AskUser` stands for your own user prompt.

- `SetAsync` and `ResetAsync` change the device configuration without confirmation.
  A timeout means the action may have been executed: read the value back before
  you replay it.
- Plugin settings often hold secrets (hoster credentials, tokens). `ToString()` of
  `PluginInfo` and `PluginConfigEntry` hides the values, but never log `Value`,
  `DefaultValue` or the result of `GetAsync`.
- Patterns are regular expressions defined on the device. Do not compile them
  without a match timeout. The names, documentation and patterns are untrusted
  text: encode them before you display them.
- An unknown plugin, key or an invalid value answers `400 BAD_PARAMETERS` or
  `400 INVALID_VALUE`, which surface as a `MyJdApiException`.
- `QueryAsync` takes the `ConfigQuery` of the config facade. Restrict it with
  `ConfigInterface` or `Pattern` to stay under the 1 MiB response cap.

## Extensions: list, install, enable

```csharp
using MyJdownloader.DeviceExtensions;

IReadOnlyList<Extension> extensions = await device.Extensions.ListAsync(
    new ExtensionQuery { Pattern = ".*Extraction.*" },
    cancellationToken);

foreach (Extension extension in extensions)
{
    Console.WriteLine($"{extension.Id}: enabled={extension.Enabled}");
}

if (extensions.Count > 0 && extensions[0].Id is { } id)
{
    // Applied immediately, without confirmation; a timeout does not mean the action failed.
    await device.Extensions.SetEnabledAsync(id, true, cancellationToken);
}
```

- `Extension.Id` is the class name that the other calls take.
- `InstallAsync` makes the device download and install the extension from its own
  JDownloader update source, not from the caller. Take the id from
  `Extension.Id` or an allow-list, never from end-user input.
- The `Pattern` is a regular expression evaluated on the device.
- Text returned by the device is untrusted: encode it before you display it.

## Update: check and restart

```csharp
await device.Update.RunUpdateCheckAsync(cancellationToken);   // may return before the check finishes

if (await device.Update.IsUpdateAvailableAsync(cancellationToken))
{
    // Restarts JDownloader immediately, without confirmation. The device usually goes
    // offline before it answers, so a timeout does not mean the action failed.
    await device.Update.RestartAndUpdateAsync(cancellationToken);
}
```

## Extraction: archives

```csharp
using MyJdownloader.Extraction;

foreach (ArchiveStatus archive in await device.Extraction.GetQueueAsync(cancellationToken))
{
    Console.WriteLine($"{archive.ArchiveName}: {archive.ControllerStatus}");
}

// Adds the password to the persisted password list of the device.
await device.Extraction.AddArchivePasswordAsync(password, cancellationToken);

// Applied immediately, without confirmation; a timeout does not mean the action failed.
await device.Extraction.SetArchiveSettingsAsync(
    archiveId,
    new ArchiveSettings { AutoExtract = true },
    cancellationToken);

// Start the extraction of some links now, or cancel a running controller.
await device.Extraction.StartExtractionNowAsync([linkId], [], cancellationToken);
await device.Extraction.CancelExtractionAsync(controllerId, cancellationToken);
```

- `ArchiveSettings` holds passwords. They are masked in `ToString()`, but the device
  returns them in clear. Do not log them.
- `GetArchiveInfoAsync` and `GetArchiveSettingsAsync` look archives up by link or
  package id, or by archive id.
