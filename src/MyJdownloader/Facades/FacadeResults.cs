namespace MyJdownloader.Facades;

using MyJdownloader.Errors;
using MyJdownloader.Transport;

/// <summary>Result adapters shared by the device namespace facades.</summary>
internal static class FacadeResults
{
    internal static Task<IReadOnlyList<T>> ListAsync<T>(Task<List<T>?> call)
        => ListAsync(call, static _ => false);

    internal static async Task<IReadOnlyList<T>> ListAsync<T>(Task<List<T>?> call, Func<T, bool> hasNullItems)
    {
        var list = await call.ConfigureAwait(false);
        if (list is null)
        {
            return [];
        }

        foreach (var item in list)
        {
            if (item is null || hasNullItems(item))
            {
                throw Unreadable();
            }
        }

        return list;
    }

    internal static async Task<T> RequireAsync<T>(Task<T?> call)
        where T : class
        => await call.ConfigureAwait(false) ?? throw Unreadable();

    internal static async Task<T> RequireAsync<T>(Task<T?> call)
        where T : struct
        => await call.ConfigureAwait(false) ?? throw Unreadable();

    internal static async Task<bool> RequireAsync(Task<bool?> call)
        => await call.ConfigureAwait(false) ?? throw Unreadable();

    internal static async Task<int> RequireAsync(Task<int?> call)
        => await call.ConfigureAwait(false) ?? throw Unreadable();

    internal static async Task<IReadOnlyDictionary<string, IReadOnlyList<long>>> UrlMapAsync(Task<Dictionary<string, List<long>>?> call)
    {
        var map = await call.ConfigureAwait(false);
        var result = new Dictionary<string, IReadOnlyList<long>>(map?.Count ?? 0, StringComparer.Ordinal);
        if (map is not null)
        {
            foreach (var (url, list) in map)
            {
                result.Add(url, list ?? throw Unreadable());
            }
        }

        return result;
    }

    internal static MyJdProtocolException Unreadable()
        => new(RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device), MyJdErrorSource.Device);
}
