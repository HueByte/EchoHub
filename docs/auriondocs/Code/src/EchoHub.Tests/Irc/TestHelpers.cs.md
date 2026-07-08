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


A lightweight test double for IChannelService that returns pre-configured results for each operation. Use this in unit tests to control and assert how code reacts to different channel-service outcomes without performing any I/O or invoking the real service.

## Remarks
This fake exists to isolate code under test from the real channel infrastructure: each public property lets a test set the exact response the service should produce (topics, lists, per-operation results, membership outcome). The implementations immediately return those configured values wrapped in completed Tasks, making it easy to inject success and failure cases into tests.

## Example
```csharp
// Arrange
var fake = new FakeChannelService
{
    TopicResult = ("announcements", true),
    DeleteResult = ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not allowed")
};

// Act
var (topic, exists) = await fake.GetChannelTopicAsync("general");
var deleteResult = await fake.DeleteChannelAsync(Guid.NewGuid(), "general");

// Assert
// topic == "announcements"; exists == true
// deleteResult.IsSuccess == false and contains the configured error
```

## Notes
- CreateChannelAsync, UpdateTopicAsync and DeleteChannelAsync return the configured ChannelOperationResult if set; otherwise they return ChannelOperationResult.Fail(ChannelError.ValidationFailed, "Not configured").
- GetChannelsAsync always returns an empty PaginatedResponse (total 0) — it is not configurable through the public properties.
- All methods return completed tasks via Task.FromResult (synchronous completion), which can hide timing/async behavior differences compared to a real asynchronous implementation.


---

## FakeChatService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeChatService : IChatService
```


A lightweight, in-memory test double for IChatService that records incoming calls and returns pre-configured results. Use this in unit tests when you need to assert that chat-related methods were invoked with expected parameters or when you want to control what the service returns (history, errors, online users, channels) without contacting a real chat backend.

## Remarks
This class exists purely for test scenarios: it exposes public mutable lists that capture the parameters passed to each method and properties that let tests configure what the service methods should return. It does not attempt to model real server behavior, validation, or persistence — callers receive whatever values the test placed on the configurable properties and the fake records whatever parameters were supplied.

## Example
```csharp
// Arrange
var fake = new FakeChatService();
fake.HistoryToReturn = new List<MessageDto>();
fake.JoinError = null; // simulate success

// Act
var (history, error) = await fake.JoinChannelAsync("conn-1", Guid.NewGuid(), "alice", "general");

// Assert (pseudo-assertions shown as comments)
// error == null
// history == fake.HistoryToReturn
// fake.JoinedChannels should contain ("general", "alice")
```

## Notes
- The fake is not thread-safe: its public `List<T>` properties are mutable and can be updated or read concurrently without synchronization.
- Methods return the configured properties as-is; for example GetChannelHistoryAsync ignores the count and offset parameters and always returns HistoryToReturn.
- Some methods are no-ops beyond recording calls (BroadcastMessageAsync, BroadcastChannelUpdatedAsync) and some always return null unless a test sets the corresponding error property (e.g., SendMessageAsync returns SendMessageError).
- SentMessages records only (channel, content) and does not store the username.


---

## FakeEncryptionService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeEncryptionService : IMessageEncryptionService
```


FakeEncryptionService is a test double that implements IMessageEncryptionService using a simple, reversible prefix-based scheme: Encrypt('hello') → '$ENC$hello' and Decrypt('$ENC$hello') → 'hello'. It leaves content unchanged if it does not start with the prefix, making it a predictable stand-in for real encryption in tests.

## Remarks
FakeEncryptionService provides a lightweight, deterministic mock of encryption to satisfy IMessageEncryptionService dependencies in tests without cryptographic logic. It uses a fixed prefix to make encrypted strings easy to spot and to verify round-trips in assertions. The nullable wrappers preserve null values, aligning with typical API expectations for optional inputs. Being internal to the test assembly, it is clearly a test-scoped helper.

## Notes
- Not secure; use only as a test double, not production encryption.
- Decrypt is permissive: non-prefixed content is returned unchanged rather than failing.
- EncryptNullable/DecryptNullable preserve null input, which helps with null-safety in tests.

---

## FakeUserService
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class FakeUserService : IUserService
```


A lightweight test double implementing IUserService that returns pre-configured results for authentication, registration and profile queries. Set the AuthResult, RegisterResult and ProfileToReturn properties to control what each method returns; when those properties are not set the service returns sensible default failures (invalid credentials / already exists) or null for profile lookups. Use this in unit tests where you need a predictable IUserService without a mocking framework.

## Remarks
This class exists as a pragmatic replacement for more complex mocks in tests: it exposes mutable properties that let a test specify the exact UserOperationResult or UserProfileDto to be returned. All methods complete synchronously using Task.FromResult which keeps test execution deterministic. The SuccessResult helper constructs a successful UserOperationResult containing a UserProfileDto with UTC timestamps — handy when a test needs a ready-made successful response.

## Example
```csharp
// Configure a successful authentication result
var fake = new FakeUserService();
var userId = Guid.NewGuid();
fake.AuthResult = FakeUserService.SuccessResult(userId, "alice");

