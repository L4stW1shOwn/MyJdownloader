namespace MyJdownloader.Facades;

using MyJdownloader.Transport;

/// <summary>Result adapters shared by the device namespace facades.</summary>
internal static class FacadeResults
{
    internal static async Task<IReadOnlyList<T>> ListAsync<T>(Task<List<T>?> call)
        => await call.ConfigureAwait(false) ?? [];

    internal static async Task<T> RequireAsync<T>(Task<T?> call)
        where T : class
        => await call.ConfigureAwait(false) ?? throw Unreadable();

    internal static async Task<bool> RequireAsync(Task<bool?> call)
        => await call.ConfigureAwait(false) ?? throw Unreadable();

    internal static async Task<int> RequireAsync(Task<int?> call)
        => await call.ConfigureAwait(false) ?? throw Unreadable();

    internal static MyJdProtocolException Unreadable()
        => new(RelayTransport.UnreadableMessageFor(MyJdErrorSource.Device), MyJdErrorSource.Device);
}
