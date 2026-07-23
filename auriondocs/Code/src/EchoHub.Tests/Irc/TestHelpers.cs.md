# TestHelpers.cs

> **Source:** `src/EchoHub.Tests/Irc/TestHelpers.cs`

## Contents

- [FakeChannelService](#fakechannelservice)
- [FakeChatService](#fakechatservice)
- [FakeEncryptionService](#fakeencryptionservice)
- [FakeUserService](#fakeuserservice)
- [TestDuplexStream](#testduplexstream)
- [TestIrcConnectionFactory](#testircconnectionfactory)

---

## FakeChannelService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeChannelService : IChannelService
```


A test double that implements [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md) and lets tests control the results of channel-related operations by setting public properties. Use `FakeChannelService` in unit tests when you need a simple, configurable implementation of [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md) without using a mocking framework.

## Remarks
`FakeChannelService` is a lightweight, stateful fake intended for unit tests. Each operation returns the value of a corresponding public property (for example, `CreateResult`, `UpdateTopicResult`, `DeleteResult`), or a sensible default when a property is not set. This makes it easy to simulate success, failure, and edge cases for callers of [`IChannelService`](../../EchoHub.Core/Contracts/IChannelService.cs.md) without wiring up a full service or external dependencies. [`EnsureSystemChannelAsync`](../../EchoHub.Server/Services/ChannelService.cs.md) will return `SystemChannelToReturn` if set; otherwise it constructs a fallback [`ChannelDto`](../../EchoHub.Core/DTOs/ChatDtos.cs.md) with deterministic fields (a new `Guid`, the supplied `channelName`/`topic`, `DateTimeOffset.UnixEpoch`, and other boolean flags as shown in the implementation).

## Example
```csharp
// Arrange
var fake = new FakeChannelService();
var createdChannel = new ChannelDto(Guid.NewGuid(), "room", null, false, 0, DateTimeOffset.UtcNow, false, false, false);
fake.CreateResult = ChannelOperationResult.Success(createdChannel);

// Act
var result = await fake.CreateChannelAsync(Guid.NewGuid(), "room", null, true);

// Assert
if (!result.IsSuccess) throw new Exception("expected success");
```

## Notes
- The fake exposes mutable public properties; tests must set the appropriate property (for example `CreateResult` or `UpdateTopicResult`) before invoking the corresponding method. Properties are read/write and not thread-safe.
- Several methods return a failure when their result property is unset: operations like `CreateChannelAsync`, [`UpdateTopicAsync`](../../EchoHub.Server/Services/ChannelService.cs.md), [`SetChannelPasswordAsync`](../../EchoHub.Server/Services/ChannelService.cs.md), `RekeyChannelAsync`, and `DeleteChannelAsync` return `ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not configured")` unless the corresponding property is provided. Tests that expect success must assign a matching [`ChannelOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) first.
- The XML summary suggests the fake "records method calls," but the implementation only exposes configurable return properties and does not record call history. If call-count or argument inspection is required, extend the fake (or use a mocking library) to capture that information.

---

## FakeChatService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeChatService : IChatService
```


Fake test double implementing [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) that records every call into in-memory lists and returns configurable, pre-set responses. Use `FakeChatService` in unit or integration tests when you need to assert which [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) methods were invoked or control what the hub/client sees without running a real chat backend.

## Remarks
`FakeChatService` is a combined stub-and-spy: each [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) method either appends a record to one of the public lists (for later assertions) and/or returns the values exposed on its configurable properties. This lets tests both (a) inject specific return values such as `HistoryToReturn`, `JoinError`, `SendMessageError`, `ChannelsForUserToReturn`, and `OnlineUsersToReturn`, and (b) verify side-effects by inspecting `ConnectedUsers`, `DisconnectedConnections`, `JoinedChannels`, `LeftChannels`, `SentMessages`, `StatusUpdates`, and `JoinKeys`. It implements the full [`IChatService`](../../EchoHub.Core/Contracts/IChatService.cs.md) surface so it can be passed anywhere the production service is expected without additional shimming.

## Example
```csharp
// Arrange
var fake = new FakeChatService();
var sampleMessage = new MessageDto(/* ... */); // construct as needed
fake.HistoryToReturn = new List<MessageDto> { sampleMessage };
fake.JoinPasswordRequired = true;

// Act
var joinResult = await fake.JoinChannelAsync("conn-1", Guid.NewGuid(), "alice", "general", "secret");
await fake.SendMessageAsync(Guid.NewGuid(), "alice", "general", "hello world");

// Assert (inspect recorded calls and configured return)
// joinResult.History contains the configured MessageDto
// fake.JoinedChannels contains ("general", "alice")
// fake.JoinKeys contains the supplied password "secret"
// fake.SentMessages contains ("general", "hello world")
```

## Notes
- `FakeChatService` is not thread-safe: all recorded collections are plain `List<T>` instances and are mutated without synchronization. Reset or recreate the instance between parallel tests.
- Several methods ignore some input parameters: for example, [`GetChannelHistoryAsync`](../../EchoHub.Server/Services/ChatService.cs.md) always returns `HistoryToReturn` and does not use the `count` or `offset` arguments; tests relying on real paging behavior will not be exercised by this fake.
- Default behaviors are simple and explicit: [`UserDisconnectedAsync`](../../EchoHub.Server/Services/ChatService.cs.md) returns `null` by default, [`SendMessageAsync`](../../EchoHub.Server/Services/ChatService.cs.md) returns whatever `SendMessageError` is set to, and the various `Broadcast*` methods are no-ops. Tests that need side-effects from broadcasts must simulate them explicitly.

---

## FakeEncryptionService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeEncryptionService : IMessageEncryptionService
```


FakeEncryptionService is a compact, test-oriented implementation that simulates encryption by prefixing plaintext with a fixed marker. It implements [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) and is intended for test scenarios where deterministic, invertible behavior is enough to exercise encryption flows without pulling in real cryptography. Encrypt("hello") produces `"$ENC$hello"`, and Decrypt("$ENC$hello") returns the original text. If a value to decrypt does not start with the expected prefix, Decrypt simply returns the input unchanged. The nullable helpers `EncryptNullable` and `DecryptNullable` mirror the non-nullable versions, preserving null semantics. The `EncryptDatabaseEnabled` flag is always true in this fake, enabling components that check encryption per database usage to behave consistently in tests.

## Remarks
Designed as a lightweight test double, this class enforces the [`IMessageEncryptionService`](../../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) contract while avoiding real crypto. It makes the encryption step observable through a constant prefix, enabling tests to locate and verify encrypted payloads, and to simulate database encryption paths via `EncryptDatabaseEnabled`. By keeping it internal and sealed, the implementation is deliberately opaque to prevent accidental misuse outside tests and to preserve a predictable test surface.

## Notes
- This is a non-secure stub and should never be used for production encryption or storage.
- Because the type is `internal`, it is intended for test code within the same assembly (or a friend-accessible setup). If you need to reference it from production-like tests, ensure appropriate internals visibility is configured.


---

## FakeUserService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeUserService : IUserService
```


Lightweight test double implementing [`IUserService`](../../EchoHub.Core/Contracts/IUserService.cs.md) that lets tests control return values and observe calls. Set the public properties `AuthResult`, `RegisterResult`, and `ProfileToReturn` to force specific outcomes; inspect `RegisterInviteCodes` to assert which `inviteCode` values were passed to `RegisterUserAsync`.

## Remarks
`FakeUserService` exists as an in-memory test helper to avoid exercising real persistence or external systems. All [`IUserService`](../../EchoHub.Core/Contracts/IUserService.cs.md) methods return completed tasks via `Task.FromResult`, so calls are synchronous from the test's perspective and easy to arrange. Use `SuccessResult` to construct a successful [`UserOperationResult`](../../EchoHub.Core/DTOs/CommonDtos.cs.md) containing a [`UserProfileDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) (it sets `UserStatus.Online`, `ServerRole.Member`, and timestamps using `DateTimeOffset.UtcNow`). The class is intended for unit tests where you need deterministic control over authentication/registration/profile responses and simple verification of arguments.

## Example
```csharp
// Arrange
var svc = new FakeUserService();
var userId = Guid.NewGuid();
svc.AuthResult = FakeUserService.SuccessResult(userId, "alice");
svc.RegisterResult = UserOperationResult.Fail(UserError.AlreadyExists, "already");

// Act
var auth = await svc.AuthenticateUserAsync("alice", "pw");
await svc.RegisterUserAsync("bob", "pw", inviteCode: "INV-123");

// Assert
if (!auth.IsSuccess) throw new Exception("expected success");
// Verify that the invite code passed to RegisterUserAsync was recorded
if (svc.RegisterInviteCodes.Count != 1 || svc.RegisterInviteCodes[0] != "INV-123")
    throw new Exception("invite code not recorded");
```

## Notes
- `RegisterInviteCodes` is a plain `List<string?>` that records the raw `inviteCode` argument (including `null`) in call order and is not synchronized; concurrent test runs must not share a single instance without synchronization.
- `SuccessResult` populates timestamps using `DateTimeOffset.UtcNow`, so created [`UserProfileDto`](../../EchoHub.Core/DTOs/ProfileDtos.cs.md) instances will have varying timestamp values; avoid strict equality checks against fixed timestamps.
- [`UpdateProfileAsync`](../../EchoHub.Client/Services/ApiClient.cs.md) and `SetAvatarAsync` always return a failure (`UserError.NotFound` with message "Not configured") unless the test replaces these behaviors; they are placeholders rather than functioning update/asset implementations.


---

## TestDuplexStream
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class TestDuplexStream : Stream
```


A lightweight in-memory duplex `Stream` intended for tests: it exposes a readable input buffer (populated from the `input` constructor argument) and a separate writable output buffer that callers can inspect via `GetOutput` and `GetOutputLines`. Use `TestDuplexStream` when you need to inject deterministic input into code that reads from a `Stream` and capture what that code writes, without opening real network sockets or files.

## Remarks
`TestDuplexStream` intentionally implements only the minimal `Stream` surface needed for typical read/write/flush scenarios in tests. Reads come from the private `_readBuffer` initialized from the constructor `input`, while writes are appended to the private `_writeBuffer` and later returned by `GetOutput`. The class is not seekable (seeking, `Length`, and `Position` throw `NotSupportedException`) because the read and write sides are logically independent buffers rather than a single random-access backing store. The implementation disposes both internal buffers in `Dispose(bool)` so test code should treat a disposed `TestDuplexStream` as unusable.

## Notes
- `GetOutput` strips a leading UTF-8 BOM (the code calls `TrimStart('\uFEFF')`); that handles writers that emit a BOM but also means a deliberate leading U+FEFF in written data will be removed.
- `GetOutputLines` splits on the literal CR+LF sequence (`"\r\n"`) and uses `StringSplitOptions.RemoveEmptyEntries`, so lone `"\n"` line endings or blank lines may not be handled as callers expect.
- Seeking and length-related members are not supported: `Seek`, `SetLength`, `Position` and `Length` throw `NotSupportedException`.
- Reading consumes the provided input buffer; once `Read`/`ReadAsync` drain the `_readBuffer`, subsequent reads return 0 (end-of-stream) unless a new instance is created.
- The class is a test helper and does not provide synchronization for concurrent callers; concurrent reads/writes from multiple threads are not guaranteed to be safe.

---

## TestIrcConnectionFactory
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal static class TestIrcConnectionFactory
```


TestIrcConnectionFactory is an internal static test helper that creates [`IrcClientConnection`](../../EchoHub.Server.Irc/IrcClientConnection.cs.md) instances backed by a `TestDuplexStream` for unit testing. It provides two entry points: `Create`, which builds a new connection wired to an in-memory duplex stream seeded with the supplied input lines; and `CreateAuthenticated`, which builds a pre-authenticated, registered connection by populating identity fields and authentication flags. The input lines are joined with `
` and a trailing `
` is appended if any lines are provided, simulating lines received from an IRC server. The returned tuple gives tests both the [`IrcClientConnection`](../../EchoHub.Server.Irc/IrcClientConnection.cs.md) and the `TestDuplexStream`, enabling observation of outgoing data and control of inbound data.

---