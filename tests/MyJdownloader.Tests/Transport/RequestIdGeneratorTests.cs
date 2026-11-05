namespace MyJdownloader.Tests.Transport;

using System.Collections.Concurrent;
using MyJdownloader.Transport;

[TestFixture]
public sealed class RequestIdGeneratorTests
{
    private static readonly DateTimeOffset Reference = new(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Next_FirstCall_ReturnsUnixMillisecondsOfTimeProvider()
    {
        var clock = new FakeTimeProvider { UtcNow = Reference };
        var generator = new RequestIdGenerator(clock);

        var id = generator.Next();

        Assert.That(id, Is.EqualTo(1793836800000L));
    }

    [Test]
    public void Next_ConsecutiveCalls_StrictlyIncrease()
    {
        var clock = new FakeTimeProvider { UtcNow = Reference };
        var generator = new RequestIdGenerator(clock);

        var first = generator.Next();
        var second = generator.Next();

        Assert.That(second, Is.EqualTo(first + 1));
    }

    [Test]
    public void Next_ClockGoesBackwards_StillIncreases()
    {
        var clock = new FakeTimeProvider { UtcNow = Reference };
        var generator = new RequestIdGenerator(clock);
        var first = generator.Next();
        clock.UtcNow = Reference.AddHours(-1);

        var second = generator.Next();

        Assert.That(second, Is.GreaterThan(first));
    }

    [Test]
    public void Next_ConcurrentCalls_ReturnDistinctValues()
    {
        var clock = new FakeTimeProvider { UtcNow = Reference };
        var generator = new RequestIdGenerator(clock);
        var ids = new ConcurrentBag<long>();

        Parallel.For(0, 1000, _ => ids.Add(generator.Next()));

        Assert.That(ids.Distinct().Count(), Is.EqualTo(1000));
    }

    [Test]
    public void Ctor_NullTimeProvider_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new RequestIdGenerator(null!));
}
