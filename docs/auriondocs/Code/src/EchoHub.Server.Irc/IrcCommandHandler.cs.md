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


Handles IRC protocol commands for a single client connection and drives the registration/authentication and channel/message flows. Instantiate one per IrcClientConnection and call RunAsync(cancellationToken) to run the command-processing loop that translates IRC messages into operations on the server-side services (IChatService, IUserService, IChannelService, IMessageEncryptionService) and emits IRC replies via the connection.

## Remarks
This class is an adapter between the IRC wire protocol and the EchoHub server services. It centralizes command handling (CAP/SASL and PASS authentication, NICK/USER registration, JOIN/PART, PRIVMSG, TOPIC, NAMES, WHO/WHOIS, etc.), enforces registration requirements, and performs translations such as converting IRC channel names to the hub's channel identifiers and decrypting history before replay. The handler owns the processing loop for one client connection and uses the provided services and logger to perform domain work and emit protocol responses.

## Example
```csharp
// Typical usage in the connection lifecycle
var handler = new IrcCommandHandler(
    conn: connection,
    options: ircOptions,
    chatService: chatService,
    userService: userService,
    channelService: channelService,
    encryption: messageEncryption,
    logger: logger);

await handler.RunAsync(cancellationToken);
```

## Notes
- Instances are intended to be used per client connection; RunAsync runs the command loop and respects the provided CancellationToken.
- Registration/authentication is handled in multiple ways (CAP/SASL, PASS, NICK/USER); callers should not assume registration is complete until the handler's registration flow finishes.
- When replaying channel history the handler decrypts messages (history is encrypted for SignalR transport); the IMessageEncryptionService is required for that step.
- Channel name conversion is performed by IrcToEchoHubChannel; invalid or non-conforming IRC channel names may result in a null/unsupported mapping.

---

## IrcCommandHandler (constructor)
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


The IrcCommandHandler constructor initializes a new handler instance by accepting all dependencies required to process IRC commands and storing them for later use. It takes an IrcClientConnection to talk to the client, IrcOptions for configuration, and services for chat, user, and channel management, plus an IMessageEncryptionService and an ILogger. These injected dependencies are saved to internal fields so that the command handling logic can route messages, look up users and channels, apply encryption as needed, and log its activity. This constructor is typically resolved by a dependency injection container or created by the composition root when wiring the IRC subsystem.

## Remarks
This constructor is a textbook example of dependency injection. It doesn't perform any command processing itself; instead, it wires together collaborators that implement the actual behavior. By depending on abstractions (interfaces) for chat, user, channel, encryption, and logging, the handler remains testable and interchangeable, while the concrete services can evolve independently.

## Notes
- No explicit parameter validation is performed; the constructor assumes non-null dependencies are supplied by the caller or DI container.

---

## ServerName
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** property

```csharp
private string ServerName => _options.ServerName
```


This private expression-bodied property returns the server name configured on the options object. It simply forwards to _options.ServerName, providing a centralized access point inside IrcCommandHandler. By isolating the retrieval here, future changes to where the server name comes from or any necessary normalization can be handled in one place without scattering _options.ServerName usage throughout the class.

## Remarks
By exposing the value through this private member, the implementation can evolve without changing its callers. It acts as a small abstraction over the options object, clarifying intent and keeping command construction logic decoupled from the exact shape of the configuration source.

## Notes
- Assumes _options is non-null; a null _options would trigger a NullReferenceException when accessing ServerName.
- If the server name ever requires validation or transformation, implement it here rather than duplicating the logic at every call site within the class.

---

## HandleAuthenticateAsync
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


Processes the SASL authentication negotiation for a single IRC connection by handling AUTHENTICATE commands, decoding base64 payloads, and authenticating or registering the user before updating the connection state and replying with the appropriate numeric replies.

## Remarks

Centralizes the SASL PLAIN flow within the server by coordinating the connection state, user service, and logging. It handles the client abort path (AUTHENTICATE *) and ensures consistent success and failure responses, including updating Nickname/UserId and sending RPL_SASLSUCCESS and RPL_LOGGEDIN when authentication succeeds.