var authResult = await fake.AuthenticateUserAsync("alice", "irrelevant");
// authResult.IsSuccess == true

// Configure a profile to be returned by GetUserProfileAsync / GetUserByIdAsync
fake.ProfileToReturn = new UserProfileDto(
    userId, "alice", null, null, null, null,
    UserStatus.Online, null, ServerRole.Member,
    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

var profile = await fake.GetUserProfileAsync("alice");
// profile is the instance assigned above
```

## Notes
- Methods return already-completed tasks via Task.FromResult; they do not perform asynchronous work or scheduling.
- The class is not synchronized; mutable properties (AuthResult, RegisterResult, ProfileToReturn) are not thread-safe for concurrent test use.
- UpdateProfileAsync and SetAvatarAsync always return a NotFound failure with message "Not configured". If a test needs different behavior for those operations, replace or extend this fake.

---

## TestDuplexStream
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal sealed class TestDuplexStream : Stream
```


A lightweight in-memory duplex Stream for tests that supplies a predefined input (read side) and captures all written bytes (write side). Use this when you need to simulate a readable input source and observe what a component writes without involving real network or console IO.

## Remarks
TestDuplexStream pairs two MemoryStream instances: one preloaded with UTF-8 bytes supplied via the constructor (the read buffer) and one that accumulates anything written to the stream (the write buffer). It implements the common Stream read/write/async variants so it can be passed to APIs that operate on Stream, while intentionally not supporting seeking. The captured output is returned as a UTF-8 string; a leading UTF-8 BOM emitted by StreamWriter is removed by GetOutput to make textual comparisons in tests simpler.

## Example
```csharp
// Preload input that consumer will read
using var duplex = new TestDuplexStream("HELLO\r\n");

// Consumer under test might read from the stream; tests can also write to it
using var writer = new StreamWriter(duplex, Encoding.UTF8, leaveOpen: true);
writer.WriteLine("OK");
writer.Flush();

// Inspect the captured output as a single string or lines
string output = duplex.GetOutput();
List<string> lines = duplex.GetOutputLines();
```

## Notes
- The stream does not support seeking: Length, Position, Seek and SetLength throw NotSupportedException.
- The read buffer is a one-time MemoryStream: reads advance its position and consumed bytes are not automatically reset.
- GetOutput trims a leading UTF-8 BOM (\uFEFF) because StreamWriter may emit one; comparisons should account for that behavior.
- TestDuplexStream is a simple test helper and does not provide synchronization for concurrent readers/writers; coordinate access in multithreaded tests.

---

## TestIrcConnectionFactory
> **File:** `src/EchoHub.Tests/Irc/TestHelpers.cs`  
> **Kind:** class

```csharp
internal static class TestIrcConnectionFactory
```


TestIrcConnectionFactory is a testing helper that builds IrcClientConnection instances backed by an in-memory duplex stream for unit tests. It avoids real network I/O by pairing a TcpClient with a TestDuplexStream and returning both the connection and the test stream. It exposes two helpers: Create, which constructs a connection from a sequence of incoming IRC lines, and CreateAuthenticated, which returns a pre-authenticated, registered connection ready to use in tests. The authenticated variant uses nickname 'alice' by default and assigns a new GUID to UserId when one isn't supplied.

## Remarks

The design isolates test scaffolding from production code by providing a deterministic stream-based IO surface. Tests can feed input lines into the TestDuplexStream and inspect outbound data via GetOutput or GetOutputLines. The returned tuple contains both the IrcClientConnection and the TestDuplexStream, enabling tests to drive the handshake, simulate server messages, and verify the client's responses. The authenticated variant wires IsRegistered and IsAuthenticated to true and populates Nickname, Username, and UserId (generating a new GUID when not provided).

## Notes

- The input is assembled by joining provided lines with CRLF and appending a trailing CRLF if any lines are given, which affects the boundary of the simulated server messages.
- A new TcpClient is created for each call, but it is not connected; this helper exists purely to satisfy the IrcClientConnection constructor in tests.
- Using CreateAuthenticated makes tests assume a pre-authenticated state; if you need to test the unauthenticated handshake, use Create and set IsRegistered/IsAuthenticated (or adjust inputs) manually.

---