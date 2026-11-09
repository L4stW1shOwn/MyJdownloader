# ADR-011: Unauthenticated binary device responses

- **Status**: Accepted
- **Date**: 2026-11-09

## Context

T-050 delivers the `/contentV2` device namespace. Three of its actions,
`getFavIcon`, `getFileIcon` and `getIcon`, return an image instead of JSON.

Verified in the JDownloader source
(`org.jdownloader.api.myjdownloader.MyJDownloaderHttpConnection.getOutputStream`
and `org.jdownloader.api.content.v2.ContentAPIImplV2`):

- A device response whose `Content-Type` starts with `application/json` is
  **AES-128-CBC encrypted** with the device encryption token (base64, chunked)
  and carries the request `rid`; the library checks it (ADR-006).
- **Any other response is written raw**: not encrypted, no envelope, no `rid`.
  The icon actions write `image/png` with `Cache-Control: public,max-age=60`,
  and the `api.jdownloader.org` relay forwards the bytes as is.
- The errors of these actions (`FILE_NOT_FOUND` 404, `BAD_PARAMETERS` 400 when
  the size exceeds 128, `INTERNAL_SERVER_ERROR` 500) go through the normal,
  encrypted JSON error path.

Consequently, the icon bytes are **neither authenticated nor bound to the
request**: only TLS to the relay protects them, and the relay (or anything able
to alter relay traffic) could substitute them.

Today `RelayTransport.ExchangeAsync` always decrypts and UTF-8-decodes the
body, and the public `IDeviceChannel` (ADR-001) only exposes JSON calls, so
these actions are unreachable.

## Decision

Internal binary channel, used only by the `ContentV2` facade:

- New **internal** interface **`IBinaryDeviceChannel`**:
  `Task<byte[]> CallBinaryAsync(string path, IReadOnlyList<JsonElement> args, CancellationToken cancellationToken)`,
  implemented by `DeviceChannel`. **No public binary API** on `IDeviceChannel`
  for now. The decision is reversible: the method can be made public later as
  a backward-compatible minor addition.
- **Request**: encrypted exactly like a JSON call (same body, `rid`, session
  handling through the client's `ExecuteAsync`). Automatic reconnection via
  regaintoken and the single replay (ADR-004) are unchanged.
- **Transport**: new **`RelayTransport.ExchangeBinaryAsync`**, sharing the
  non-200 error handling and the timeout logic with `ExchangeAsync`. On a 200
  response:
  - the `Content-Type` must start with `image/png` (case-insensitive);
  - the body must not exceed the existing 1 MiB cap;
  - the body must start with the 8-byte PNG signature;
  - otherwise a **`MyJdProtocolException`** is thrown (unreadable device
    response).
  - No decryption and no `rid` check: the device sends neither.
- **Facade**: the icon methods return the bytes as **`byte[]`**. The XML doc
  states that they are **untrusted and unauthenticated**: decode them with a
  robust image library, never serve them under another content type, and never
  use them for security decisions.

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Public `CallBinaryAsync` on `IDeviceChannel` | Freezes public surface (PublicApiAnalyzers, package validation) without a current need; only `ContentV2` returns binary data. Can still be added later as a minor change. |
| Not typing the icon actions | The `ContentV2` namespace would be incomplete and the icon actions would stay unreachable, since `IDeviceChannel` cannot return non-JSON bodies. |
| Returning the bytes without validation | The `Content-Type` and PNG signature checks are cheap; they reject non-image payloads early and limit content-sniffing misuse by callers that forward the bytes. |
| Returning a `Stream` | Icons are small and the body is already capped at 1 MiB; a `byte[]` is simpler to consume and test, and has no disposal contract. |
| Decrypting or verifying the response | Impossible: the device sends no encryption, signature, MAC or `rid` for non-JSON responses; there is nothing to verify. |

## Consequences

Positive:

- The three icon actions become reachable, completing the `ContentV2`
  namespace.
- No public surface change beyond the facade methods; the binary channel stays
  internal and can be published later as a minor addition.
- Request encryption, session handling, reconnection, error mapping, timeouts
  and the 1 MiB cap are shared with the JSON path (ADR-004, ADR-005, ADR-006).
- The trust level of the bytes is explicit in the XML doc, and the format
  checks reject obviously wrong payloads.

Negative:

- The integrity of the icon bytes relies on the relay and TLS only; the library
  cannot detect a substitution by the relay or by anything able to alter relay
  traffic.
- The PNG check (content type and signature) is not a full image validation: a
  malformed or hostile PNG still reaches the caller.
- The binary path is not reachable for custom or generic calls through
  `IDeviceChannel`; a caller needing another binary action must wait for a
  public API.
- A second exchange method in `RelayTransport` adds a code path to maintain and
  test, even though error handling and timeouts are shared.