## Notes

- A broad catch (Exception) converts any unexpected error during SASL processing into a generic SASL failure response; ensure proper logging and test for edge cases to avoid masking real issues.
- The payload decoding path assumes UTF-8 and base64 encoding; non-UTF-8 payloads will trigger a malformed payload failure.
- The authentication flow relies on the decoded payload supplying a password that is used with the username; ensure that client-side payload construction matches the server-side expectations.

---

## HandleAwayAsync
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


Handles the AWAY command by updating the current user's away status. It first ensures the user is registered; if not, the method exits early. When a non-empty parameter is supplied, it stores that text as the away message, updates the user's status to Away via the chat service, and responds to the client with RPL_NOWAWAY to indicate the user is away. If no parameter is provided, it clears the away message, resets the status to Online, and replies with RPL_UNAWAY to indicate the user is no longer away.

---

## HandleCapAsync
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


HandleCapAsync processes CAP negotiation messages from the IRC server for this client connection. It inspects the first parameter and, for LS, begins SASL negotiation; for REQ it acknowledges SASL or returns a NAK; for END it ends negotiation and may trigger the registration flow if credentials are present but not yet registered.

## Remarks
Conceptually, this method centralizes the CAP negotiation handshake within the IrcCommandHandler, translating server prompts into local state changes and flow control. It relies on the surrounding IrcConnection (represented by _conn) to track whether SASL is advertised or in progress, and to kick off the final registration once negotiation ends.

## Notes
- If a REQ message lacks a second parameter, the code sends NAK with an empty request string, which may prompt the server to retry or close the capability negotiation.
- END only attempts to complete registration when Nickname and Username are non-null and the client is not yet registered; otherwise, the END path has no effect.

---

## HandleCommandAsync
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


It normalizes the incoming IrcMessage.Command to upper case and dispatches it via a switch expression to the appropriate per-command async handler, covering both pre-registration (CAP, AUTHENTICATE, PASS, NICK, USER) and post-registration (PING, JOIN, PART, PRIVMSG, QUI T, NAMES, TOPIC, WHO, WHOIS, AWAY, LIST, MODE) commands. It special-cases PONG as a no-op (returns Task.CompletedTask) and maps USERHOST/LUSERS to a no-op as well; MOTD triggers SendMotdAsync. If the command is unknown, it replies with an ERR_UNKNOWNCOMMAND to the client through _conn.SendNumericAsync. A developer would reach for it when you need to route incoming IRC commands to their dedicated handlers or extend the supported command set in this central dispatcher.

## Remarks
This method acts as the central command dispatcher for the IRC connection, isolating command routing from per-command implementations and enforcing uniform handling paths (including no-ops and standard error reporting for unknown commands).

---

## HandleJoinAsync
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


Handles the IRC JOIN command for a connected user. It first verifies the user is registered, enforces that at least one parameter (the channel list) is provided, and then processes one or more comma-separated channels. For each raw channel, it maps the IRC channel name to the internal EchoHub channel; if the mapping fails it reports an invalid channel to the client via ERR_NOSUCHCHANNEL. When a valid channel is identified, it asks the chat service to join the user to that channel and retrieves the join history. On success, it updates the connection state, echoes the JOIN notice to the client, and delivers the channel topic and NAMES list. Finally, it replays the channel history by decrypting each message (history is encrypted for SignalR transport) and sending the resulting IRC lines to the client.

## Remarks
By centralizing the JOIN handling, this method coordinates authentication, channel validation, state updates, and client-visible synchronization (JOIN message, topic, NAMES, and history). It relies on IrcToEchoHubChannel to map channel identifiers, the chat service to perform the join and return historical messages, and the encryption subsystem to decrypt stored history for playback. It also demonstrates support for joining multiple channels in a single JOIN command with per-channel success/failure handling.

