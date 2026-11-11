namespace MyJdownloader.Ui;

using System.Text.Json;
using MyJdownloader.Facades;

/// <summary>Typed facade over the <c>/ui</c> namespace of one device (context menus and their actions).</summary>
/// <remarks>
/// <para>All content returned by the device is untrusted. Only the <see cref="MenuContext.Dlc"/> context is implemented by the device.</para>
/// <para>Results of actions are untyped on the device and are exposed as <see cref="JsonElement"/> (ADR-008).</para>
/// </remarks>
public sealed class UiApi
{
    private const string Base = "/ui/";

    private static readonly JsonElement _nullElement = JsonDocument.Parse("null").RootElement.Clone();

    private readonly IDeviceChannel _channel;

    internal UiApi(IDeviceChannel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);
        this._channel = channel;
    }

    /// <summary>Gets the menu of a user interface context (<c>/ui/getMenu</c>).</summary>
    /// <param name="context">The context of the menu.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The root item of the menu; it has no identifier, name or type, and holds the menu entries as children.</returns>
    /// <remarks>
    /// Only <see cref="MenuContext.Dlc"/> is implemented by the device; any other context is answered with 404 (<see cref="MyJdApiException"/>). The menu comes from the device and is untrusted. Items of type <see cref="MenuItemType.Link"/> (for example <c>enable</c> and <c>rename</c>) are hints for the client user interface and do nothing on the device.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="context"/> is <see cref="MenuContext.Unknown"/> or not defined.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid (including a menu without data or with a <see langword="null"/> child).</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<MenuItem> GetMenuAsync(MenuContext context, CancellationToken cancellationToken = default)
    {
        var args = new[] { ContextArg(context) };
        return ToMenuAsync(this._channel.CallAsync(Base + "getMenu", UiJsonContext.Default.MenuItem, args, cancellationToken));
    }

    /// <summary>Runs a menu action on the selected links and packages (<c>/ui/invokeAction</c>).</summary>
    /// <param name="context">The context of the menu.</param>
    /// <param name="id">The identifier of the action, taken from <see cref="MenuItem.Id"/>.</param>
    /// <param name="linkIds">The identifiers of the selected links.</param>
    /// <param name="packageIds">The identifiers of the selected packages.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The result of the action; an element of kind <see cref="JsonValueKind.Null"/> when the device returns none, which is the case for every current JDownloader action.</returns>
    /// <remarks>
    /// <para>Actions have side effects on the device: for example <c>checkonline</c> queues an online check of the selection and <c>forcedls</c> forces its downloads. Only <see cref="MenuContext.Dlc"/> acts; other contexts do nothing. An unknown action id is answered with 404 (<see cref="MyJdApiException"/>). Items of type <see cref="MenuItemType.Link"/> do nothing on the device. Obtain the explicit consent of the end user before calling it and never call it unattended.</para>
    /// <para>The result comes from the device and is untrusted.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="id"/>, <paramref name="linkIds"/> or <paramref name="packageIds"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="context"/> is <see cref="MenuContext.Unknown"/> or not defined, or <paramref name="id"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An identifier in <paramref name="linkIds"/> or <paramref name="packageIds"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled; the action may have been executed.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task<JsonElement> InvokeActionAsync(MenuContext context, string id, IReadOnlyList<long> linkIds, IReadOnlyList<long> packageIds, CancellationToken cancellationToken = default)
    {
        var contextArg = ContextArg(context);
        FacadeGuard.ValidateText(id, nameof(id), FacadeGuard.MaxIdentifierLength);
        var ids = FacadeGuard.IdArgs(linkIds, packageIds);
        var args = new[] { contextArg, JsonSerializer.SerializeToElement(id, FacadeJsonContext.Default.String), ids[0], ids[1] };
        return ToValueAsync(this._channel.CallAsync(Base + "invokeAction", UiJsonContext.Default.NullableJsonElement, args, cancellationToken));
    }

    private static JsonElement ContextArg(MenuContext context)
    {
        if (!FacadeGuard.IsKnown(context))
        {
            throw new ArgumentException("The menu context must be a known value.", nameof(context));
        }

        return JsonSerializer.SerializeToElement(context, UiJsonContext.Default.MenuContext);
    }

    private static async Task<MenuItem> ToMenuAsync(Task<MenuItem?> call)
    {
        var menu = await FacadeResults.RequireAsync(call).ConfigureAwait(false);
        return HasNullChild(menu) ? throw FacadeResults.Unreadable() : menu;
    }

    private static bool HasNullChild(MenuItem item)
    {
        foreach (var child in item.Children)
        {
            if (child is null || HasNullChild(child))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<JsonElement> ToValueAsync(Task<JsonElement?> call)
        => await call.ConfigureAwait(false) ?? _nullElement;
}
