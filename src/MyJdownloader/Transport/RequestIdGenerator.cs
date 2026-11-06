namespace MyJdownloader.Transport;

/// <summary>
/// Generates strictly increasing request identifiers seeded from the current Unix time in milliseconds.
/// </summary>
internal sealed class RequestIdGenerator
{
    private readonly TimeProvider _timeProvider;
    private long _last;

    public RequestIdGenerator(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        this._timeProvider = timeProvider;
    }

    /// <summary>Returns the next identifier; it is always greater than any previously returned value.</summary>
    public long Next()
    {
        while (true)
        {
            var previous = Interlocked.Read(ref this._last);
            var next = Math.Max(previous + 1, this._timeProvider.GetUtcNow().ToUnixTimeMilliseconds());
            if (Interlocked.CompareExchange(ref this._last, next, previous) == previous)
            {
                return next;
            }
        }
    }
}
