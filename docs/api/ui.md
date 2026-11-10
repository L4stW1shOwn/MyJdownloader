# Ui

The ui namespace reads the context menu of the download list and runs its actions on the selected links and packages.

**Coverage:** 2/2 actions of `/ui` (complete).

Reach it as `device.Ui`, which returns a `UiApi`. The C# namespace is `MyJdownloader.Ui`, and the device API path is `/ui`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

> **Warning: side effects.** `InvokeActionAsync` runs the action at once, without confirmation. The actions act on the device: `checkonline` queues an online check of the selection and `forcedls` forces the downloads of the selection. Obtain the explicit consent of the end user for the action before you call it, and never call it unattended (background jobs, batch tools, automated tests against a real device).
>
> A `TimeoutException`, `OperationCanceledException` or `HttpRequestException` means the action may have been executed. Do not retry blindly: check the state of the download list before you replay the call.

## UiApi

`public sealed class UiApi`

Typed facade over the `/ui` namespace of one device (context menus and their actions). All content returned by the device is untrusted.

| Member | Signature | Description |
|---|---|---|
| `GetMenuAsync` | `Task<MenuItem> GetMenuAsync(MenuContext context, CancellationToken cancellationToken = default)` | Gets the menu of a context (`/ui/getMenu`). The root item has no `Id`, `Name` or `Type`; its `Children` are the menu entries. Throws `ArgumentException` for `MenuContext.Unknown`, and `MyJdProtocolException` when the response holds no menu or a `null` child. |
| `InvokeActionAsync` | `Task<JsonElement> InvokeActionAsync(MenuContext context, string id, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)` | Runs the action `id` on the selected links and packages (`/ui/invokeAction`) and returns the result. Returns an element of kind `JsonValueKind.Null` when the device returns none. Throws `ArgumentNullException` for a `null` id or list, and `ArgumentException` for an unknown context, a blank id or an id with control characters or lone surrogates. These checks run before any request. |

### GetMenuAsync

Remarks:

- Only `MenuContext.Dlc` is implemented by the device. Any other context is answered with `404`, which surfaces as a `MyJdApiException`.
- The menu is user-customizable on the device. Its entries can change between two calls: read it again rather than keep the identifiers of an earlier menu.
- Items of type `MenuItemType.Link` (for example `enable` and `rename`) are hints for the client user interface. They do nothing on the device; the caller must handle them itself.
- The content comes from the device and is untrusted. Show the names as plain text.

### InvokeActionAsync

Remarks:

- Pass the `Id` of an item of type `MenuItemType.Action`, taken from the menu returned by `GetMenuAsync`. An unknown identifier is answered with `404`, which surfaces as a `MyJdApiException`.
- Only `MenuContext.Dlc` acts. Other contexts do nothing on the device.
- The action runs on the selection: `linkIds` for links and `packageIds` for packages. Use the identifiers from the download list (see [Downloads](downloads.md)).
- Every current JDownloader action returns no value, so the result is a `JsonElement` of kind `Null`. The result is untrusted anyway; read it only if a future device version returns one.
- Passing `Link` items to this method has no effect on the device.

## MenuItem

`public sealed record MenuItem`

One entry of a device menu, as returned by `GetMenuAsync`.

| Property | Type | Description |
|---|---|---|
| `Id` | `string?` | Identifier of the item, to pass to `InvokeActionAsync` for an action. `null` when the device sends none. |
| `Name` | `string?` | Display name of the item. `null` when the device sends none. |
| `Icon` | `string?` | Icon key of the item, usable with the [ContentV2](content-v2.md) icon methods. `null` when the device sends none. |
| `Type` | `MenuItemType` | Kind of the item. `Unknown` when the device sends none or a value this library does not know. |
| `Children` | `IReadOnlyList<MenuItem>` | Child items. Empty when the item has none; never `null`. |

**Remarks**

- The values come from the device and are untrusted. Do not log them unfiltered.
- `ToString()` includes the untrusted device text (`Id`, `Name`, `Icon`) unfiltered. Do not log it as is; filter control characters first.
- `Children` compares by reference, so two items with equal content and equal but distinct non-empty child lists are not equal (`==` is `false`). Items without children compare by content.
- The identifier of a `Container` or `Link` item is not meant for `InvokeActionAsync`.

## MenuContext

`public enum MenuContext`

Selects the user interface context of a menu.

| Value | Wire name | Description |
|---|---|---|
| `Unknown` | — | Missing or not recognized. Cannot be sent: the library rejects it with `ArgumentException`. |
| `Dlc` | `DLC` | The right-click menu of the download list. The only context the device implements. |
| `Lgc` | `LGC` | The right-click menu of the link grabber. Not implemented by the device: `GetMenuAsync` answers `404`, and `InvokeActionAsync` does nothing. |

## MenuItemType

`public enum MenuItemType`

Kind of a `MenuItem`.

| Value | Wire name | Description |
|---|---|---|
| `Unknown` | — | Missing or not recognized (the root item has no type). |
| `Container` | `CONTAINER` | A container of other items. |
| `Action` | `ACTION` | An action the device runs with `InvokeActionAsync`. |
| `Link` | `LINK` | A hint for the client user interface that does nothing on the device. |

### Example

```csharp
using System.Linq;
using System.Text.Json;
using MyJdownloader.Ui;

// 1. Read the context menu of the download list. The menu is untrusted device content.
MenuItem menu = await device.Ui.GetMenuAsync(MenuContext.Dlc, cancellationToken);

// 2. Flatten the tree and keep the actions the device runs. Link items are client hints: skip them.
List<MenuItem> actions = Flatten(menu)
    .Where(item => item.Type == MenuItemType.Action && item.Id is not null)
    .ToList();

// 3. Let the user choose one action. The selection comes from your own user interface.
MenuItem chosen = PickAction(actions);

// 4. Run it on the links and packages the user selected, once the user has confirmed.
//    The action acts at once; after a timeout, check the download list before you retry.
JsonElement result = await device.Ui.InvokeActionAsync(
    MenuContext.Dlc,
    chosen.Id!,
    selectedLinkIds,
    selectedPackageIds,
    cancellationToken);

static IEnumerable<MenuItem> Flatten(MenuItem item)
{
    foreach (MenuItem child in item.Children)
    {
        yield return child;

        foreach (MenuItem descendant in Flatten(child))
        {
            yield return descendant;
        }
    }
}
```

`PickAction`, `selectedLinkIds` and `selectedPackageIds` stand for your own selection screen and the identifiers of the selection, taken from the download list. `chosen.Id!` is safe because the filter above keeps only items with an identifier.

## Remarks on consent

- The consent is the end user's, not the application's: ask for it in an interactive session, for each action that changes the download list.
- Reading the menu does not change the device. Only `InvokeActionAsync` acts.

See also:

- [client.md](client.md)
- [downloads.md](downloads.md)
- [content-v2.md](content-v2.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [Managing downloads](../guides/managing-downloads.md)
