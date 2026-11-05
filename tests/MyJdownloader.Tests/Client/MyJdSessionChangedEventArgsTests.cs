namespace MyJdownloader.Tests.Client;

[TestFixture]
public sealed class MyJdSessionChangedEventArgsTests
{
    private static MyJdSession CreateSession() =>
        new("user@example.com", Key(1), Key(2), "0011aabb", "ccdd2233", Key(3), Key(4));

    private static byte[] Key(byte value) => [.. Enumerable.Repeat(value, 32)];

    [Test]
    public void Session_Session_IsExposed()
    {
        var session = CreateSession();

        var args = new MyJdSessionChangedEventArgs(session);

        Assert.That(args.Session, Is.SameAs(session));
    }

    [Test]
    public void Session_Null_IsExposedAsNull()
    {
        var args = new MyJdSessionChangedEventArgs(null);

        Assert.That(args.Session, Is.Null);
    }
}
