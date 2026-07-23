# IrcCommandHandler.cs

> **Source:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`

## Contents

- [IrcCommandHandler](#irccommandhandler)
  - [IrcCommandHandler (constructor)](#irccommandhandler-constructor)
  - [ServerName](#servername)
  - [HandleAuthenticateAsync](#handleauthenticateasync)
  - [HandleAwayAsync](#handleawayasync)
  - [HandleCapAsync](#handlecapasync)
  - [HandleCommandAsync](#handlecommandasync)
  - [HandleJoinAsync](#handlejoinasync)
  - [HandleListAsync](#handlelistasync)
  - [HandleModeAsync](#handlemodeasync)
  - [HandleNamesAsync](#handlenamesasync)
  - [HandleNickAsync](#handlenickasync)
  - [HandlePartAsync](#handlepartasync)
  - [HandlePassAsync](#handlepassasync)
  - [HandlePingAsync](#handlepingasync)
  - [HandlePrivmsgAsync](#handleprivmsgasync)
  - [HandleQuitAsync](#handlequitasync)
  - [HandleTopicAsync](#handletopicasync)
  - [HandleUserAsync](#handleuserasync)
  - [HandleWhoAsync](#handlewhoasync)
  - [IrcToEchoHubChannel](#irctoechohubchannel)
  - [RequireRegisteredAsync](#requireregisteredasync)
  - [RunAsync](#runasync)
  - [SendChannelTopicAsync](#sendchanneltopicasync)
  - [SendModeErrorAsync](#sendmodeerrorasync)
  - [SendMotdAsync](#sendmotdasync)
  - [SendNamesReplyAsync](#sendnamesreplyasync)
  - [SendWelcomeBurstAsync](#sendwelcomeburstasync)
  - [TryCompleteRegistrationAsync](#trycompleteregistrationasync)
- [HandleWhoisAsync](#handlewhoisasync)

---

## IrcCommandHandler
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** class

```csharp
public sealed class IrcCommandHandler
```


Acts as the main dispatcher that translates incoming IRC protocol messages from a single IrcClientConnection into actions against the server-side chat, user and channel services. Reach for this class when you need the gateway that accepts an IRC client, performs registration/authentication (SASL or PASS), sends the welcome/MOTD burst, and maps channel/query commands (JOIN, PART, PRIVMSG, NAMES, TOPIC, WHO/WHOIS, LIST, etc.) into the server's chat/channel/user APIs.

## Remarks
This sealed handler centralizes IRC protocol handling for one client connection. It coordinates authentication (including SASL and PASS fallbacks), completes IRC registration (NICK/USER), and then routes post-registration commands to the underlying IChatService, IUserService, and IChannelService. It also enforces gateway-specific policies mentioned in the source comments: encrypted rooms are not readable over IRC (so joins are blocked), system channels are not proxied to IRC, and private channels are omitted from LIST results. Message history replay must be decrypted via IMessageEncryptionService before being sent to the IRC client.

## Notes
- Encrypted rooms are intentionally blocked from JOIN over the IRC gateway: the gateway does not hold room keys and therefore cannot expose encrypted room contents to IRC clients.  
- Private channels are hidden from LIST to match the SignalR client's channel visibility; expect LIST to only include public/discoverable channels.  
- Registration and authentication have multiple paths (SASL, PASS, or account registration fallback); callers should expect asynchronous authentication flow and that RunAsync accepts a CancellationToken to stop processing.

---

### IrcCommandHandler (constructor)
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** constructor

```csharp
public IrcCommandHandler(
        IrcClientConnection conn,
        IrcOptions options,
        IChatService chatService,
        IUserService userService,
        IChannelService channelService,
        IMessageEncryptionService encryption,
        ILogger logger)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `conn` | [`IrcClientConnection`](IrcClientConnection.cs.md) | — |
| `options` | [`IrcOptions`](IrcOptions.cs.md) | — |
| `chatService` | [`IChatService`](../EchoHub.Core/Contracts/IChatService.cs.md) | — |
| `userService` | [`IUserService`](../EchoHub.Core/Contracts/IUserService.cs.md) | — |
| `channelService` | [`IChannelService`](../EchoHub.Core/Contracts/IChannelService.cs.md) | — |
| `encryption` | [`IMessageEncryptionService`](../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) | — |
| `logger` | `ILogger` | — |


The constructor initializes an IrcCommandHandler by wiring together its required collaborators: IrcClientConnection, IrcOptions, IChatService, IUserService, IChannelService, IMessageEncryptionService, and ILogger. It stores these dependencies in private fields so the command-handling logic can access the IRC connection, configuration, chat and user/channel services, encryption features, and logging throughout command processing. This pattern follows dependency injection, enabling easy testing with mocks and seamless composition by the application’s DI container at startup.

