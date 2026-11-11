namespace MyJdownloader.LiveTests.Live;

using MyJdownloader.Config;

[TestFixture]
[Category("Live")]
public sealed class ConfigLiveTests
{
    private static readonly ConfigQuery _noValues = new()
    {
        Values = false,
        DefaultValues = false,
    };

    [Test]
    [CancelAfter(60_000)]
    public async Task Query_ReturnsEntries(CancellationToken cancellationToken)
    {
        var entries = await LiveGuard.RunAsync(() => LiveSession.Device.Config.QueryAsync(_noValues, cancellationToken));

        Assert.That(entries, Is.Not.Empty);
    }

    [Test]
    [CancelAfter(60_000)]
    public async Task Get_KnownKey_Answers(CancellationToken cancellationToken)
    {
        var entries = await LiveGuard.RunAsync(() => LiveSession.Device.Config.QueryAsync(_noValues, cancellationToken));
        var entry = entries.FirstOrDefault(e => e.InterfaceName is not null && e.Key is not null);
        if (entry is null)
        {
            Assert.Ignore("No config entry available");
        }

        await LiveGuard.RunAsync(() => LiveSession.Device.Config.GetAsync(entry.InterfaceName!, entry.Storage, entry.Key!, cancellationToken));
    }
}
