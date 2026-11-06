namespace MyJdownloader.Extensions.DependencyInjection.Tests;

internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