## Remarks
By aggregating these collaborators, the constructor centralizes the wiring of core capabilities—network I/O, configuration, domain services for chat, user and channel state, cryptographic operations, and observability—so command processing remains focused on business logic rather than setup. This design promotes testability, consistency, and clear separation of concerns within the IRC subsystem.

## Notes
- The constructor as shown does not perform null checks; ensure the DI container enforces non-null registrations or add guards in production code.
- Be mindful of lifetime management: the handler should typically share lifetimes with its collaborators or be disposed in tandem to avoid resource leaks.

---

### ServerName
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** property

```csharp
private string ServerName => _options.ServerName
```


This private read-only property exposes the server name configured in the handler’s options by forwarding to _options.ServerName. It should be used whenever the command handler needs the target IRC server name, offering a single indirection point if the source of that value changes in the future.

## Remarks

By wrapping the access in ServerName, you decouple usage from the underlying options data. This centralization makes future changes (like deriving the server name from a different config source or applying normalization) localized to this property. It also communicates that the server name is a configuration concern and not a computed field of the handler itself.

## Notes

- The property simply forwards to _options.ServerName; it does not perform validation or mutation.
- If _options.ServerName can change at runtime, callers may observe updates on subsequent accesses.

---

### HandleAuthenticateAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleAuthenticateAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Handles SASL authentication for a connected IRC client by processing the SASL-related AUTHENTICATE messages. It supports initiating SASL with PLAIN, aborting SASL, and performing the actual PLAIN payload verification, ultimately authenticating or registering the user, and then updating the connection state and sending appropriate IRC numeric replies. 

## Remarks
This method centralizes SASL negotiation within the command handler, bridging the IRC SASL protocol with the application's user store. It redacts the password in logs and relies on the user service to either authenticate or register the user, enabling a smooth first-time login flow. It validates payload structure and wraps the process in a catch block to translate unexpected errors into SASL failure feedback while preserving a consistent connection state.

## Notes
- The SASL payload must decode to a null-delimited string yielding at least three parts; malformed payloads trigger an ERR_SASLFAIL response. 
- The code derives the username from parts[1] when present, otherwise parts[0], and normalizes it to lowercase; the actual password is sourced from a redacted variable and is not logged. 
- On success, the connection's Nickname and UserId are populated, the connection is marked authenticated, and the client receives both a LOGGEDIN notice and a SASL success reply; failures emit ERR_SASLFAIL and are logged for auditing.


---

### HandleAwayAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleAwayAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


HandleAwayAsync processes a user's away status in response to the IRC AWAY command. It first ensures the caller is registered; if not, it exits early. When a non-empty parameter is supplied, it stores that string as the away message on the connection, updates the user's status to Away via the chat service, and sends a RPL_NOWAWAY reply to the client. If no parameter is provided, it clears the away message, updates the status to Online with a null message, and sends a RPL_UNAWAY reply. The method is asynchronous, so it does not block the command handling path while performing persistence and network communication.

## Remarks
Consolidates away-state handling in a single place so all callers see the same effect on status and client notification. It keeps IrcCommandHandler lean by delegating away management and relies on _chatService to persist user state. It uses a simple, deterministic flow based on whether a message parameter is provided.

## Notes
- Accessing _conn.UserId with the null-forgiving operator assumes RequireRegisteredAsync succeeded; calling this method without a valid registered session could throw a NullReferenceException.
- This method sends numeric replies (RPL_NOWAWAY / RPL_UNAWAY) to the connected client; ensure ServerName and _conn are valid at call time.

---

### HandleCapAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleCapAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


HandleCapAsync processes CAP negotiation commands from the IRC server. It requires at least one parameter; if none are provided, it returns without action. It switches on the upper-cased first parameter to implement the SASL/capability handshake: on LS it requests the sasl capability and marks negotiation as in progress; on REQ it either acknowledges the 'sasl' request and enables SASL, or responds with NAK for the requested capability; on END it ends negotiation and, if identity information is available and the client is not yet registered, triggers a registration attempt via TryCompleteRegistrationAsync.

## Remarks
This method centralizes the CAP negotiation lifecycle for the IRC connection, coordinating with the connection state (_conn) to track whether a CAP negotiation is underway, whether SASL is engaged, and whether registration has completed. By encapsulating the protocol specifics here, it avoids scattering CAP handling logic across multiple handlers and ensures correct sequencing between CAP negotiation, SASL activation, and user registration.

## Notes
- The method short-circuits when there are no parameters, avoiding potential null-reference issues.
- The REQ path treats a missing or non-matching second parameter as a NAK for the requested capability, preserving protocol safety.
- END clears the negotiation flag and only triggers registration if Nickname and Username are non-null and the client is not already registered, preventing premature or repeated registration attempts.


---

### HandleCommandAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private Task HandleCommandAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Dispatches incoming IRC commands by normalizing the command to uppercase and routing to the corresponding per-command asynchronous handler, centralizing the IRC command handling logic (e.g., CAP -> HandleCapAsync, PRIVMSG -> HandlePrivmsgAsync). If the command is unknown, it responds with the ERR_UNKNOWNCOMMAND numeric back to the client.

## Remarks
Centralizes command dispatch behind a single switch expression, mapping command strings to their asynchronous handlers. This design makes it straightforward to extend support for new commands by adding a new case to the switch. It returns a Task to support asynchronous work and relies on the private _conn to send numeric replies back to the client; some branches return Task.CompletedTask to represent no-op work for certain commands (e.g., PONG).

## Example
```csharp
// Example: dispatch flow for a known command
IrcMessage msg = /* ... */;
await HandleCommandAsync(msg); // if msg.Command == "PRIVMSG" this path invokes HandlePrivmsgAsync(msg)
```

## Notes
- Unknown commands trigger an ERR_UNKNOWNCOMMAND reply, authored with the server name and the raw command.
- The PONG path is treated as a no-op by returning Task.CompletedTask, avoiding unnecessary asynchronous work.


---

### HandleJoinAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleJoinAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Handles an IRC JOIN request for a registered user, performing parameter validation, channel-name mapping, and policy checks before joining the user to each requested channel. It delegates to backend services to perform the join, then updates the client with a JOIN confirmation, channel topic, NAMES list, and a decrypted history replay.

## Remarks
This function centralizes the join workflow for the IRC gateway and enforces privacy and policy constraints: end-to-end encrypted channels and server-managed system channels are blocked from IRC joins, ensuring the EchoHub client remains the source of truth for restricted channels. It coordinates with the connection object, channel service, and chat service to validate input, perform joins per channel (supporting RFC 1459-style per-channel keys), and synchronize the IRC client view (JOIN message, topic, NAMES, and history).

## Notes
- Requires the user to be registered; if not, the method exits early and no join is attempted.
- If there are fewer than one parameter, the gateway responds with ERR_NEEDMOREPARAMS to indicate insufficient input.
- For each channel, invalid channel names yield ERR_NOSUCHCHANNEL with an invalid channel notice.
- End-to-end encrypted channels are blocked from IRC joins; use the EchoHub client for such channels.
- System channels are blocked from IRC joins because they stream content over SignalR; use the EchoHub client for access.
- When a channel join requires a password and the provided key is incorrect or missing, the gateway responds with ERR_BADCHANNELKEY.
- History is replayed after joining, with Content and any embedded replies decrypted for proper IRC presentation.

---

### HandleListAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleListAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Implements the IRC LIST command for the server. After verifying the client is registered, it fetches the channel list from the channel service, filters to public channels, and sends one RPL_LIST reply per channel containing the channel name, online user count, and topic. If a channel is protected, a [+k] lock hint is prefixed to the topic. Private channels are intentionally hidden to match what the SignalR client sees. Once all public channels have been reported, it sends RPL_LISTEND to signal completion.

## Remarks
This handler encapsulates the server-side semantics of channel discovery separate from the client protocol encoding. By filtering to IsPublic channels, it keeps private channels from being exposed to clients, preserving privacy where appropriate. The lock indicator (+k) encodes channel protection state in the LIST output, while the Topic is plumbed directly into the listing, enabling clients to present useful metadata without additional requests. The approach keeps channel management in _channelService and I/O in _conn, promoting testability and a clean separation between data retrieval and protocol signaling.

## Notes
- The method requires a registered user; unauthenticated users will cause the method to return early without emitting LIST data due to the initial RequireRegisteredAsync check.
- Private channels are hidden by design via the IsPublic filter; modify the filter only if you intend to expose private channels and ensure client expectations are updated accordingly.


---

### HandleModeAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleModeAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


HandleModeAsync processes incoming MODE commands for channels and queries. It first ensures the caller is registered, then validates parameters and resolves the IRC target. For channel targets, it either returns the current channel mode or applies mode changes (notably +k to set a channel password and -k to clear it), persisting changes through the channel service and signaling results with the appropriate IRC numerics. When the target is not a channel, it responds with the user-mode indicator (+) to indicate no user modes are reported. If the channel cannot be resolved, it returns ERR_NOSUCHCHANNEL. When querying a channel's mode (MODE #channel with no extra parameters), it responds with RPL_CHANNELMODEIS and, if the channel is protected, indicates +k. For mode changes, it handles +k (requiring a key) and -k (clearing the key); unknown modes yield ERR_UNKNOWNMODE. A small, targeted behavior detail is that probing the ban list returns an empty list via RPL_ENDOFBANLIST to mirror common client expectations during join.

## Dependencies
- MODE
- Parameters
- IrcNumericReply

