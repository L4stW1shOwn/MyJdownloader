# Device

The device namespace checks that a device answers. Its typed method, `ping`, tells you whether the device replies through the relay.

**Coverage:** 1/3 actions of `/device` — not yet typed: `getDirectConnectionInfos` (direct LAN connection, planned as T-030), `getSessionPublicKey` (protocol internals). See [API coverage](../api-coverage.md).

Reach it as `device.Device`, which returns a `DeviceApi`. The C# namespace is `MyJdownloader.Device`, and the device API path is `/device`.

Every method also throws `InvalidOperationException` when the client is not connected, `MyJdException` for relay or device rejections, `OperationCanceledException`, `TimeoutException` and `HttpRequestException`. The error model is described in [errors.md](errors.md).

## DeviceApi

`public sealed class DeviceApi`

Typed facade over the `/device` namespace of one device (liveness check).

| Member | Signature | Description |
|---|---|---|
| `PingAsync` | `Task<bool> PingAsync(CancellationToken cancellationToken = default)` | Checks that the device answers (`/device/ping`). Returns `true` when the device answered the ping. A missing answer from the device is a `MyJdProtocolException`. |

Remarks:

- The device API does not document a `false` answer. An unreachable device surfaces as an exception, such as `MyJdDeviceOfflineException` or a timeout, so handle the exception types as well as the result.
- `getDirectConnectionInfos` and `getSessionPublicKey` are not part of this facade. They remain reachable through [`IDeviceChannel`](device-channel.md).

### Example

```csharp
bool alive = await device.Device.PingAsync(cancellationToken);
if (!alive)
{
    Console.WriteLine("The device did not answer the ping.");
}
```

See also:

- [client.md](client.md)
- [device-channel.md](device-channel.md)
- [errors.md](errors.md)
- [common-types.md](common-types.md)
- [Device administration](../guides/device-administration.md)
</content>
