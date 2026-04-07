# Channels

## Channel Creation

Channels are created via the REST API. Public channels are broadcast to all
connected clients.

```mermaid
sequenceDiagram
    participant Client as Client (TUI/API)
    participant CC as ChannelsController
    participant ChS as ChannelService
    participant CS as ChatService
    participant DB as SQLite
    participant SRB as SignalRBroadcaster
    participant IRCB as IrcBroadcaster

    Client->>CC: POST /api/channels {name, topic, isPublic}
    CC->>ChS: CreateChannelAsync(creatorId, name, topic, isPublic)
    ChS->>ChS: Normalize name (lowercase, trim)
    ChS->>ChS: Validate format (2-100 chars, regex)
    ChS->>DB: Check duplicate
    ChS->>DB: INSERT Channel + ChannelMembership (creator auto-added)
    ChS-->>CC: ChannelDto

    alt Channel is public
        CC->>CS: BroadcastChannelUpdatedAsync(channel)
        CS->>SRB: SendChannelUpdatedAsync(channelDto)
        SRB->>SRB: Notify all SignalR clients
        CS->>IRCB: SendChannelUpdatedAsync(channelDto)
    end

    CC-->>Client: 201 Created (ChannelDto)
```

---

## Channel Deletion

Only the channel creator (or admin) can delete a channel. The default channel
is protected.

```mermaid
sequenceDiagram
    participant Client as Client
    participant CC as ChannelsController
    participant ChS as ChannelService
    participant DB as SQLite

    Client->>CC: DELETE /api/channels/{channel}
    CC->>ChS: DeleteChannelAsync(channelName, callerId)
    ChS->>ChS: Reject if default channel
    ChS->>DB: Lookup channel
    ChS->>ChS: Verify caller is creator or admin
    ChS->>DB: DELETE Channel (cascade: messages, memberships)
    ChS-->>CC: Success
    CC-->>Client: 204 No Content
```

---

## Joining a Channel

Both SignalR and IRC clients join channels through `ChatService`. The presence
tracker determines if this is a genuinely new join (vs. a second connection) and
broadcasts accordingly.

```mermaid
sequenceDiagram
    participant Client as Client (SignalR or IRC)
    participant Entry as ChatHub / IrcCommandHandler
    participant CS as ChatService
    participant ChS as ChannelService
    participant PT as PresenceTracker
    participant DB as SQLite
    participant SRB as SignalRBroadcaster
    participant IRCB as IrcBroadcaster

    Client->>Entry: JoinChannel / JOIN #channel
    Entry->>CS: JoinChannelAsync(connectionId, userId, username, channel)
    CS->>ChS: EnsureChannelMembershipAsync(userId, channel)
    ChS->>DB: INSERT ChannelMembership (if not exists)

    CS->>PT: JoinChannel(username, channel)
    PT-->>CS: isNewJoin?

    alt First connection in this channel
        CS->>DB: Fetch UserPresenceDto
        CS->>CS: BroadcastToAllAsync(SendUserJoinedAsync)
        par
            CS->>SRB: SendUserJoinedAsync(channel, user, excludeConn)
        and
            CS->>IRCB: SendUserJoinedAsync(channel, user)
        end
    end

    CS->>DB: Fetch message history
    CS-->>Entry: (history, error)
    Entry-->>Client: History messages
```

---

## Leaving a Channel

```mermaid
sequenceDiagram
    participant Client as Client
    participant Entry as ChatHub / IrcCommandHandler
    participant CS as ChatService
    participant PT as PresenceTracker
    participant SRB as SignalRBroadcaster
    participant IRCB as IrcBroadcaster

    Client->>Entry: LeaveChannel / PART #channel
    Entry->>CS: LeaveChannelAsync(connectionId, username, channel)
    CS->>PT: LeaveChannel(username, channel)
    CS->>CS: BroadcastToAllAsync(SendUserLeftAsync)
    par
        CS->>SRB: SendUserLeftAsync(channel, username)
    and
        CS->>IRCB: SendUserLeftAsync(channel, username)
    end
```
