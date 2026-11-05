"""Generate reference vectors for the internal MyJD crypto (stdlib + openssl CLI)."""
import base64
import hashlib
import hmac
import subprocess


def sha256(data: bytes) -> bytes:
    return hashlib.sha256(data).digest()


def openssl_aes(data: bytes, key: bytes, iv: bytes, decrypt: bool = False, nopad: bool = False) -> bytes:
    args = ["openssl", "enc", "-aes-128-cbc", "-K", key.hex(), "-iv", iv.hex()]
    if decrypt:
        args.append("-d")
    if nopad:
        args.append("-nopad")
    return subprocess.run(args, input=data, capture_output=True, check=True).stdout


def encrypt(data: bytes, key: bytes, iv: bytes, nopad: bool = False) -> str:
    cipher = openssl_aes(data, key, iv, nopad=nopad)
    # Round trip check through openssl itself.
    assert openssl_aes(cipher, key, iv, decrypt=True, nopad=nopad) == data
    return base64.b64encode(cipher).decode("ascii")


def main() -> None:
    email = "User.Name@Example.COM"
    password = "pässwörd€"
    stc = "00112233445566778899aabbccddeeff00112233445566778899aabbccddeeff"
    str_ = "ffeeddccbbaa99887766554433221100ffeeddccbbaa99887766554433221100"

    login_secret = sha256((email.lower() + password + "server").encode("utf-8"))
    device_secret = sha256((email.lower() + password + "device").encode("utf-8"))
    server_token_connect = sha256(login_secret + bytes.fromhex(stc))
    server_token_reconnect = sha256(server_token_connect + bytes.fromhex(str_))
    device_token = sha256(device_secret + bytes.fromhex(str_))
    sign_data = "/my/connect?email=user.name%40example.com&appkey=MyJdownloader.NET&rid=1730800000000"
    signature = hmac.new(login_secret, sign_data.encode(), hashlib.sha256).hexdigest()

    key, iv = device_token[16:], device_token[:16]
    json_plain = '{"data":"élève ✓","rid":1730800000000}'

    vectors = [
        ("EMAIL", email),
        ("PASSWORD", password),
        ("SESSION_TOKEN_CONNECT", stc),
        ("SESSION_TOKEN_RECONNECT", str_),
        ("LOGIN_SECRET", login_secret.hex()),
        ("DEVICE_SECRET", device_secret.hex()),
        ("SERVER_TOKEN_CONNECT", server_token_connect.hex()),
        ("SERVER_TOKEN_RECONNECT", server_token_reconnect.hex()),
        ("DEVICE_TOKEN", device_token.hex()),
        ("SIGN_DATA", sign_data),
        ("SIGNATURE", signature),
        ("CIPHER_EMPTY", encrypt(b"", key, iv)),
        ("CIPHER_BLOCK", encrypt(b"0123456789abcdef", key, iv)),
        ("CIPHER_JSON", encrypt(json_plain.encode("utf-8"), key, iv)),
        ("CIPHER_BAD_PADDING", encrypt(bytes(16), key, iv, nopad=True)),
    ]
    for name, value in vectors:
        print(f"{name} = {value}")


if __name__ == "__main__":
    main()