## Dependency APIs (verified signatures)
- property `Parameters` (`src/EchoHub.Server.Irc/IrcMessage.cs`)
- class [`IrcNumericReply`](IrcNumericReply.cs.md) (`src/EchoHub.Server.Irc/IrcNumericReply.cs`)
  - field `string RPL_WELCOME`
  - field `string RPL_YOURHOST`
  - field `string RPL_CREATED`
  - field `string RPL_MYINFO`
  - field `string RPL_ISUPPORT`
  - field `string RPL_MOTDSTART`
  - field `string RPL_MOTD`
  - field `string RPL_ENDOFMOTD`
  - field `string ERR_NOMOTD`
  - field `string RPL_NOTOPIC`
  - field `string RPL_TOPIC`
  - field `string RPL_NAMREPLY`
  - …and 35 more member(s) not shown

## Symbol To Document
- Name: HandleModeAsync
- Kind: method
- File: src/EchoHub.Server.Irc/IrcCommandHandler.cs
- Language: csharp
- ID: 7207cb42-b229-42e0-9b0c-126018e8c975

---

### HandleNamesAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleNamesAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


HandleNamesAsync processes an incoming NAMES-like query for the IRC command handler. It first ensures the caller is registered by awaiting RequireRegisteredAsync; if the user is not registered, the method exits early to prevent exposing channel membership information to unauthorized callers. It then requires at least one parameter; if none are provided, it returns without a response. It converts the first parameter to the internal EchoHub channel using IrcToEchoHubChannel; if this mapping yields null, the method again exits. When all preconditions succeed, it issues the names response for the mapped channel by calling SendNamesReplyAsync with that channel.

## Remarks
This method centralizes the NAMES query flow, isolating authentication, input validation, and channel-name resolution from the response formatting logic. It enforces that only authenticated, well-formed requests proceed to produce a response, contributing to predictable and secure command handling.

## Notes
- Silent declines: if preconditions fail (not registered, missing parameters, or invalid channel mapping), the method returns without emitting a response.
- The mapping function (IrcToEchoHubChannel) determines whether an IRC channel reference has a corresponding internal EchoHub channel; a null result means no valid target was found, and no response is produced.

---

### HandleNickAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleNickAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Handles the NICK command from a connected IRC client. It validates that a nickname parameter is supplied, enforces the server's username rules, stores a canonical lowercase nickname on the connection, and, when applicable, advances the registration flow by attempting to complete registration if a username is already present.

## Remarks
Centralizes nickname processing in the command handler to ensure consistent validation, normalization, and state progression. It uses numeric replies to communicate issues back to the client (missing nickname or invalid nickname) and coordinates with the registration logic via TryCompleteRegistrationAsync once the client is partially authenticated. Normalizing to lowercase provides a stable internal identity, independent of the client's casing.

## Notes
- The error text for invalid nicknames lists allowed characters and length; confirm that UsernameRegex() and the user-visible message remain in sync to avoid misleading users.

---

### HandlePartAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandlePartAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Leaves one or more IRC channels as requested by an incoming IrcMessage. It first ensures the caller is registered; if not, it exits early without issuing any IRC traffic. It expects at least one parameter; the first parameter is a comma-separated list of raw channel names, and an optional second parameter carries a part message to be appended after PART. For each channel in the list, the method translates the raw channel into an internal channel name using IrcToEchoHubChannel; if mapping returns null, that channel is skipped. It then tells the chat service to leave the mapped channel, updates the local connection state by calling LeaveChannel, and finally emits the IRC PART command for that channel, including the optional message.

## Remarks
Acts as a coordination boundary between the IRC protocol and the application's connection state. It encapsulates registration verification, channel translation, state mutation, and protocol emission in a single command path. Because it awaits each channel in sequence, multiple PARTs are issued in order rather than in parallel.

## Notes
- Early returns ensure no actions occur if the user is not registered or if no channels are specified.
- Channels that cannot be translated via IrcToEchoHubChannel are skipped without error.
- The emitted PART command uses the hostmask and a '#channel' target, and appends an optional message if provided.

---

### HandlePassAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private Task HandlePassAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


HandlePassAsync is a private helper in the IRC command handling flow that processes the PASS command for a connection. It blocks re-registration by sending ERR_ALREADYREGISTERED when the connection is already registered, and if a password parameter is provided, it routes that parameter to the connection’s password handling path (the actual value is redacted in this snippet). In cases where neither condition applies, it completes without performing additional work.

## Remarks
This abstraction centralizes PASS command handling within the command handler to ensure consistent protocol error signaling and password processing across the handshake sequence. It delegates state management and messaging to the underlying connection object, which keeps the command dispatch logic focused and testable. The explicit redaction of the password demonstrates a security-conscious approach to handling sensitive data, avoiding exposure in logs or snapshots. By returning a Task, the method remains composable with the asynchronous command pipeline.

