# ADR-006: Relay-only transport and protocol cryptographic constraints

- **Status**: Accepted
- **Date**: 2026-11-02
- **Reference**: `docs/superpowers/specs/2026-11-02-myjdownloader-client-design.md` (§1, §4, §11)

## Context

A JDownloader device can be reached in two ways: through the relay
`https://api.jdownloader.org` (`/t_{sessionToken}_{deviceId}{path}`) or by a
direct connection on the local network (`getDirectConnectionInfos`). The direct
connection adds address discovery, failover, and certificate management.

The MyJD protocol is imposed by a third party. It uses:

- derivation `SHA256(lower(email) + password + "server"|"device")`, with no slow
  KDF (neither PBKDF2 nor Argon2);
- AES-128-CBC, PKCS7 padding, no MAC, with `IV = k[0..16]` and
  `Key = k[16..32]` derived from the same 32-byte key;
- HMAC-SHA256 signature of `/my/*` calls.

## Decision

- **Transport v1**: relay `api.jdownloader.org` only
  (`MyJDownloaderOptions.BaseAddress` configurable, default
  `https://api.jdownloader.org`). Direct LAN connection is deferred to the
  backlog.
- **Cryptography**: faithful implementation of the protocol in `MyJdCrypto`
  (internal, pure functions, BCL only). The weaknesses are **documented as an
  accepted risk**, not modifiable on the client side:
  - AES-CBC without MAC: the ciphertext is not authenticated; mitigated by TLS
    to the relay and by checking the `rid` in the decrypted response
    (inconsistency ⇒ `MyJdProtocolException`);
  - SHA-256 without a slow KDF: the exposed derived keys (exported session) are
    vulnerable to brute force; mitigated by the recommendation to use protected
    storage (ADR-004) and by the absence of secret logging.
- **In-memory key material**: temporary byte copies of secrets are cleared
  after use. Declared members holding byte-typed key material (or a plaintext
  request) are `DebuggerBrowsable(Never)`, enforced by a reflection test;
  string secrets (tokens, passwords) are annotated manually. This is a
  convenience against casual exposure in debugger variable
  views, not a protection: raw views, compiler-generated members (backing
  fields, async state machines), locals, immutable strings and memory dumps
  still reveal the secrets.
- Frozen cryptographic test vectors, produced by the reference client (official
  Python implementation).

## Considered alternatives

| Alternative | Reason for rejection |
|---|---|
| Direct LAN connection from v1 | Complexity (discovery, failover, certificates) outside the v1 success criterion; deferred to the backlog. |
| Strengthen the crypto (AES-GCM, PBKDF2/Argon2, MAC) | Impossible: the server and devices impose the protocol; any deviation would make the client incompatible. |
| Use a third-party crypto library | Unnecessary: the BCL covers SHA-256, HMAC-SHA256 and AES-CBC (ADR-002). |

## Consequences

Positive:

- Reduced v1 scope, a single network path to test.
- Interoperability guaranteed with the service and the reference clients.
- Explicit cryptographic risks for consumers.

Negative:

- Latency and dependence on the relay, even when the device is on the LAN.
- End-to-end security limited by the third-party protocol; confidentiality also
  relies on TLS.
- Adding direct connection later must remain non-breaking for the public API.
