# RoomCrypto

> **File:** `src/EchoHub.Core/Security/RoomCrypto.cs`  
> **Kind:** class

```csharp
public static class RoomCrypto
```


Client-side utilities for envelope encryption used by private (end-to-end encrypted) channels. Use `RoomCrypto` when you need a simple, opinionated way to derive keys from a passphrase, generate a random room content key (RCK), and encrypt/decrypt room content in the wire format this project uses (a `$RC1$`-prefixed base64 blob for text and a nonce||tag||ciphertext blob for raw bytes).

## Remarks
`RoomCrypto` implements the client-side half of an envelope-encryption scheme: the client generates a random 256-bit room content key (RCK) that actually encrypts all channel content, and a key-encryption key (KEK) derived from the user's passphrase is used to wrap the RCK before the wrapped RCK is stored on the server. The derivation uses PBKDF2-SHA256 with `Pbkdf2Iterations` (210000) and a `SaltSizeBytes` (16) salt; the resulting 64 bytes are split so the first `KeySizeBytes` (32) bytes are exported as a lowercase hex `AuthKeyHex` (the join credential) and the last `KeySizeBytes` bytes are kept as the `KeyEncryptionKey`. `RoomCrypto` keeps a small, explicit surface: `GenerateSalt`, `GenerateRoomKey`, `DeriveKeys`, `EncryptText`, `TryDecryptText`, `IsRoomCiphertext`, and byte-level `EncryptBytes`/`DecryptBytes` (used internally). The text wire format is the literal `CiphertextPrefix` (`"$RC1$"`) followed by `Convert.ToBase64String(nonce||tag||ciphertext)`; binary APIs return/expect the raw `nonce||tag||ciphertext` blob. The implementation zeroes the slice of derived bytes used for the auth key after converting to hex to reduce exposure of sensitive material.

## Example
```csharp
// Typical client flow: derive keys from a passphrase, create a room key, encrypt and decrypt text.
var salt = RoomCrypto.GenerateSalt();
var derived = RoomCrypto.DeriveKeys("correct horse battery staple", salt);
// `derived.AuthKeyHex` is sent to the server as the join credential; `derived.KeyEncryptionKey` stays local.
var roomKey = RoomCrypto.GenerateRoomKey();

var ciphertext = RoomCrypto.EncryptText("hello room", roomKey);
if (RoomCrypto.TryDecryptText(ciphertext, roomKey, out var plaintext))
{
    // plaintext == "hello room"
}
```

## Notes
- `RoomCrypto` expects a `KeySizeBytes`-length key (32 bytes) for its AES-GCM operations; supplying a key of the wrong length will fail when constructing the cipher.
- Nonces are randomly generated per-encryption (`NonceSizeBytes` = 12). Do not reuse a `roomKey`/nonce pair for different plaintexts; the implementation already generates random nonces, so avoid reusing the same nonce manually.
- `TryDecryptText` returns `false` (and sets `plaintext` to empty) both for non-room content (missing the `CiphertextPrefix`) and for any integrity/format errors (bad base64, authentication failure).