## Notes
- Per-channel error handling: a failure for one channel does not abort processing of other channels in the same JOIN request.
- History replay requires a functioning encryption backend: each stored message is decrypted before being formatted and sent to the client.
- The client receives a JOIN notification, followed by the channel's topic and NAMES list, only after a successful join for that channel.

---

## HandleListAsync
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


Handles the IRC LIST command by fetching the current channel catalog and emitting a sequence of numeric replies, one line per channel, containing the channel name prefixed with a '#', the number of online users, and the topic, followed by an end-of-list marker. It first requires that the client is registered; if not, it returns early. It then retrieves the list of channels from the channel service and writes a RPL_LIST response for each channel using the server connection, finally signaling end of list with RPL_LISTEND. This method encapsulates the LIST-specific protocol formatting and transport, delegating data retrieval to the channel service and response delivery to the IRC connection layer.

---

## HandleModeAsync
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


Handles incoming MODE commands by replying with a minimal status depending on the target. It first ensures the caller is registered, then requires at least one parameter. If the first parameter represents a channel (the target starts with '#'), it replies with RPL_CHANNELMODEIS for that channel and a '+' to indicate there are no additional channel modes currently. If the target is not a channel, it replies with RPL_UMODEIS and a '+' indicating the user's mode. The method uses the underlying connection to send numeric replies and exits early on invalid input or an unregistered state.

## Remarks
This method centralizes a small, specific IRC protocol branch: channel vs. user mode inquiry. It relies on the connection to emit numeric replies and on the RPL codes defined in IrcNumericReply, keeping mode-state specifics elsewhere. The implementation is intentionally minimal, delegating actual mode state maintenance to other components and returning a conservative, placeholder response when no modes are known.

## Notes
- The function recognizes only channel targets that begin with '#'. Other channel prefixes or unconventional targets will be treated as user targets.
- If actual channel or user modes exist elsewhere, this implementation emits a generic "+" response rather than enumerating real modes.

---

## HandleNamesAsync
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


Handles an IRC command that requests the list of names for a channel. It first ensures the caller is registered and that a channel parameter is present, translates that channel name to the internal EchoHub channel, and, if successful, delegates to SendNamesReplyAsync to emit the response.

## Remarks
It acts as a small orchestration layer that enforces preconditions before producing a reply, keeping command handlers concise. It separates concerns: registration gating, parameter validation, channel-name translation, and the actual reply emission. This separation improves reuse of the IrcToEchoHubChannel mapping logic across multiple commands and makes testing easier by isolating the decision points before I/O.

---

## HandleNickAsync
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


Handles the NICK command by validating the requested nickname, returning IRC-style numeric errors for a missing or invalid nickname, normalizing the nickname to lowercase, and continuing the registration process when possible.

## Remarks
This method centralizes nickname handling in the connection lifecycle, ensuring consistent error reporting and state updates during client registration. It relies on centralized constants and regex validation (ValidationConstants.UsernameRegex) to enforce allowed nicknames, keeping the server's behavior aligned with the IRC protocol. Lowercasing the nickname ensures case-insensitive matching across the session, preventing subtle duplicates. The final TryCompleteRegistrationAsync call is only reached when the connection is not yet registered but already has a provided username, reflecting the intended handshake order.

## Notes
- If no nickname is provided, it sends ERR_NONICKNAMEGIVEN and returns without mutating state.
- If the nickname fails ValidationConstants.UsernameRegex(), it sends ERR_ERRONEUSNICKNAME with a message about 3–50 chars and allowed characters.
- The nickname is stored in lowercase (ToLowerInvariant) and registration completion is triggered only when the connection is not registered and a username has been supplied.

---

## HandlePartAsync
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


Handles the IRC PART command by removing the current connection from one or more channels and notifying the client. It first ensures the caller is registered, validates that at least one parameter is present, and then processes a comma-separated list of target channels. For each valid channel, it translates the IRC channel name to the internal EchoHub channel, leaves the channel via the chat service, updates the in-memory connection state, and finally sends the PART message back to the client, including an optional part message when provided.