## Notes
- If a PASS parameter is provided, ensure proper validation and secure handling of the credential; the actual value is redacted here, so verify correctness in your environment.
- The method relies on external state (_conn.IsRegistered) and may either complete synchronously or proceed asynchronously via SendNumericAsync; callers should await as appropriate to preserve command-ordering guarantees.
- This function does not perform full authentication itself; it coordinates with the connection object for state and output, acting as a gateway in the PASS handling path.

---

### HandlePingAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandlePingAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Responds to IRC PING messages by sending a corresponding PONG back to the server to keep the connection alive. It chooses the token to include in the PONG from the incoming message: if a parameter is present, that token is used; otherwise it falls back to the server name. The response is sent using the underlying connection with the format :<ServerName> PONG <ServerName> :<token>.

## Remarks
Internally, this method serves as the keep-alive handler for the IRC command flow. By basing the PONG on ServerName and the received parameters, it guarantees a consistent reply format and avoids leaking raw protocol details to higher layers. It relies on the ServerName and Parameters dependencies and on the underlying connection to transmit the response.

## Example
```csharp
// Example: a PING with a token results in a PONG containing that token
var token = "12345";
// Assuming ServerName is "irc.example.org"
var response = $":{ServerName} PONG {ServerName} :{token}";
// The actual send occurs via _conn.SendAsync in HandlePingAsync
```

## Notes
- If msg.Parameters is empty, token defaults to ServerName.
- The method is private; usage is internal to the IrcCommandHandler and not exposed publicly.
- Exceptions from SendAsync propagate; callers may need to log or retry as part of larger connection management.

---

### HandlePrivmsgAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandlePrivmsgAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Handles the PRIVMSG command by validating parameters, ensuring the sender is registered, and routing channel-targeted messages to the EchoHub chat service. It rejects private messages (targets that do not start with '#') with an appropriate error and surfaces delivery failures back to the IRC client.

## Remarks
This method acts as a boundary between IRC protocol handling and the EchoHub chat system. It enforces channel-only messaging for PRIVMSG, consolidates parameter validation and error reporting via IRC numeric replies, and delegates the actual delivery to a dedicated chat service. By encapsulating channel-name translation (IrcToEchoHubChannel) and the delivery call (SendMessageAsync), it keeps command handling focused and testable, while remaining resilient to mapping failures and chat-service errors.

## Notes
- Requires the current connection to be registered; otherwise the operation is short-circuited.
- If the PRIVMSG target does not begin with '#', a private-message error is returned: ERR_NOSUCHNICK with a hint to use channels.
- If channel name mapping returns null, the method exits without performing delivery.
- If SendMessageAsync reports an error, the client receives ERR_CANNOTSENDTOCHAN to indicate delivery failure to the channel.

---

### HandleQuitAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleQuitAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Handles a quit event by sending an IRC ERROR line to the active connection that signals the closing of the link. It derives the quit reason from the first parameter of the incoming IrcMessage when provided, otherwise it uses 'Client quit' as a default.

## Remarks
Centralizes the termination messaging for quit scenarios, ensuring a consistent closing notice across paths that terminate a connection. It formats the message with the current nickname and the resolved quit reason, and delegates the actual network transmission to _conn.SendAsync, keeping the higher-level quit flow simple and testable.

## Example
```csharp
// Example usage within the same class (quit with a reason)
var msg = new IrcMessage { Parameters = new List<string> { "Server maintenance" } };
await HandleQuitAsync(msg);
```

## Notes
- This method only sends the closing line; it does not by itself terminate the connection. The caller should close the connection after the message is sent.
- It relies on msg.Parameters[0] as the quit reason; if there are multiple parameters, only the first is used.
- Assumes msg.Parameters is non-null; if it's null, this will throw a NullReferenceException.

---

### HandleTopicAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleTopicAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


HandleTopicAsync processes the IRC TOPIC command for a channel. It verifies the caller is registered, resolves the channel name from the command parameters, and either sends the current topic or updates it via the channel service using the provided topic text. On a successful update, it broadcasts the change to SignalR clients and echoes the topic back to the IRC client; on failure it returns an appropriate IRC numeric error.

## Remarks
This method acts as the integration point between IRC command handling, domain services, and client notifications. It relies on the authentication check (RequireRegisteredAsync) and uses the channel service to persist topic changes while informing connected clients through the chat service. Numeric errors are produced through IrcNumericReply based on the nature of the failure (non-existent channel vs. insufficient privileges), ensuring correct IRC protocol behavior. When a topic is cleared, a whitespace topic is treated as null and passed to UpdateTopicAsync, signaling a topic removal.

