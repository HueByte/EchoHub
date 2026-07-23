# Encryption and room keys

> Client-side encryption and per-room key protection for secure messaging.

Client-side message confidentiality is implemented in two cooperating pieces: a runtime encryptor that performs AES-256-GCM on outgoing and incoming message payloads, and a storage protector that keeps per-room content keys encrypted on disk. The runtime service expects a 32-byte key (provided as base64) and emits self-contained ciphertext that carries nonce and tag; the protector hides those room keys at rest behind platform-specific protections so the config file never stores raw base64 keys.

## ClientEncryptionService.cs
Implements client-side encryption for messages and room keys.

The [ClientEncryptionService](../Code/src/EchoHub.Client/Services/ClientEncryptionService.cs.md) is a sealed implementation of the message-encryption contract that performs AES-256-GCM on plaintext before it leaves the client. Its public surface includes SetKey (accepts a base64-encoded key and enforces exactly 32 bytes), Encrypt (generates a fresh 12-byte nonce, produces a 16-byte authentication tag, and returns a string that begins with an EncryptionPrefix and contains base64-encoded nonce and payload), and Decrypt (which returns plaintext unchanged if no key is set or if the input lacks the expected prefix). The class also provides nullable-friendly helpers EncryptNullable and DecryptNullable; decryption failures are handled gracefully by returning a sentinel message rather than throwing. Because it expects a server-provisioned key via SetKey, it does not manage persistent key storage itself and therefore can be paired with a separate on-disk protector to obtain that key material at runtime.

## RoomKeyProtector.cs
Provides protection for per-room encryption keys used in chats.

The [RoomKeyProtector](../Code/src/EchoHub.Client/Services/RoomKeyProtector.cs.md) encrypts the cached per-room content keys so the client configuration does not hold plain base64 keys. It exposes Protect(byte[] roomKey) to produce a storable string and TryUnprotect(string stored, out byte[] roomKey, out bool wasLegacy) to recover raw key bytes. On Windows it prefers DPAPI in the current-user scope and marks values with the DpapiPrefix (dp1:); on other platforms it encrypts keys with AES-GCM using a per-user master key file stored next to the config (KeyFilePrefix, k1:) with 0600 permissions. Entries with no known prefix are treated as legacy plain-base64 keys: they are loaded once and re-encrypted under the active scheme on save. The class caches the per-user master key, chooses the protection mechanism by platform, and explicitly never stores the room passphrase itself.

How the pieces fit

At runtime the pattern is: RoomKeyProtector is responsible for safe at-rest storage of raw room key bytes; callers call TryUnprotect to obtain the byte[] for a room, base64-encode that raw key and pass it to ClientEncryptionService.SetKey, and then call Encrypt/Decrypt to protect message payloads. Conversely, when a new room key is generated or received from the server, callers call Protect to produce the on-disk representation (with the dp1: or k1: prefix) so future runs can recover the same raw bytes. The direction of dependency is clear: the protector controls persistent formats and prefixes and hands raw key bytes to higher-level encryption (which enforces the 32-byte requirement and performs AES-GCM message operations).

---
*Covers 2 of 2 source files identified for this topic.*

*Synthesised by AurionDocs on 2026-07-23 09:34:11 UTC*