## Remarks
By encapsulating this flow, HandlePartAsync coordinates protocol handling, membership state, and outbound signaling in a single place. It relies on the IRC-to-internal-channel mapping to decide whether a channel can be processed, and it processes all channels in the request in sequence to keep state consistent.

## Notes
- If an IRC channel cannot be mapped to an internal channel name, the channel is skipped without error.
- The PART message is appended only when a part message is supplied; otherwise the PART notice is sent with no trailing message.
- The method assumes the user is registered; otherwise RequireRegisteredAsync short-circuits the operation.

---

## HandlePassAsync
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


Processes the PASS command during the IRC handshake. It first guards against re-registration by returning an ERR_ALREADYREGISTERED if the connection is already registered; if a password parameter is present, it applies that value to the connection (the exact operation is redacted in this excerpt). In all paths it completes as a Task, with no additional asynchronous work unless the error path triggers sending the numeric reply.

---

## HandlePingAsync
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


HandlePingAsync is a private asynchronous handler that replies to an IRC server’s PING with a corresponding PONG to keep the connection alive. It derives the pong token from the incoming message’s first parameter when available, otherwise falling back to the configured ServerName, and then sends a raw IRC PONG response through the active connection. This ensures the client adheres to the IRC keep-alive protocol and maintains connectivity even when the server provides no explicit token.

## Remarks
This method encapsulates the keep-alive handshake used by IRC servers, centralizing the formatting and dispatch of PONG responses. It relies on the ServerName and the incoming message’s Parameters to determine the correct token and on the underlying connection (_conn) to transmit the response, promoting consistency across PING handling in the command handler. The private scope signals that PING handling is an internal concern, delegated to the dedicated message-processing path rather than being exposed as a public utility.

---

## HandlePrivmsgAsync
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


Handles incoming PRIVMSG commands for an authenticated IRC user. It first ensures the client is registered, validates parameter presence, restricts messages to channel targets, maps IRC channel names to internal EchoHub channels, and forwards the content to the chat service, surfacing any errors back to the IRC client.

## Remarks
 Acts as a bridge between the IRC protocol layer and the chat delivery service. It enforces protocol constraints (registered users, enough parameters, channel targets) and translates IRC-level errors into appropriate numeric replies, ensuring the client receives clear feedback when a message cannot be delivered. By delegating the actual message delivery to the chat service after channel-name translation, it keeps IRC handling decoupled from chat routing concerns and centralizes channel-based messaging through a single code path.

## Notes
- The method assumes the user is authenticated (RequireRegisteredAsync) and uses _conn.UserId directly when sending the message; a missing UserId would surface as a null-reference risk if authentication state is violated.
- If the target cannot be mapped to an internal channel (IrcToEchoHubChannel returns null), the method exits without notifying the client.
- If the chat service reports an error, the client receives a ERR_CANNOTSENDTOCHAN for the corresponding channel, conveying the failure reason returned by the service.


---

## HandleQuitAsync
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


This private async method handles an IRC client quit by sending an ERROR Closing Link response to the connected user, including the server-side nickname and an optional quit message. It chooses the quit reason from the first element of msg.Parameters when present, defaulting to "Client quit" otherwise, and then transmits the formatted error line via the underlying connection.

## Remarks

This abstraction centralizes the finalization of a client session on quit, ensuring a consistent protocol-compliant closure message across the server. It relies on the current connection's nickname and the provided parameters to craft a meaningful reason, minimizing duplication of the close-link signaling logic elsewhere in the command handler. The method is intentionally compact and side-effectful (it sends data over the network) to guarantee the client receives an explicit termination signal.

## Example

```csharp
// Example: quit with a custom message
var msg = new IrcMessage { Parameters = new List<string> { "Bye!" } };
await HandleQuitAsync(msg);
```

## Notes

- If msg.Parameters is null, this method will throw a NullReferenceException; callers should ensure a non-null Parameters collection is provided (or handle upstream).
- If _conn or _conn.Nickname is null at the time of invocation, the resulting error message may be malformed; ensure the connection is properly initialized before calling.

