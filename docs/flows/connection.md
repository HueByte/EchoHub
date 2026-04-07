# Connection

## SignalR Client Connection

After authentication, the TUI client establishes a SignalR WebSocket, registers
event handlers, joins the default channel, and loads history.

```mermaid
sequenceDiagram
    participant CM as ConnectionManager
    participant EHC as EchoHubConnection
    participant Hub as ChatHub
    participant CS as ChatService
    participant PT as PresenceTracker
    participant DB as SQLite

    CM->>CM: Fetch encryption key (GET /api/server/encryption-key)
    CM->>EHC: new EchoHubConnection(apiClient, encryption)
    EHC->>EHC: Build HubConnection (URL + JWT token provider + auto-reconnect)
    EHC->>EHC: RegisterHandlers() — wire ReceiveMessage, UserJoined, etc.
    EHC->>Hub: ConnectAsync() → WebSocket handshake
    Hub->>CS: UserConnectedAsync(connectionId, userId, username)
    CS->>PT: UserConnected(connectionId, userId, username)
    CS->>DB: Update user: Status=Online, LastSeenAt=now
    CM->>CM: Fetch channel list (GET /api/channels)
    CM->>EHC: JoinChannelAsync("general")
    EHC->>Hub: InvokeAsync("JoinChannel", "general")
    Hub->>CS: JoinChannelAsync(connectionId, userId, username, "general")
    CS->>DB: EnsureChannelMembership
    CS->>PT: JoinChannel(username, "general")
    CS->>CS: BroadcastToAllAsync → UserJoined
    CS->>DB: Fetch message history
    Hub-->>EHC: List<MessageDto> (encrypted)
    EHC-->>CM: Decrypted history
```

---

## IRC Client Connection

IRC clients connect via TCP, authenticate with PASS/NICK/USER or SASL PLAIN,
and auto-join channels. New usernames are auto-registered.

```mermaid
sequenceDiagram
    participant IRC as IRC Client
    participant GW as IrcGatewayService
    participant CH as IrcCommandHandler
    participant US as UserService
    participant CS as ChatService
    participant PT as PresenceTracker

    IRC->>GW: TCP connect (:6667 or :6697 TLS)
    GW->>GW: Accept + create IrcClientConnection
    GW->>CH: new IrcCommandHandler(connection, services)
    GW->>CH: RunAsync() — start read loop

    alt SASL PLAIN
        IRC->>CH: CAP REQ :sasl
        CH-->>IRC: CAP ACK :sasl
        IRC->>CH: AUTHENTICATE PLAIN
        CH-->>IRC: AUTHENTICATE +
        IRC->>CH: AUTHENTICATE <base64(\0user\0pass)>
        CH->>US: AuthenticateUserAsync(user, pass)
        alt Auth fails → auto-register
            CH->>US: RegisterUserAsync(user, pass)
        end
        CH-->>IRC: 903 :SASL authentication successful
    else PASS/NICK/USER
        IRC->>CH: PASS <password>
        IRC->>CH: NICK <nickname>
        IRC->>CH: USER <username> 0 * :<realname>
        CH->>US: AuthenticateUserAsync(nick, pass)
        alt Auth fails → auto-register
            CH->>US: RegisterUserAsync(nick, pass)
        end
    end

    CH->>CS: UserConnectedAsync(irc-{guid}, userId, username)
    CS->>PT: UserConnected(irc-{guid}, userId, username)
    CH-->>IRC: 001-004 RPL_WELCOME burst + MOTD

    Note over IRC,CH: Client is now ready for JOIN/PART/PRIVMSG
```

---

## User Disconnect & Presence

When a client disconnects, the presence tracker determines if the user has any
remaining connections. If not, status is set to Invisible and all channels are
notified.

```mermaid
sequenceDiagram
    participant Client as Client
    participant Entry as ChatHub / IrcGatewayService
    participant CS as ChatService
    participant PT as PresenceTracker
    participant DB as SQLite
    participant SRB as SignalRBroadcaster

    Client->>Entry: Disconnect / TCP close
    Entry->>CS: UserDisconnectedAsync(connectionId)
    CS->>PT: Get username + channels (before removal)
    CS->>PT: UserDisconnected(connectionId)
    PT->>PT: Remove connection from tracking

    alt No remaining connections for user
        CS->>DB: Update user: Status=Invisible, LastSeenAt=now
        loop For each channel user was in
            CS->>CS: BroadcastToAllAsync(SendUserStatusChangedAsync)
            CS->>SRB: Notify channel members
        end
    end
```