## Notes
- The method returns early if the caller is not registered or if there are insufficient parameters, preventing unintended state changes.
- It uses a null-forgiving operator on UserId when updating the topic; preconditions ensure a valid user context.
- Topic clearing is achieved by passing null to UpdateTopicAsync when the provided topic string is whitespace.
- Error handling maps ChannelError.NotFound to ERR_NOSUCHCHANNEL and all other failure cases to ERR_CHANOPRIVSNEEDED, aligning with IRC protocol expectations.

---

### HandleUserAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleUserAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Handles the USER command as part of the IRC registration handshake. It first rejects re-registration attempts, then validates that enough parameters are present, stores the provided username and real name on the connection, and finally triggers registration completion if a nickname has already been supplied.

## Remarks
This method encapsulates the user-side portion of the registration flow, coordinating between the incoming command data (via IrcMessage.Parameters) and the connection state. By separating the completion trigger (TryCompleteRegistrationAsync) from initial USER parsing, it keeps the registration logic cohesive and allows the NICK/USER agreement to occur in any order. It relies on the server-generated numeric replies to communicate errors back to the client and uses the connection state to decide when registration can advance.

## Notes
- Parameter indexing assumes four parameters for a valid USER command; if fewer are provided, the handler responds with ERR_NEEDMOREPARAMS. The RealName is taken from Parameters[3], which is a potential source of off-by-one mistakes if the protocol is extended or parameters are reformatted.
- A full registration is only completed when a nickname is already present; otherwise, the method merely populates Username and RealName and leaves completion to a later trigger when Nickname arrives.
- The code does not validate that Username or RealName are non-empty; additional validation may be needed if stricter user data integrity is required.

---

### HandleWhoAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleWhoAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Responds to an IRC WHO request for a channel by listing online users and signaling completion. It is invoked when a registered client asks for the current participants of a channel; it maps the supplied channel parameter to EchoHub's channel, retrieves online users via the chat service, and streams RPL_WHOREPLY rows followed by RPL_ENDOFWHO to the client.

## Remarks
By translating EchoHub's channel membership into IRC WHO semantics, this method acts as the bridge between the IRC protocol and the chat model. It performs early guards (registration and parameter validation) before querying the chat service, ensuring consistent behavior and preventing unnecessary work for unauthenticated callers. Each user is emitted with a RPL_WHOREPLY line containing their nick, username, server, and away/here flag, followed by a final EndOfWho line to signal completion.

## Notes
- Always emits an End of WHO line even if the channel has no online users.
- Away vs. here status is encoded as 'G' for away and 'H' for present, matching IRC conventions.

---

### IrcToEchoHubChannel
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private static string? IrcToEchoHubChannel(string ircChannel)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `ircChannel` | `string` | — |

**Returns:** `string?`


Converts a raw IRC channel into a canonical EchoHub channel name by stripping the leading '#', lowercasing, and trimming the remainder, returning null if the result does not satisfy ValidationConstants.ChannelNameRegex. This is used when bridging IRC channels to EchoHub to obtain a policy-compliant channel identifier.

## Remarks
Centralizes the logic for translating IRC-style channels into EchoHub identifiers and enforces channel naming policy via ValidationConstants.ChannelNameRegex. It returns a lowercase, trimmed name when valid, or null when the input cannot be mapped, allowing callers to handle non-mappable channels explicitly.

## Notes
- If ircChannel is null, this method will throw a NullReferenceException; callers should ensure a non-null value before calling.
- Results are always lowercase due to ToLowerInvariant, providing a consistent channel namespace.
- A non-matching input yields null rather than an exception, signaling an unmapped channel to the caller.

---

### RequireRegisteredAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<bool> RequireRegisteredAsync()
```

**Returns:** `Task<bool>`


RequireRegisteredAsync is a small helper that enforces a precondition: the client connection must be registered before proceeding with commands that require registration. It returns true when the connection is already registered; otherwise it sends the IRC error reply ERR_NOTREGISTERED and returns false. Callers await this method to guard subsequent operations and avoid duplicating boilerplate checks across command handlers.

## Remarks
This abstraction centralizes the registration precondition and the associated user feedback. It guarantees consistent behavior by issuing the standard ERR_NOTREGISTERED along with the message You have not registered, matching the IRC protocol's expectations, and it short-circuits command execution when the precondition isn’t met.

## Example
```csharp
// Usage: ensure the user is registered before issuing a command that requires registration
if (!await RequireRegisteredAsync())
{
    return; // bail out if not registered
}

