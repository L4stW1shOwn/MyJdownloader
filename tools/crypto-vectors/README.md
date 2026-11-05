# Crypto reference vectors

Generates the reference vectors for the internal MyJD crypto (secrets, tokens,
HMAC signature, AES-128-CBC ciphertexts) using only the Python standard library
and the OpenSSL CLI, independently of the C# implementation.

## Prerequisites

- Python 3
- OpenSSL (`openssl` on the PATH)

## Usage

```
python3 -I tools/crypto-vectors/generate.py
```

Output is deterministic: one `NAME = value` line per vector.

## Rule

Paste the output into `MyJdCryptoTests.cs`; tests never run this script.
