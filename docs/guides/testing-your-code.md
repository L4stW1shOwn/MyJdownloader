# Testing your code

Each typed facade has a public constructor that takes an `IDeviceChannel`. You can build a facade over a test double and unit-test the code that uses the library, without the relay, a device or network access.

## What you can substitute

- The facade over a substitute `IDeviceChannel`, for example `new LinkCrawlerApi(channel)`. This applies to the 24 facades (`LinkGrabberApi`, `DownloadsApi`, `ConfigApi`, ...). The facade still runs its own argument checks and maps the answer, so you test the real code path up to the channel.
- Not `MyJDownloaderClient` nor `JDownloaderDevice`: both are sealed, and `JDownloaderDevice` has no public constructor. Depend on the facade instead (see below).

## Example

NUnit and NSubstitute, from the library's own tests:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MyJdownloader;
using MyJdownloader.LinkCrawler;
using NSubstitute;
using NUnit.Framework;

[TestFixture]
public sealed class LinkCrawlerFacadeTests
{
    [Test]
    public async Task IsCrawlingAsync_FacadeOverSubstituteChannel_ForwardsCallAndReturnsAnswer()
    {
        var channel = Substitute.For<IDeviceChannel>();
        channel.CallAsync(Arg.Any<string>(), Arg.Any<JsonTypeInfo<bool?>>(), Arg.Any<IReadOnlyList<JsonElement>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));
        var api = new LinkCrawlerApi(channel);

        var result = await api.IsCrawlingAsync();

        Assert.That(result, Is.True);
        await channel.Received(1).CallAsync(
            "/linkcrawler/isCrawling",
            Arg.Any<JsonTypeInfo<bool?>>(),
            Arg.Is<IReadOnlyList<JsonElement>>(args => args.Count == 0),
            Arg.Any<CancellationToken>());
    }
}
```

## Depend on the facade, not on the client

Take the facade in your constructor instead of `MyJDownloaderClient`. Your code can then be tested with a substitute, and the production wiring passes the facade of a real device:

```csharp
public sealed class CrawlWatcher(LinkCrawlerApi linkCrawler)
{
    public Task<bool> IsBusyAsync(CancellationToken cancellationToken) =>
        linkCrawler.IsCrawlingAsync(cancellationToken);
}

// Production: new CrawlWatcher(client.GetDevice(deviceId).LinkCrawler)
```

## Limits

- `ContentV2Api.GetFavIconAsync`, `GetFileIconAsync` and `GetIconAsync` need the binary channel that the library provides. Over any other channel they throw `NotSupportedException`. `GetIconDescriptionAsync` works over any channel.
- Argument validation runs in the facade before the channel is called. An invalid argument throws without any call reaching your substitute, so assert it with `Assert.Throws` and check that `Received()` is not called.
- The substitute returns only what you configure. The library does not check the answer against the device, so a test passes only when your fake matches the real answer shape.
- The facade does not retry. Your test double decides the outcome of each call, including exceptions such as `MyJdApiException`.

See [ADR-007](../adr/ADR-007-namespace-facade-conventions.md) for the decision and [Generic device calls](generic-device-calls.md) for the channel.
