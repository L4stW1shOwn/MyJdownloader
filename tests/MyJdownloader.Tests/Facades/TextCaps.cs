namespace MyJdownloader.Tests.Facades;

using MyJdownloader.Facades;
using NSubstitute;

/// <summary>Texts sized on the exact text length caps, and assertions shared by the per-facade boundary tests.</summary>
internal static class TextCaps
{
    internal static readonly string Identifier = new('a', FacadeGuard.MaxIdentifierLength);
    internal static readonly string IdentifierOver = new('a', FacadeGuard.MaxIdentifierLength + 1);
    internal static readonly string Path = new('a', FacadeGuard.MaxPathLength);
    internal static readonly string PathOver = new('a', FacadeGuard.MaxPathLength + 1);
    internal static readonly string LongText = new('a', FacadeGuard.MaxLongTextLength);
    internal static readonly string LongTextOver = new('a', FacadeGuard.MaxLongTextLength + 1);

    internal static void AssertSent(IDeviceChannel channel, Func<Task> call)
        => AssertSent(() => channel.ReceivedCalls().Count(), call);

    internal static void AssertSent(Func<int> callCount, Func<Task> call)
    {
        _ = call();

        Assert.That(callCount(), Is.EqualTo(1));
    }

    internal static void AssertRejected(IDeviceChannel channel, Func<Task> call, string paramName)
        => AssertRejected(() => channel.ReceivedCalls().Count(), call, paramName);

    internal static void AssertRejected(Func<int> callCount, Func<Task> call, string paramName)
    {
        var thrown = Assert.Throws<ArgumentException>(() => _ = call());

        Assert.Multiple(() =>
        {
            Assert.That(thrown!.ParamName, Is.EqualTo(paramName));
            Assert.That(callCount(), Is.Zero);
        });
    }
}