---

## HandleTopicAsync
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


HandleTopicAsync processes requests to view or change a channel's topic. It requires the caller to be registered, validates input parameters, and resolves the channel name before either returning the current topic or denying topic changes via the API.

## Remarks
By centralizing this logic in a single asynchronous handler, the system enforces access control and input validation at the point where topic-related commands are interpreted. It relies on the IrcToEchoHubChannel mapping to translate channel identifiers into internal echo hub channels; the decision to fetch vs. deny is driven purely by parameter count, making the command predictable and auditable.

## Notes
- The method returns early without side effects if the caller is not registered or if the channel name cannot be derived from the first parameter, which may surprise callers relying on a response.
- When a topic change is attempted with extra parameters, the code issues a numeric ERR_CHANOPRIVSNEEDED to the server with a message indicating that only the channel creator can change the topic via the API; ensure your user has channel-creator privileges if a change is expected to succeed.

---

## HandleUserAsync
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


Handles the USER command by validating the current registration state, enforcing parameter requirements, and updating the connection with the requested username and real name. If the client is already registered it replies ERR_ALREADYREGISTERED; if there are not enough parameters it replies ERR_NEEDMOREPARAMS; when parameters are present and a nickname exists, it advances the registration flow by calling TryCompleteRegistrationAsync.

## Remarks
This symbol encapsulates the pre-registration checks required for the USER command within the IRC server's command handling pipeline. By verifying whether the session is already registered and whether enough parameters are supplied, it enforces protocol correctness before mutating session state. It coordinates with the connection state and, when a nickname has already been established, delegates to TryCompleteRegistrationAsync to finish the registration handshake.

## Notes
- Parameter indices are assumed: username at 0 and real name at 3; a change in parameter formatting would require updating the indexing.
- Error responses use the IrcNumericReply constants and do not throw, preserving the server's asynchronous control flow.

---

## HandleWhoAsync
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


HandleWhoAsync implements the server-side handling for an IRC WHO command. It validates that the caller is registered, ensures a channel parameter is present, translates the IRC channel name to EchoHub’s channel, retrieves the list of online users for that channel, and streams a series of RPL_WHOREPLY numerics back to the IRC connection for each user. It finishes with an RPL_ENDOFWHO to signal completion. This method is invoked when an IRC client requests WHO information for a channel and centralizes the discovery and formatting of that data so callers don’t have to assemble the numeric replies themselves.

## Remarks
By centralizing channel-user discovery and numeric formatting, this method isolates IRC protocol specifics from higher-level command handling. It relies on IrcToEchoHubChannel to map an IRC channel to the EchoHub channel, on _chatService to obtain live user presence, and on the IRC connection to deliver the numeric replies, enabling real-time reflection of channel membership in WHO responses without duplicating logic elsewhere.

## Notes
- Early exits (not registered, missing parameters, or a null channel translation) result in no WHO responses, which can appear as a silent failure to the requester.
- The WHO data is produced from a live snapshot of online users; if user states change during iteration, the resulting list may reflect the state at retrieval time but not beyond that moment.
- The away status is encoded as a single flag ('G' for away, 'H' for here); ensure consumers interpret these flags consistently with the IRC WHO semantics.


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


Processes a WHOIS command for a given nickname. It first ensures the client is registered, then extracts the target nick from the command parameters (normalizing to lowercase for lookup) and retrieves the corresponding user profile. If no profile exists, it replies with ERR_NOSUCHNICK and stops. If a profile is found, it sends the standard WHOIS sequence: RPL_WHOISUSER with the user identity and display name (falling back to the nick), RPL_WHOISSERVER with the server name, and, if the user belongs to channels, RPL_WHOISCHANNELS listing those channels prefixed with '#'. If the profile is marked Away and has a StatusMessage, it also sends RPL_AWAY. It then computes idle time from LastSeenAt and sign-on time from CreatedAt (as a Unix timestamp) and sends RPL_WHOISIDLE. The interaction ends with RPL_ENDOFWHOIS to indicate completion. All data fetches and replies are performed asynchronously via _userService, _chatService, and _conn.

