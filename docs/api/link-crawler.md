# Link crawler

The link crawler namespace tells whether the link crawler of the device is running.

**Coverage:** 1/1 actions of `/linkcrawler` (complete).

Reach it as `device.LinkCrawler`, which returns a `LinkCrawlerApi`. The C# namespace is `MyJdownloader.LinkCrawler`, and the device API path is `/linkcrawler`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

## LinkCrawlerApi

`public sealed class LinkCrawlerApi`

Typed facade over the `/linkcrawler` namespace of one device (link crawler activity).

| Member | Signature | Description |
|---|---|---|
| `IsCrawlingAsync` | `Task<bool> IsCrawlingAsync(CancellationToken cancellationToken = default)` | Tells whether the link crawler of the device is running (`/linkcrawler/isCrawling`). Returns `true` while it runs. A missing answer from the device is a `MyJdProtocolException`. |

Remarks:

- The answer is a snapshot taken when the call reaches the device. The crawler may start or stop right after the call returns.
- Each call is one request. To watch the crawler over time, repeat the call with a delay of your own choosing.

### Example

```csharp
bool crawling = await device.LinkCrawler.IsCrawlingAsync(cancellationToken);
Console.WriteLine(crawling ? "The link crawler is running." : "The link crawler is idle.");
```

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Managing downloads](../guides/managing-downloads.md)
</content>