// proceed with the operation that requires registration
```

## Notes
- Ensure the caller returns immediately when RequireRegisteredAsync() returns false to avoid sending duplicate replies.
- This helper assumes the underlying connection (_conn) and the server name (ServerName) are initialized; null references may occur if called too early.

---

### RunAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
public async Task RunAsync(CancellationToken ct)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `ct` | `CancellationToken` | — |

**Returns:** `Task`


Runs an asynchronous loop that continuously reads lines from the IRC connection, trims trailing CR/LF, ignores blank lines, and dispatches each non-empty message to the IRC command handler until cancellation is requested. This is the core IO loop for processing incoming IRC traffic in the command handler lifecycle; you start it to begin processing and cancel it to stop.

## Remarks
RunAsync is the primary lifecycle loop for the IRC command processor. It reads a raw line via _conn.ReadLineAsync(ct), cleans trailing CR/LF, and skips empty lines before turning the line into an IrcMessage with IrcMessage.Parse. The resulting message is passed to HandleCommandAsync for per-command processing, and any exceptions thrown during that processing are caught and logged to avoid tearing down the loop. Only the HandleCommandAsync call is wrapped in the try-catch; errors in reading, parsing, or line pre-processing may bubble up if they throw, which means callers should supervise the task accordingly.

## Notes
- Exceptions from ReadLineAsync or IrcMessage.Parse are not caught here; they could terminate the loop.
- The loop ends when a null line is read (end of stream) or when the cancellation token is canceled.
- Whitespace-only lines are ignored; lines are trimmed before parsing.

---

### SendChannelTopicAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendChannelTopicAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task`


Fetches the current topic for the specified channel and sends the corresponding IRC numeric to the client. It queries the channel service for (topic, exists) and, if the channel exists, emits RPL_TOPIC when a topic is set or RPL_NOTOPIC when no topic is configured; if the channel doesn't exist, it returns without replying.

## Remarks
By centralizing the topic-resolution and numeric-emission logic in a single private method, this symbol encapsulates the IRC topic-response behavior for channel-related command flow. It hides the implementation details of IrcNumericReply mappings behind a concise interface and ensures consistent message formatting (channel name prefixed with '#', topic payload prefixed with ':') when interacting with the connection and channel services.

## Notes
- If exists is false, the method returns early with no notification to the client.
- When a channel exists but has no topic, a RPL_NOTOPIC reply is sent with the message "No topic is set".

---

### SendModeErrorAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendModeErrorAsync(string channelName, ChannelOperationResult result)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `result` | [`ChannelOperationResult`](../EchoHub.Core/DTOs/CommonDtos.cs.md) | — |

**Returns:** `Task`


Translates a channel operation error into the corresponding IRC numeric response and sends it to the client for the specified channel. It chooses the numeric based on result.Error (NotFound -> ERR_NOSUCHCHANNEL, Forbidden -> ERR_CHANOPRIVSNEEDED, otherwise ERR_KEYSET) and delivers a message containing the channel (prefixed with '#') and the human-readable error via _conn.SendNumericAsync(ServerName, numeric, `#${channelName} :${result.ErrorMessage}`). This method centralizes the error reporting for channel-mode operations so callers don't duplicate the mapping and formatting logic.

## Remarks
By centralizing the error-to-numeric mapping, this method ensures consistent client feedback and prevents duplication of channel-name formatting and error-message construction across callers. It relies on the surrounding class’s _conn and ServerName being available; changes to the mapping or messaging format would affect all mode-error reports produced by this helper.

## Notes
- The error mapping is not exhaustive: any ChannelError value not explicitly NotFound or Forbidden will default to ERR_KEYSET.
- This method is private and intended solely for internal command-handling use; it is not part of the public API.

---

### SendMotdAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendMotdAsync()
```

**Returns:** `Task`


SendMotdAsync is an internal helper that transmits the server's Message of the Day (MOTD) to the connected client. It validates the configured Motd; if it is missing or whitespace it replies with ERR_NOMOTD and stops. Otherwise it sends a MOTD banner with RPL_MOTDSTART, then each newline-delimited line as an RPL_MOTD, trimming CR characters, and ends with RPL_ENDOFMOTD.

This method consolidates MOTD delivery behind a private surface, so higher-level Irc command handlers don't need to know the exact numeric codes or line-breaking semantics. It depends on _conn for transport and _options for the Motd value, and it's a private method intended to be invoked by the MOTD-related command flow.

## Remarks
Encapsulates the formatting and transport of MOTD to ensure consistent behavior across the server. By isolating the MOTD delivery, it keeps the command-handling code focused on protocol logic rather than presentation details.

## Notes
- If Motd is null or whitespace, the method sends ERR_NOMOTD and returns without sending any MOTD lines.
- Each MOTD line is sent as a separate RPL_MOTD message; the code splits on '\n' and trims a trailing '\r' from each line to normalize Windows-style endings. A trailing newline in Motd may produce an empty MOTD line.
- All sends are awaited asynchronous calls to the connection; exceptions propagate to the caller.

---

### SendNamesReplyAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendNamesReplyAsync(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `Task`


Sends the channel’s NAMES list to the IRC client by querying the chat service for online users in the channel, producing a space-separated set of nicknames, and then emitting two standard IRC numerics: RPL_NAMREPLY with the channel and nicklist, and RPL_ENDOFNAMES to mark completion. This method is invoked when handling a NAMES request for a channel, and it centralizes the formatting and numeric-codes so callers don't have to build the response themselves.

## Remarks

This keeps NAMES formatting centralized and aligns with the IRC protocol surface exposed by IrcNumericReply. It delegates data retrieval to _chatService and transmission to _conn, making the implementation resilient to channel naming and user list changes. It also ensures the end-of-list is always signaled after the list is sent, which is essential for IRC clients to know the response is complete.

## Notes
- If no online users are found, the NAMES reply will carry an empty nicklist while still issuing EndOfNames; clients should handle an empty list gracefully.
- Any exceptions raised by GetOnlineUsersAsync or SendNumericAsync bubble up to the caller, so this method assumes the surrounding command handler will decide how to respond to errors.

---

### SendWelcomeBurstAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendWelcomeBurstAsync()
```