---

## IrcToEchoHubChannel
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


IrcToEchoHubChannel converts an IRC channel (prefixed with '#') into a normalized EchoHub channel name. It strips the leading '#', lowercases and trims the remainder, then validates it against ValidationConstants.ChannelNameRegex; if valid it returns the normalized name, otherwise null. This centralizes channel-name normalization and validation for IRC-to-EchoHub mappings.

## Remarks
It encapsulates the rule set for what constitutes a valid EchoHub channel name and ensures IRC-derived channels are consistently transformed before use. By delegating to ChannelNameRegex, it keeps the validation policy centralized in ValidationConstants and reduces duplication across callers.

## Notes
- Input must be non-null; passing null will result in a NullReferenceException.
- If the input does not start with '#', the method returns null.

---

## RequireRegisteredAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task<bool> RequireRegisteredAsync()
```

**Returns:** `Task<bool>`


A guard that ensures a client is registered before proceeding with commands that require registration. It checks the connection's registration state and, if unregistered, sends the IRC numeric ERR_NOTREGISTERED to the client and returns false; if registered, it returns true to allow the caller to continue.

## Remarks
This method centralizes the common pattern of validating registration and signaling the appropriate protocol error in one place. By encapsulating the check and the error response, it prevents duplicated boilerplate across command handlers and ensures a consistent user experience when an unregistered client attempts to execute restricted commands. The implementation ties closely to the connection object and the server name, reflecting a small, server-side guard in the command handling layer.

## Example
```csharp
if (await RequireRegisteredAsync())
{
    // proceed with handling the command
}
```

## Notes
- The method relies on _conn.SendNumericAsync to emit ERR_NOTREGISTERED; if that call throws, the exception will propagate to the caller and should be handled at a higher level.
- This guard only enforces registration; additional authorization checks may be required for commands with different access requirements.


---

## RunAsync
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


This RunAsync method implements the core processing loop for an IRC command handler. It continuously reads lines from the underlying connection until the provided CancellationToken signals cancellation or the stream ends, trims trailing CR and LF characters, ignores empty lines, logs each incoming line for diagnostics, parses it into an IrcMessage, and delegates processing to HandleCommandAsync. Any exceptions thrown by the command handler are caught and logged to prevent a single failing command from tearing down the loop.

## Remarks
Runs as the main worker loop for command handling. By separating the IO, parsing, and command dispatch, it keeps the high-level flow straightforward and resilient: the loop continues after recoverable errors, and cancellation is respected through the token. It relies on the IrcMessage type to capture the IRC line structure and on HandleCommandAsync to implement the specific command semantics.

## Notes
- Parsing errors are not caught here; a malformed line will propagate and may terminate RunAsync if not observed by the caller.
- Only exceptions from HandleCommandAsync are swallowed by the loop and logged; exceptions from parsing or line reading will bubble up.

---

## SendChannelTopicAsync
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


SendChannelTopicAsync fetches the current topic for a given IRC channel from the channel service and emits the appropriate IRC numeric reply back to the client. If the channel does not exist, the method exits without sending a response; if a topic is present, it sends RPL_TOPIC with the channel and topic; if there is no topic, it sends RPL_NOTOPIC with a standard message.

## Remarks
By centralizing the topic-response logic, this helper hides the protocol details (which numeric codes are used) from higher-level command handling and coordinates the channel state with the connection layer. It relies on IrcNumericReply to supply the correct constants and on the channel service to reflect the channel's topic state, ensuring consistent behavior across callers.

## Notes
- Empty topic edge-case: topic != null allows empty string; you may want to treat empty string as "not set" if that’s semantically important.
- Silent no-op for non-existent channels: the method returns early; this means callers must handle such cases if differentiation is required.
- Exception propagation: SendNumericAsync is awaited without a catch; downstream code or global handlers should log or manage failures.

---

## SendMotdAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendMotdAsync()
```

