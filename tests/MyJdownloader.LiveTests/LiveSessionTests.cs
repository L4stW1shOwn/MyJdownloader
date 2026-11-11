namespace MyJdownloader.LiveTests;

using MyJdownloader.LiveTests.Live;

[TestFixture]
public sealed class LiveSessionTests
{
    private static DeviceInfo Device(string id, string name) =>
        new() { Id = id, Name = name, Type = "t" };

    [Test]
    public void SelectDevice_NameMatches_ReturnsThatDevice()
    {
        var a = Device("1", "alpha");
        var b = Device("2", "beta");

        var result = LiveSession.SelectDevice([a, b], "beta");

        Assert.That(result, Is.SameAs(b));
    }

    [Test]
    public void SelectDevice_SingleDeviceNoName_ReturnsIt()
    {
        var a = Device("1", "alpha");

        var result = LiveSession.SelectDevice([a], null);

        Assert.That(result, Is.SameAs(a));
    }

    [Test]
    public void SelectDevice_SeveralDevicesNoName_ThrowsWithCountOnly()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LiveSession.SelectDevice([Device("1", "alpha"), Device("2", "beta")], null));

        Assert.That(ex!.Message, Is.EqualTo("Set MYJD_DEVICE: the account has 2 devices."));
        Assert.That(ex.Message, Does.Not.Contain("alpha"));
        Assert.That(ex.Message, Does.Not.Contain("beta"));
    }

    [Test]
    public void SelectDevice_UnknownName_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            LiveSession.SelectDevice([Device("1", "alpha")], "secretname"));

        Assert.That(ex!.Message, Is.EqualTo("No device matches MYJD_DEVICE."));
        Assert.That(ex.Message, Does.Not.Contain("secretname"));
    }

    [Test]
    public void SelectDevice_NoDevice_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => LiveSession.SelectDevice([], null));

        Assert.That(ex!.Message, Is.EqualTo("The account has no device."));
    }
}
