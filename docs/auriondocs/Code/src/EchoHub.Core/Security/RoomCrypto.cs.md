# RoomCrypto

> **File:** `src/EchoHub.Core/Security/RoomCrypto.cs`  
> **Kind:** class

```csharp
public static class RoomCrypto
```


Client-side envelope encryption primitives used for end-to-end encrypted channels: derive per-room keys from a passphrase, generate random room content keys (RCKs), and encrypt/decrypt room content using AES-GCM. Use this class when you need a canonical, interoperable way to create room key material, wrap/unlock a room key with a passphrase-derived key, and produce/recognize the wire format used on the server ($RC1$base64(nonce||tag||ciphertext)).

## Remarks
This class encapsulates the protocol choices and low-level crypto work so callers don't compose PBKDF2, hex encoding, and AES-GCM themselves. It implements an envelope pattern: the client generates a random 256-bit room content key (RCK) to encrypt room data; the RCK is stored server-side wrapped (AES-GCM) with a key-encryption key (KEK) derived from the user's passphrase. PBKDF2-SHA256 with 210000 iterations produces 64 bytes: the first 32 bytes (returned as lowercase hex) are the auth key used as the join gate, and the final 32 bytes are the KEK (never sent). Re-wrapping the RCK on passphrase change avoids re-encrypting history.

## Example
```csharp
// Typical client flow:
// 1) Create room: generate salt and room key, derive keys from passphrase, wrap RCK and send auth key + wrapped blob to server.
var salt = RoomCrypto.GenerateSalt();
var roomKey = RoomCrypto.GenerateRoomKey();
var derived = RoomCrypto.DeriveKeys("correct horse battery staple", salt);
// derived.AuthKeyHex is sent to server as the join credential
// derived.KeyEncryptionKey (KEK) is used locally to wrap roomKey with AES-GCM (use EncryptBytes/EncryptText as appropriate)

// 2) Encrypt/decrypt room content with the room key
var plaintext = "hello room";
var ct = RoomCrypto.EncryptText(plaintext, roomKey);
if (RoomCrypto.IsRoomCiphertext(ct) && RoomCrypto.TryDecryptText(ct, roomKey, out var recovered))
{
    // recovered == "hello room"
}
```

## Notes
- PBKDF2 parameters are fixed: 16-byte salt, 210000 iterations, 64-byte output; the auth key is returned as lowercase hex and the KEK as raw bytes.
- AES-GCM parameters are fixed: 12-byte nonce, 16-byte tag, 32-byte key (AES-256). Text wire format is the literal prefix "$RC1$" then base64(nonce||tag||ciphertext).
- TryDecryptText returns false for non-room ciphertext or when decryption/authentication fails (malformed base64, wrong key, or tampering). Protect KEK and RCK in memory and avoid persisting raw keys.
