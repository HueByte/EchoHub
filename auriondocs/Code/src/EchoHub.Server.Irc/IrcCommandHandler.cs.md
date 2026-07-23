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
  - [HandleWhoisAsync](#handlewhoisasync)
  - [IrcToEchoHubChannel](#irctoechohubchannel)
  - [RequireRegisteredAsync](#requireregisteredasync)
  - [RunAsync](#runasync)
  - [SendChannelTopicAsync](#sendchanneltopicasync)
  - [SendModeErrorAsync](#sendmodeerrorasync)
  - [SendMotdAsync](#sendmotdasync)
  - [SendNamesReplyAsync](#sendnamesreplyasync)
  - [SendWelcomeBurstAsync](#sendwelcomeburstasync)
  - [TryCompleteRegistrationAsync](#trycompleteregistrationasync)

---

## IrcCommandHandler
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** class

```csharp
public sealed class IrcCommandHandler
```


Handles and dispatches IRC client commands for a single connected client and bridges them to the server-side chat services. Reach for `IrcCommandHandler` when you need an adapter that translates IRC commands (registration, authentication, channel operations, queries like `NAMES`/`WHOIS`, messaging) into calls on the backend services ([`IChatService`](../EchoHub.Core/Contracts/IChatService.cs.md), [`IUserService`](../EchoHub.Core/Contracts/IUserService.cs.md), [`IChannelService`](../EchoHub.Core/Contracts/IChannelService.cs.md)) and emits the corresponding IRC replies (welcome burst, MOTD, topic/NAMES lists, etc.).

## Remarks
`IrcCommandHandler` is the protocol-layer coordinator for an IRC gateway: it receives parsed [`IrcMessage`](IrcMessage.cs.md) instances from the [`IrcClientConnection`](IrcClientConnection.cs.md), interprets IRC semantics (registration flows, SASL vs PASS authentication, channel join/part, PMs, queries), and invokes the appropriate backend services and helpers ([`IMessageEncryptionService`](../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) for decrypting history, `ILogger` for diagnostics). The class groups responsibilities into logical regions — authentication, welcome/MOTD, channel operations, and query commands — and exposes a long-running `RunAsync` loop driven by a `CancellationToken` to process client messages until the connection ends. Many of the private handlers (`HandleCapAsync`, `HandleAuthenticateAsync`, `HandleNickAsync`, `HandleUserAsync`, `HandleJoinAsync`, `HandlePrivmsgAsync`, `HandleNamesAsync`, `HandleTopicAsync`, `HandleWhoisAsync`, etc.) encapsulate the IRC-to-service mapping and the reply generation.

## Notes
- Registration/authentication ordering is important: the handler supports both SASL (`AUTHENTICATE`) and legacy `PASS` flows and contains explicit fallbacks (e.g. try to auto-register on auth failure). Callers should not assume a client is fully registered until the registration completion path in `TryCompleteRegistrationAsync` completes.
- Encrypted and system channels are treated specially: the handler deliberately prevents joining channels that cannot be safely proxied (end-to-end encrypted rooms or server-only system channels), and history replay requires decrypting messages via [`IMessageEncryptionService`](../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md) before sending them to the IRC client.
- All public operations are async and driven by `RunAsync(CancellationToken)`: callers should respect the `CancellationToken` and be prepared for async exceptions to surface from the handlers; the class uses `ILogger` for recording failures and important state transitions.

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


Initializes a new `IrcCommandHandler` by injecting all required dependencies: [`IrcClientConnection`](IrcClientConnection.cs.md), [`IrcOptions`](IrcOptions.cs.md), [`IChatService`](../EchoHub.Core/Contracts/IChatService.cs.md), [`IUserService`](../EchoHub.Core/Contracts/IUserService.cs.md), [`IChannelService`](../EchoHub.Core/Contracts/IChannelService.cs.md), [`IMessageEncryptionService`](../EchoHub.Core/Contracts/IMessageEncryptionService.cs.md), and `ILogger`. The constructor stores these in private fields so the command handler can coordinate chat, user, and channel operations, apply encryption, and log activity when processing IRC commands. This initialization pattern is typically used by the dependency injection container or in tests to assemble a fully wired, ready-to-run handler.

## Remarks
The constructor serves as a wiring point that decouples `IrcCommandHandler` from concrete implementations, enabling substitution in tests and different runtime configurations. By wiring `_conn`, `_options`, `_chatService`, `_userService`, `_channelService`, `_encryption`, and `_logger`, it ensures the handler has immediate access to the resources needed to parse and route IRC commands, manage users and channels, apply encryption, and emit logs. It does not execute command logic itself; its purpose is to provide a fully initialized, ready-to-use instance for later operation.

---

### ServerName
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** property

```csharp
private string ServerName => _options.ServerName
```


The `ServerName` property is a private, read-only accessor that forwards to `_options.ServerName` to obtain the configured IRC server name. It serves as an internal convenience within the `IrcCommandHandler` class, enabling consistent access to the server name without coupling to the `_options` object.

## Remarks
This private indirection isolates the `IrcCommandHandler` from changes to where the server name is stored. If `_options`' structure changes or the server name is sourced from elsewhere, update only this member and keep the rest of the class intact. It also clarifies intent by naming and exposing the concept of 'server name' as a single retrieval point for internal command handling.

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


Handles the SASL authentication flow for an IRC connection. When invoked, it interprets the first parameter to drive a SASL PLAIN exchange: it can prompt the client to provide credentials, abort SASL, or process a base64-encoded payload to authenticate or register a user, updating the connection state on success and replying with appropriate IRC numerics on failure. The method encapsulates the end-to-end SASL Plain handling, including error signaling and logging for traceability.

## Remarks
This method centralizes the SASL PLAIN authentication handshake for a client, coordinating between the incoming [`IrcMessage`](IrcMessage.cs.md) payload, the server connection state (`_conn`), the user service (`_userService`), and server numerics. It performs decoding and validation of the SASL payload, derives a username from the payload, and attempts authentication first, then automatic registration as a fallback. On success, it binds the authenticated user to the connection (setting `Nickname`, `UserId`, and `IsAuthenticated`) and notifies the client with both `RPL_LOGGEDIN` and `RPL_SASLSUCCESS`. The structured exception handling ensures a consistent failure path with an `ERR_SASLFAIL` response and logging for operational visibility.

## Notes
- Malformed SASL payloads (e.g., payloads that do not yield at least three parts after decoding) trigger an authentication failure early, signaling to the client via `ERR_SASLFAIL`.
- If initial authentication fails, the flow transparently attempts to register a new user with the extracted credentials; if registration also fails, it reports the error back to the client and logs a warning.
- The password is sourced from the SASL PLAIN payload; ensure that credential handling complies with your security requirements and that secrets are managed appropriately within the `_userService`.


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


This private async method handles the AWAY command for the current connection. After confirming the user is registered via `RequireRegisteredAsync`, it checks for a non-empty first parameter on `msg.Parameters`; if present, it stores the away message on ``_conn`` (i.e., `_conn`), updates the user's status to `UserStatus.Away` via `_chatService`, and sends the `IrcNumericReply.RPL_NOWAWAY`. If no message is supplied, it clears the away message, updates the status to `UserStatus.Online`, and sends the `IrcNumericReply.RPL_UNAWAY`.

## Remarks
This method centralizes away-state management for the connected user by coordinating the connection state (`_conn`), persistence/update semantics (`_chatService`), and client feedback via numeric replies ([`IrcNumericReply`](IrcNumericReply.cs.md)). It ensures that providing an away message both reflects in server-side status and informs the client promptly.

## Notes
- Rapid, repeated calls may race with the chat service updates; consider sequencing on the caller side or adding concurrency guards.

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


HandleCapAsync processes IRC CAP negotiation messages related to SASL authentication. It inspects the first element of `msg.Parameters` to drive a small, centralized CAP flow: starting negotiation with `LS`, acknowledging or declining a SASL request with `REQ`, and ending negotiation with `END`. The method updates internal connection state via `_conn.CapNegotiating`, `_conn.IsSasl`, and coordinates with registration by triggering `TryCompleteRegistrationAsync()` when appropriate. Messages are sent back to the server using `_conn.SendAsync`, built from the current `ServerName` (e.g. `":{ServerName} CAP * LS :sasl"`) and reflecting the outcome of each branch. The logic short-circuits on insufficient parameters and handles case-insensitive comparisons for SASL requests.

The typical flow is:
- LS starts capability negotiation and marks the connection as negotiating.
- REQ sasl acknowledges SASL capability and enables SASL, while any other requested capability prompts a NAK with the requested name.
- END ends negotiation and, if credentials are present (non-null `Nickname` and `Username`) but the client is not yet registered, proceeds to complete registration via `TryCompleteRegistrationAsync()`.


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


Handles an incoming IRC command by normalizing the textual command to upper-case and dispatching to the corresponding asynchronous handler. As the central router, it maps pre-registration commands (such as `CAP`, `AUTHENTICATE`, `PASS`, `NICK`, [`USER`](../EchoHub.Core/Models/User.cs.md)) and post-registration commands (such as `PING`, `JOIN`, `PART`, `PRIVMSG`, `QUIT`, `NAMES`, `TOPIC`, `WHO`, `WHOIS`, `AWAY`, `LIST`, `MODE`, `MOTD`, and related aliases) to their dedicated `HandleXAsync` methods, returning the resulting `Task`. For the `PONG` case it completes synchronously with `Task.CompletedTask`; for any unknown command, it responds via `_conn.SendNumericAsync` using `IrcNumericReply.ERR_UNKNOWNCOMMAND` and the command text. This design provides a single, maintainable dispatch point that enforces consistent routing and error reporting across all IRC commands.

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


`HandleJoinAsync` processes the IRC `JOIN` command for a connected user. It validates that the user is registered, requires at least one channel parameter, and then parses comma-separated channel names with optional per-channel keys; for each channel it translates the raw IRC channel name to the internal channel identifier, blocks end-to-end encrypted channels (which must be joined via the EchoHub client) and system channels (server-managed) from IRC, delegates the actual join to `_chatService.JoinChannelAsync` with the user's connection and identity, and on success updates the connection state, announces the join, and replays the channel topic, NAMES list, and decrypted history to the IRC client. On failure, it returns the appropriate IRC error (e.g. `ERR_BADCHANNELKEY` or `ERR_NOSUCHCHANNEL`).

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


HandleListAsync processes the IRC LIST command by emitting the list of public channels to the connected client. It first ensures the caller is registered via `RequireRegisteredAsync()`; if not, it returns immediately. It then retrieves the channel collection from `_channelService.GetChannelListAsync()` and sends an `RPL_LIST` line for each channel that has `IsPublic` set to true, formatting the line as `#{ch.Name} {ch.OnlineCount} :{lockHint}{ch.Topic ?? ""}` where `lockHint` is `[+k] ` when `IsProtected` is true. After enumerating all public channels, it issues `RPL_LISTEND` with End of LIST to finish.

## Remarks
By filtering to `IsPublic` channels, private channels are hidden from discovery, aligning the server's LIST output with the SignalR client's channel exposure. The `[+k]` indicator communicates a protected channel requiring a key and is propagated in the line alongside the channel's `Topic` (or an empty string if no topic is set). This method coordinates a read-only view of channel state and relies on [`IrcNumericReply`](IrcNumericReply.cs.md)-provided numeric codes (`RPL_LIST` and `RPL_LISTEND`).

## Notes
- The handler short-circuits if the user is not registered, so no LIST data is sent to unregistered users.

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


Handles the IRC `MODE` command for a target in the gateway. It first ensures the caller is registered via `RequireRegisteredAsync` and returns user-mode information with `RPL_UMODEIS` when the target isn’t a channel, emitting a leading `+` in that case. For channel targets, it resolves the internal channel name with `IrcToEchoHubChannel`, validates the channel, and then either reports the current mode with `RPL_CHANNELMODEIS` or processes mode changes such as ban-list probes (`b`/`+b`) and password changes (`+k`/`-k`) by delegating to `_channelService.SetChannelPasswordAsync` and broadcasting results through `_conn`; unknown modes yield `ERR_UNKNOWNMODE`.

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


HandleNamesAsync is a private asynchronous method that processes a NAMES query. It begins by verifying the caller is registered using `RequireRegisteredAsync()`, returning early if not. It then validates that a parameter is provided (`msg.Parameters.Count < 1`); if not, it returns. The first parameter is converted to the internal channel name by `IrcToEchoHubChannel`, and if this conversion yields `null`, the method exits. Otherwise, it calls `SendNamesReplyAsync(channelName)` to emit the names list for the channel.

## Remarks
This method encapsulates the precondition checks for name-related queries and centralizes the channel-name translation, keeping the response logic contained in `SendNamesReplyAsync`.

## Notes
- If `IrcToEchoHubChannel` cannot map the input to a channel, no response is sent.
- The method relies on `Parameters` being provided by [`IrcMessage`](IrcMessage.cs.md) and uses early returns to avoid unnecessary work.

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


`HandleNickAsync` handles the NICK command by validating input and updating the connection state. If no nickname is supplied, it sends `ERR_NONICKNAMEGIVEN` with a "No nickname given" message. If the nickname fails the policy check against `ValidationConstants.UsernameRegex()`, it responds with `ERR_ERRONEUSNICKNAME` and a descriptive error like "Erroneous nickname (must be 3-50 chars: a-z, 0-9, _, -)". On success, it normalizes the nickname to lowercase via `ToLowerInvariant()` and assigns it to `_conn.Nickname`. Finally, if the connection is not yet registered but already has a `Username`, it advances the registration by calling `TryCompleteRegistrationAsync()`. 

## Remarks
By encapsulating parameter validation, nickname syntax enforcement, normalization, and the progression toward registration, this method centralizes the Nick command workflow. It coordinates with `_conn` to store the chosen nickname, uses [`IrcNumericReply`](IrcNumericReply.cs.md) values to emit exact IRC error codes for invalid or missing nicknames, and triggers `TryCompleteRegistrationAsync()` when appropriate, ensuring a cohesive startup sequence.

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


Handles a PART command from a registered IRC client by parsing a comma-separated list of channels from the first parameter and an optional part message from the second parameter, then leaving each channel both in the internal chat state and by sending an IRC PART message back to the client.

Channels are mapped from their raw IRC name to the internal Echo Hub channel via `IrcToEchoHubChannel`; invalid mappings are skipped. For each valid channel, the method first awaits `_chatService.LeaveChannelAsync(_conn.ConnectionId, _conn.Nickname!, channelName)`, then updates the local connection state with `_conn.LeaveChannel(channelName)`, and finally emits the IRC PART notice `":{_conn.Hostmask} PART #{channelName}"` with an optional payload appended if a part message was supplied.

## Remarks
Coordinates internal state with the external IRC protocol to keep the user’s channel memberships in sync across both domains. The `IrcToEchoHubChannel` mapping acts as a guardrail, ensuring only recognized channels are processed and leaving others untouched.

## Notes
- If `IrcToEchoHubChannel` yields `null` for a channel, that channel is ignored rather than causing an exception.

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


HandlePassAsync processes the IRC PASS command for the current connection. If the connection is already registered (`_conn.IsRegistered`), it replies with `ERR_ALREADYREGISTERED` by calling `_conn.SendNumericAsync(ServerName, IrcNumericReply.ERR_ALREADYREGISTERED, ":You may not reregister")`; if a password parameter is provided, it stores the password on the connection (the exact storage is redacted in the source). The method always completes by returning a `Task`—the send task when replying, or `Task.CompletedTask` when no action is needed.

## Remarks
Centralizes PASS command handling within the `IrcCommandHandler` and enforces the re-registration guard in one place. It updates the connection state when a parameter is present, separating command validation from the subsequent authentication flow.

## Notes
- The method is not declared `async`; it returns a `Task` and may complete synchronously via `Task.CompletedTask` when no password parameter is supplied.
- The password value is written to a connection field whose exact name is redacted; handling of this sensitive data should be reviewed in the surrounding authentication flow.

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


`HandlePingAsync` handles an IRC `PING` by replying with a `PONG` to keep the connection alive. It reads the first parameter from the incoming `IrcMessage.Parameters` as the token, or falls back to `ServerName` if none is provided, and sends the response via `_conn.SendAsync` using the IRC format `":{ServerName} PONG {ServerName} :{token}"`.

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


Handles an IRC PRIVMSG from a connected user by first ensuring the user is registered, then validating that the message has enough parameters and targets a channel. If parameters are missing, it replies with `ERR_NEEDMOREPARAMS`; if the target is not a channel (does not start with `#`), it replies with `ERR_NOSUCHNICK` and instructs to use channels. It then maps the IRC channel to an internal EchoHub channel via `IrcToEchoHubChannel` and forwards the message content to the chat service through [`SendMessageAsync`](../EchoHub.Server/Services/ChatService.cs.md), supplying the current connection's user id, nickname, channel name, message content, and connection id. If the chat service reports an error, it communicates it back to the client with `ERR_CANNOTSENDTOCHAN` for the affected channel. 

In short, it acts as the IRC surface to the EchoHub chat layer for channel-based private messages, performing parameter validation, channel resolution, and error propagation in a single, cohesive flow.

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


HandleQuitAsync is a private async method that processes a quit request by sending an IRC `ERROR` message to close the client connection. It derives the quit reason from the first element of the [`IrcMessage`](IrcMessage.cs.md)'s `Parameters` (falling back to the literal `Client quit` if none is provided) and includes the nickname via `_conn.Nickname` in the response by calling `_conn.SendAsync` with the string `ERROR :Closing Link: <nickname> (<quitMessage>)`.

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


HandleTopicAsync processes an IRC TOPIC command for a channel. It ensures the caller is registered, validates parameters, converts the IRC channel name to the internal channel via `IrcToEchoHubChannel`, and then either returns the current topic with `SendChannelTopicAsync` when only the channel is provided or updates the topic via `_channelService.UpdateTopicAsync` using the current user's ID, passing `null` for an empty topic. If the update fails, it replies with an IRC numeric error—`ERR_NOSUCHCHANNEL` when the channel is not found, otherwise `ERR_CHANOPRIVSNEEDED`—including the error message; on success, it broadcasts the updated channel to connected web clients via `_chatService.BroadcastChannelUpdatedAsync` and echoes the new topic back to the IRC client with `SendAsync` using the `TOPIC` command.

## Remarks
This method centralizes the TOPIC command flow: it validates the caller, resolves the channel, performs the update, and coordinates notification to both SignalR clients and the IRC client. It also maps domain errors to IRC numeric replies to preserve protocol semantics across layers.


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


HandleUserAsync is the private asynchronous handler for processing the IRC USER command as part of the client registration flow. It first guards against re-registration by sending the IRC numeric `ERR_ALREADYREGISTERED` via `_conn.SendNumericAsync` when `_conn.IsRegistered` is true, and then returns. If there are fewer than four parameters, it responds with `ERR_NEEDMOREPARAMS` and terminates early. When invoked with a valid parameter set, it assigns the username from `msg.Parameters[0]` to `_conn.Username` and the real name from `msg.Parameters[3]` to `_conn.RealName`. Finally, if a nickname has already been established (`_conn.Nickname` is not null), it awaits `TryCompleteRegistrationAsync()` to advance the registration process.

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


HandleWhoAsync processes a WHO request for a channel by validating the caller and translating the IRC channel into the EchoHub channel, then streaming the current online users. It first ensures the client is registered, validates that a channel parameter is provided, and derives the internal channel name with `IrcToEchoHubChannel`. If any of these steps fail, it exits without emitting data. When a valid channel is obtained, it fetches online users via `_chatService.GetOnlineUsersAsync(channelName)` and, for each user, sends a `RPL_WHOREPLY` using [`IrcNumericReply`](IrcNumericReply.cs.md) data, encoding the channel, user, host (`echohub`), server, user nickname, away state, hop count, and display name (falling back to the username when necessary). After listing all users, it signals completion with `RPL_ENDOFWHO`.

This method is the IRC-facing surface that translates EchoHub's online-user model into IRC protocol replies, making it the point of integration for WHO-style channel listings. The flow is fully asynchronous and relies on the [`UserStatus`](../EchoHub.Core/Models/UserStatus.cs.md) enum to determine the away flag, as well as the defined `RPL_WHOREPLY`/`RPL_ENDOFWHO` numeric replies for protocol correctness.


---

### HandleWhoisAsync
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


HandleWhoisAsync processes the IRC WHOIS command by ensuring the requester is registered, validating the target nick parameter, and then assembling and sending the standard WHOIS information for that user. It fetches the user profile, emits the appropriate WHOIS numeric replies (and the away/idle data when available), and gracefully reports when the target nick does not exist.

## Remarks
This method acts as a protocol adapter that wires together user data and channel memberships to produce a coherent WHOIS response. It coordinates between `_userService` for profile data, `_chatService` for channel membership, and `_conn` for sending IRC numerics, encapsulating the protocol-specific choreography in a single, testable unit. The logic defensively handles missing profile data and optional information (channels, away message) to align with RFC-like WHOIS expectations while keeping the flow readable and isolated from business rules.


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


IrcToEchoHubChannel converts a raw IRC channel name into EchoHub's internal channel identifier, returning null when the input cannot be mapped. It requires the input to start with the '#' prefix and to be at least two characters long; it then drops the leading '#', lowercases the remainder invariantly, trims whitespace, and validates the result against the central channel-name pattern provided by `ValidationConstants.ChannelNameRegex()`. If the name matches, the canonical, lowercased name is returned; otherwise null. This function is typically invoked when translating IRC channel references into EchoHub's normalized channel namespace, ensuring downstream logic always works with validated, consistent channel names rather than arbitrary IRC inputs.

---

### RequireRegisteredAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<bool> RequireRegisteredAsync()
```

**Returns:** `Task<bool>`


`RequireRegisteredAsync` checks whether the IRC connection is registered and returns true when it is. If not registered, it sends the standard `ERR_NOTREGISTERED` reply using `SendNumericAsync` with `ServerName`, `IrcNumericReply.ERR_NOTREGISTERED`, and the message `":You have not registered"`, then returns false.

## Remarks
Conceptually, this method centralizes the precondition for commands that require a registered session, avoiding duplicated checks across handlers. It relies on `_conn` to inspect `IsRegistered`, and on `ServerName` and `IrcNumericReply.ERR_NOTREGISTERED` to deliver a consistent IRC-compliant error. By returning a boolean, it makes the caller's flow straightforward: proceed when true, bail when false.

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


Runs an asynchronous, cancellation-aware loop that reads lines from the IRC connection via `_conn.ReadLineAsync(ct)`, stops when `line` is `null`, trims trailing CR/LF, skips blank lines, logs each received line with `_logger.LogDebug("IRC < {Id}: {Line}", _conn.ConnectionId, line)`, parses the line into an [`IrcMessage`](IrcMessage.cs.md) using `IrcMessage.Parse(line)`, and dispatches the resulting message to `HandleCommandAsync(msg)`. This method is the central inbound processor for an IRC connection: it bridges the raw socket input to the higher-level command handling logic and continues running until the provided `CancellationToken ct` signals cancellation or the connection ends.

## Remarks

This method is the primary inbound processor for a single IRC connection, isolating IO, parsing, and command dispatch from higher-level application logic. It logs critical diagnostic information: per-line debugging via `_logger.LogDebug` and per-command failures via `_logger.LogError`, including the command name and the nick when available. By catching exceptions only around `HandleCommandAsync(msg)` it ensures that a failure in handling one command does not crash the entire loop, preserving resilience.

## Notes

- The call to `IrcMessage.Parse(line)` occurs outside the `try` block that guards `HandleCommandAsync(msg)`; a parsing error could bubble up and terminate the loop. Consider moving parsing inside the try/catch or adding its own guard.

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


This private helper fetches the current topic for a channel and, if the channel exists, delivers the appropriate IRC numeric reply to the connected client. It calls `_channelService.GetChannelTopicAsync(channelName)` to obtain `(topic, exists)` and, depending on the result, returns early when the channel doesn't exist, sends `RPL_TOPIC` with `#<channelName> :<topic>` when a topic is set, or sends `RPL_NOTOPIC` with `#<channelName> :No topic is set` when there is no topic.

## Remarks
This method acts as a small integration point between channel-data access and IRC protocol messaging. By encapsulating the topic-notification logic, it coordinates `_channelService` (data) and `_conn` (connection) to produce consistent numeric replies via [`IrcNumericReply`](IrcNumericReply.cs.md) constants, reducing duplication across the command-handling code. Its private scope signals it's an internal helper used by higher-level IRC commands, keeping the channel-topic flow centralized.

## Notes
- If the channel does not exist (`exists` is false), the method returns without sending any reply, which can appear as a missing response to the client; callers should ensure channel existence or handle this case.

## Dependencies
- IrcNumericReply

## Dependency APIs (verified signatures)

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
- Name: `SendChannelTopicAsync`
- Kind: `method`
- File: `src/EchoHub.Server.Irc/IrcCommandHandler.cs`
- Language: `csharp`
- ID: `d23ca663-2c48-4576-9b5f-759527f87f1c`

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


Translates a channel-mode operation failure into the appropriate IRC numeric for the target channel and forwards it to the server. When a mode operation fails for the given `channelName`, the method maps the domain error to an IRC numeric using a switch over [`ChannelError`](../EchoHub.Core/DTOs/CommonDtos.cs.md) (NotFound -> `IrcNumericReply.ERR_NOSUCHCHANNEL`, Forbidden -> `IrcNumericReply.ERR_CHANOPRIVSNEEDED`, otherwise `IrcNumericReply.ERR_KEYSET`) and sends a message using `_conn.SendNumericAsync(ServerName, numeric, `$"#{channelName} :{result.ErrorMessage}"`)`.

---

### SendMotdAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendMotdAsync()
```

**Returns:** `Task`


SendMotdAsync is a private asynchronous helper that delivers the server's Message of the Day to the current IRC connection. It checks the configured `Motd` on `_options` and, if missing, responds with the IRC error code `ERR_NOMOTD`; otherwise it streams the MOTD lines between `RPL_MOTDSTART` and `RPL_ENDOFMOTD` using `RPL_MOTD` for each line. This method formats each line by trimming a trailing carriage return and sends one line per message, adhering to the IRC protocol expectations.

## Remarks
`SendMotdAsync` centralizes MOTD delivery to ensure consistent IRC protocol formatting and behavior. It relies on `_conn` to emit numeric replies and on the [`IrcNumericReply`](IrcNumericReply.cs.md) constants to signal the start, each line, and the end of the MOTD, while consulting the configured `Motd` via the `_options` object. This encapsulation prevents duplication and makes it straightforward to adjust MOTD formatting in one place.

## Notes
- The method trims trailing carriage returns (`'\r'`) from each MOTD line to gracefully handle Windows-style line endings when sending lines via `RPL_MOTD`.
- If `_options.Motd` is null or whitespace, the method short-circuits and emits `ERR_NOMOTD` before attempting any `RPL_MOTD` messages.
- MOTD lines are sent individually in order, one `RPL_MOTD` message per line, followed by `RPL_ENDOFMOTD` to mark completion.

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


Responds to an IRC NAMES request for a channel by collecting the currently online users and emitting the standard numeric replies that enumerate channel members. It calls `_chatService.GetOnlineUsersAsync(channelName)` to obtain user objects, builds a space-separated list of their `Username`s, and sends two numeric replies: first `IrcNumericReply.RPL_NAMREPLY` with the channel's nick list via `_conn.SendNumericAsync`, and then `IrcNumericReply.RPL_ENDOFNAMES` to mark the end of the list.

## Remarks
This method encapsulates the protocol details of responding to the IRC `NAMES` command for a channel. It isolates the discovery of online users from the formatting and emission of the numeric replies, ensuring consistent NAMES responses and simplifying the caller's responsibilities.

## Notes
- If there are no online users, the constructed `nicks` string will be empty, but an `RPL_NAMREPLY` line will still be emitted followed by `RPL_ENDOFNAMES`.
- The method is private and relies on `_chatService` and `_conn` being available; callers must ensure the surrounding context handles validation and errors appropriately.

---

### SendWelcomeBurstAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendWelcomeBurstAsync()
```

**Returns:** `Task`


This private async method emits the initial IRC handshake to the connected client by sending a series of standard numeric replies. It reads the nickname from `_conn.Nickname`, uses `ServerName` as the server identity, and dispatches the numerics `RPL_WELCOME`, `RPL_YOURHOST`, `RPL_CREATED`, `RPL_MYINFO`, and `RPL_ISUPPORT` via `_conn.SendNumericAsync`. After sending these banners, it calls `SendMotdAsync` to deliver the MOTD and complete the handshake.

## Remarks
This method centralizes the handshake so every connection receives a consistent welcome, isolating IRC protocol formatting from higher-level command handling. It relies on the [`IrcNumericReply`](IrcNumericReply.cs.md) constants to produce the standard numerics and on `_conn` to transmit messages, keeping the transport details out of the handshake logic. The method assumes `_conn.Nickname` is non-null at the time it runs, as evidenced by the null-forgiving read.

## Notes
- Be aware that `_conn.Nickname` is read with a null-forgiving operator; if nickname isn't set yet, a runtime `NullReferenceException` could occur. Ensure the nickname is established earlier in the connection sequence before calling this method.

---

### TryCompleteRegistrationAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task TryCompleteRegistrationAsync()
```

**Returns:** `Task`


Asynchronously completes a client's registration by deciding whether SASL authentication has already succeeded, or whether to perform PASS-based login to complete or create the user. If SASL is already authenticated ( `_conn.IsAuthenticated` and `_conn.UserId` is not null ), it marks the connection as registered, notifies the chat subsystem via [`UserConnectedAsync`](../EchoHub.Server/Services/ChatService.cs.md), and then triggers the welcome sequence with `SendWelcomeBurstAsync`. If not SASL-authenticated, it requires a password; if missing, it returns `ERR_PASSWDMISMATCH` and a generic error. Otherwise it calls `_userService.AuthenticateUserAsync(_conn.Nickname, _conn.Password)` and, on failure, falls back to `_userService.RegisterUserAsync(_conn.Nickname, _conn.Password)`; on success it binds the resulting user to the connection, updates `_conn.UserId`, `_conn.Nickname`, and flags `_conn.IsAuthenticated` and `_conn.IsRegistered`, then notifies the chat service and sends the welcome burst.

## Remarks
This method encapsulates the end-to-end registration/authentication handoff, coordinating between the connection state, the user service, and the chat subsystem. It guards against re-entrancy by exiting early when capability negotiation is in progress or the connection is already registered, and it ensures a consistent welcome sequence is delivered once authentication or registration succeeds.

## Notes
- Be mindful that the initial logging emits user-identifying state (e.g. `_conn.Nickname`, `_conn.Username`); ensure logging remains appropriate for your privacy and security policy.

---