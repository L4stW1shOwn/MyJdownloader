namespace MyJdownloader.Tests.Transport;

internal sealed class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UnixEpoch;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
