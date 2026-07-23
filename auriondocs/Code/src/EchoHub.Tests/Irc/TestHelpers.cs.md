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


A lightweight test double that implements IChannelService by returning pre-configured results for each operation. Use this in unit tests to simulate success, failure, or specific payloads from channel-related operations without wiring up real storage or network dependencies.

## Remarks
The fake exposes properties you set from tests (for example CreateResult, TopicResult, ChannelListToReturn, MembershipResult, CryptoToReturn, KeyEnvelopeToReturn, ChannelMetaToReturn, SystemChannelToReturn and others). Most methods return Task.FromResult(...) of those properties or a default failure (ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not configured")) when a result property has not been provided. EnsureSystemChannelAsync returns the configured SystemChannelToReturn or a minimal default ChannelDto (with a new Guid and IsSystem = true) when not configured.

## Example
```csharp
// Arrange: create the fake and configure the CreateChannelAsync result
var fake = new FakeChannelService();
var created = new ChannelDto(Guid.NewGuid(), "my-channel", "topic", false, 0, DateTimeOffset.UnixEpoch, false, false, false);
fake.CreateResult = ChannelOperationResult.Success(created);

// Act: call the service (synchronously here via Task.Result for brevity in tests)
var result = fake.CreateChannelAsync(Guid.NewGuid(), "my-channel", "topic", isPublic: true).Result;

// Assert: the configured success is returned
if (!result.IsSuccess) throw new Exception("expected success");
```

## Notes
- The fake uses Task.FromResult and no asynchronous I/O; it's intended only for synchronous-style unit tests and will not reproduce concurrency or latency characteristics of a real implementation.
- If you forget to set a specific "Result" property (e.g. CreateResult, UpdateTopicResult, RekeyResult), the fake returns ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not configured").
- EnsureSystemChannelAsync will fabricate a new ChannelDto with a fresh Guid when SystemChannelToReturn is not set; tests that rely on a stable id should explicitly set SystemChannelToReturn.

---

## FakeChatService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeChatService : IChatService
```


A test double that implements IChatService for use in unit tests. It records calls (connected users, disconnections, joins/leaves, sent messages, status updates) into public lists and returns configurable, pre-seeded results (history, errors, online users, channels). Reach for this when you need a deterministic, inspectable chat service in tests rather than the real implementation.

## Remarks
FakeChatService exists solely to make tests observable and controllable: callers can assert that particular chat operations were invoked by inspecting the public lists, and tests can control what operations return by setting the configurable properties (HistoryToReturn, JoinError, SendMessageError, etc.). It does not perform any real validation or I/O and intentionally records inputs (including join passwords) so tests can verify them.

## Example
```csharp
// Typical usage in a unit test
var svc = new FakeChatService();

// Simulate a user connecting
await svc.UserConnectedAsync("conn-1", Guid.NewGuid(), "alice");
// svc.ConnectedUsers now contains "alice"

// Simulate joining a channel (password may be null)
var (history, error, passwordRequired) = await svc.JoinChannelAsync("conn-1", Guid.NewGuid(), "alice", "#room", null);
// svc.JoinedChannels contains ("#room", "alice") and svc.JoinKeys contains the password passed

// Simulate sending a message and configure an error result
svc.SendMessageError = "rate-limited";
var sendErr = await svc.SendMessageAsync(Guid.NewGuid(), "alice", "#room", "hello world");
// sendErr == "rate-limited" and svc.SentMessages contains ("#room", "hello world")
```

## Notes
- The public list properties are mutable and intended for test inspection; tests should reset or recreate the FakeChatService between cases to avoid cross-test contamination.
- JoinChannelAsync records the supplied password into JoinKeys — this test double intentionally captures sensitive inputs for verification, so treat recorded passwords carefully in test logs.
- This implementation makes no concurrency guarantees; if tests exercise the fake from multiple threads you may encounter race conditions.

---

## FakeEncryptionService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeEncryptionService : IMessageEncryptionService
```


It is a test double that mimics encrypted content by prefixing plaintext with a fixed marker, allowing tests to verify code paths that handle encrypted data without introducing real cryptography.

## Remarks
This internal, sealed class provides a deterministic, reversible "encryption" scheme for testing scenarios that depend on encrypted strings. By implementing IMessageEncryptionService, it enables tests to validate integration points that consume or produce ciphertext without relying on real cryptographic routines. The EncryptDatabaseEnabled property being true signals that encryption should be considered active in the test database layer. Use this class when you need predictable, fast behavior in unit tests that exercise encryption-related code paths.

## Example
```csharp
var service = new FakeEncryptionService();
string ciphertext = service.Encrypt("hello"); // "$ENC$hello"
string plaintext = service.Decrypt(ciphertext); // "hello"

string? nullCipher = service.EncryptNullable(null); // null
string? nullPlain = service.DecryptNullable(null); // null
string? recovered = service.DecryptNullable(ciphertext); // "hello"
```

## Notes
- This is a fake encryption shim for tests; it is not cryptographically secure.
- Decrypt only removes the prefix if present; non-prefixed content is returned unchanged.
- EncryptNullable/DecryptNullable are null-safe helpers that simplify test code.

---

## FakeUserService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeUserService : IUserService
```


A lightweight test double of IUserService that records calls and returns pre-configured results. Use this in unit or integration tests when you need to simulate authentication, registration, and profile lookups without exercising the real user backend.

## Remarks
This class exposes mutable properties (AuthResult, RegisterResult, ProfileToReturn) that callers set to control the behavior of the corresponding IUserService methods. It also records invite codes passed to RegisterUserAsync in RegisterInviteCodes so tests can assert which invite codes were used. The SuccessResult helper creates a typical successful UserOperationResult with a UserProfileDto for convenience; other operations (UpdateProfileAsync, SetAvatarAsync) are intentionally left to always return a NotFound failure to indicate they are not implemented in this fake.

## Notes
- The fake is stateful: AuthResult, RegisterResult, ProfileToReturn and RegisterInviteCodes are mutable. Reset or recreate the FakeUserService between tests to avoid cross-test contamination.
- If AuthResult or RegisterResult are left null, AuthenticateUserAsync and RegisterUserAsync return default Fail results (InvalidCredentials and AlreadyExists respectively) with explanatory messages.
- UpdateProfileAsync and SetAvatarAsync always return a NotFound failure ("Not configured"). They are placeholders rather than working implementations.

---

## TestDuplexStream
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class TestDuplexStream : Stream
```


A lightweight in-memory duplex Stream intended for tests: it supplies readable bytes from a preloaded input buffer and captures all written bytes to a separate output buffer that can be inspected. Use this when you need to simulate a readable/writable stream (for example, feeding input to code that reads a Stream and asserting what that code wrote) without interacting with files or the console.

## Remarks
This class models a unidirectional read buffer and a separate write buffer so consumers can read a fixed input and concurrently write output without interfering with each other. It intentionally implements only the Stream surface required by simple producers/consumers: reading delegates to an internal MemoryStream created from the provided input string; writing appends to a second MemoryStream. The stream is non-seekable to better emulate pipes or network streams and to discourage tests from relying on seeking behavior.

## Notes
- The stream does not support seeking: Position, Length, Seek and SetLength all throw NotSupportedException.
- GetOutput trims a UTF-8 BOM from the captured bytes because StreamWriter may emit one; tests that rely on raw bytes should use _writeBuffer directly instead of GetOutput (the internal buffer is disposed on Dispose()).
- GetOutputLines splits on the Windows CRLF sequence ("\r\n") and removes empty entries; inputs using only "\n" will not be split by this helper.
- The class is intended for test use and does not provide synchronization; concurrent access from multiple threads is not guaranteed to be safe.


---

## TestIrcConnectionFactory
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal static class TestIrcConnectionFactory
```


Creates a convenient factory for constructing IrcClientConnection instances that are wired to an in-memory TestDuplexStream, enabling deterministic unit tests of IRC-related behavior without a real network. Use Create to supply a set of incoming lines that the client will read from; the method returns both the constructed IrcClientConnection and the TestDuplexStream so you can inspect what the client writes. Use CreateAuthenticated to obtain a connection that is already registered and authenticated, with nickname/username and a UserId, so tests can focus on post-auth flow without performing login or handshake.

## Remarks
TestIrcConnectionFactory centralizes test harness setup, reducing boilerplate in tests that exercise IRC server interaction. It returns both the connection and the stream to let tests feed input and observe output, including greeting lines or protocol messages. The authenticated variant preconfigures identity and flags (IsRegistered and IsAuthenticated) to simulate a fully connected client, enabling tests that assume a ready-to-use session.

## Example
```csharp
// Example: raw connection with input lines
var (conn, stream) = TestIrcConnectionFactory.Create("PING :server", ":server 001 :Welcome");

// Example: authenticated connection
var (authConn, authStream) = TestIrcConnectionFactory.CreateAuthenticated(nickname: "alice");
```

## Notes
- The factory creates a real TcpClient under the hood; the IrcClientConnection uses that client but all I/O is performed via the in-memory TestDuplexStream, so tests must dispose the resources (the stream and client) when finished to avoid leaks.
- CreateAuthenticated defaults nickname to "alice" and generates a new GUID for UserId if none is supplied; pass explicit values for deterministic testing.

---