**Returns:** `Task`


Sends the IRC welcome burst to a newly connected client by issuing the standard numeric replies (RPL_WELCOME, RPL_YOURHOST, RPL_CREATED, RPL_MYINFO, RPL_ISUPPORT) and then starts the MOTD flow via SendMotdAsync. It uses the current connection's nickname and the server name to populate the messages, and awaits each dispatch to preserve the canonical handshake order.

## Remarks
It centralizes the initial handshake, ensuring a consistent greeting sequence for every new user. By consuming IrcNumericReply codes and composing messages with the live nickname, server name, and current time, it guarantees the client receives both identification and capability information before proceeding. The method delegates the final output of the MOTD to SendMotdAsync, keeping the handshake concerns isolated from the MOTD generation.

## Notes
- Relies on _conn and Nickname being non-null; the null-forgiving operator means a null nickname could yield a greeting with an empty nickname.
- RPL_CREATED uses DateTimeOffset.UtcNow; this stamps the handshake time rather than the server creation date, which may be intentional for the MOTD moment but can be misleading if interpreted as server age.

---

### TryCompleteRegistrationAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task TryCompleteRegistrationAsync()
```

**Returns:** `Task`


Finalizes the user's registration by completing the authentication handshake and establishing an active session. It guards against concurrent registration work by returning early if capability negotiation is still in progress or the user is already registered. If SASL-based authentication has already succeeded (IsAuthenticated and UserId is not null), it marks the connection as registered, notifies the chat service of the connected user, and sends the welcome burst to complete onboarding.

If SASL authentication is not yet complete, it enforces a password-based login: a missing password results in an IRC error and authentication failure. When a password is supplied, it delegates to the user service to authenticate; if that fails, it attempts to register a new user with the provided nickname and password. On a successful outcome, it stores the resulting UserId and Username on the connection, marks the connection as authenticated and registered, signals the chat service that the user has connected, and sends the welcome burst.

The method is a private helper used during the IRC session setup to ensure the connection transitions to a fully authenticated and registered state before normal chat activity begins.

---

## HandleWhoisAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task HandleWhoisAsync(IrcMessage msg)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `msg` | [`IrcMessage`](IrcMessage.cs.md) | — |

**Returns:** `Task`


Handles an IRC WHOIS command by querying the target nick's user profile and returning the standard WHOIS information to the requester. It validates the connection is registered, extracts the nick from the message, fetches the user profile via _userService, and then dispatches a sequence of numeric replies: WHOIS user, WHOISSERVER, and optionally WHOISCHANNELS, RPL_AWAY if the user is away, and RPL_WHOISIDLE with idle and sign-on times, finishing with RPL_ENDOFWHOIS. If no profile exists for the nick, it replies with ERR_NOSUCHNICK. The method relies on asynchronous services and formats times using the profile's LastSeenAt and CreatedAt to populate idle and sign-on data.

## Remarks
This method centralizes the WHOIS response logic for a given nickname, encapsulating the sequence of IRC numeric replies required to convey user information. It coordinates multiple collaborators (the connection, user service, and chat service) to assemble a consistent, standards-compliant response stream without leaking implementation details to callers. The precondition that the connection must be registered is enforced up front, ensuring WHOIS handling only occurs in an appropriate session context.

## Notes
- If the target profile cannot be found, the handler emits ERR_NOSUCHNICK and aborts further replies.
- Idle time is calculated from LastSeenAt and sign-on time from CreatedAt; both are emitted via RPL_WHOISIDLE when available.
- The RPL_WHOISCHANNELS reply is sent only when the user belongs to one or more channels; otherwise this section is omitted.
- Away status (RPL_AWAY) is emitted only if the profile.Status is Away and a StatusMessage exists.

---