**Returns:** `Task`


Sends the Message of the Day (MOTD) to the connected IRC client by streaming the configured Motd via IRC numeric replies. If no Motd is configured, it returns ERR_NOMOTD and does not send MOTD content; otherwise it emits RPL_MOTDSTART, followed by one RPL_MOTD line per line of Motd (splitting on newline and trimming trailing carriage returns), and finally RPL_ENDOFMOTD.

## Remarks
By encapsulating MOTD delivery in this method, the class keeps protocol details separate from MOTD storage. It relies on the IrcNumericReply constants to ensure the IRC protocol semantics are preserved, while the Motd option provides the content. The implementation ensures a deterministic, line-oriented delivery, preserving order and formatting for all lines of the message.

## Notes
- Correctly handles Windows CRLF endings by trimming '\r' from each line before sending.
- Early return on missing MOTD ensures the client is notified with an error numeric (ERR_NOMOTD) instead of silently failing.

---

## SendNamesReplyAsync
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


This private async method answers an IRC NAMES request for a given channel by gathering the currently online users and sending the appropriate numeric replies. It fetches the online users from the chat service, constructs a space-separated list of their usernames, and emits the IRC NAMES sequence (RPL_NAMREPLY) followed by the end marker (RPL_ENDOFNAMES).

## Remarks
Consolidates the IRC protocol framing for the /NAMES response in a single, reusable unit. It coordinates between the chat service (for the source of truth about who is online) and the connection layer (for emitting the numeric replies to the client). This separation keeps higher-level command handling focused on protocol flow and channel semantics while centralizing how user lists are presented to clients.

## Notes
- No internal error handling is visible; exceptions from GetOnlineUsersAsync or SendNumericAsync propagate to the caller. 
- If there are no online users, the joined nick string is empty, resulting in a NAMREPLY payload that reflects an empty channel user list. 
- The two replies are sent in sequence: NAMREPLY is emitted first, and ENDOFNAMES follows after the first operation completes.


---

## SendWelcomeBurstAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task SendWelcomeBurstAsync()
```

**Returns:** `Task`


Sends the initial IRC welcome sequence to the connected client. It reads the active nickname from the connection and transmits a fixed set of numeric replies (WELCOME, YOURHOST, CREATED, MYINFO, ISUPPORT) using the server name and nickname, establishing the standard IRC handshake before presenting the MOTD. The CREATED reply embeds the current UTC date, and once the greeting messages are dispatched, the method delegates to SendMotdAsync() to deliver the Message of the Day. This encapsulates the startup handshake for a new IRC session and should be invoked during the connection establishment when a client is being welcomed.

---

## TryCompleteRegistrationAsync
> **File:** `src/EchoHub.Server.Irc/IrcCommandHandler.cs`  
> **Kind:** method

```csharp
private async Task TryCompleteRegistrationAsync()
```

**Returns:** `Task`


Completes the registration by validating the client’s identity via SASL authentication or PASS-based login and, on success, wiring the user into the chat system and delivering the welcome sequence. It short-circuits early if capability negotiation is in progress or the user is already registered, supports SASL-first flow by marking the connection as registered and notifying the chat service, and will automatically create a new user if password-based authentication fails before finalizing the connection state.

## Remarks
By centralizing the final step of registration, this method coordinates the connection state, user identity, and side effects (notifying the chat service and sending the welcome burst) in one place. It encapsulates both SASL and PASS authentication paths and ensures that a successful authentication consistently updates UserId, Nickname, IsAuthenticated, and IsRegistered, while announcing the newly connected user to the chat service.

## Notes
- Implicitly creates a user if authentication fails and registration succeeds, thereby transitioning a nickname into a registered account.
- Relies on Nickname being non-null when calling authentication and assigning User/Username; the code uses null-forgiving operators, so upstream flow must ensure Nickname is set.

---