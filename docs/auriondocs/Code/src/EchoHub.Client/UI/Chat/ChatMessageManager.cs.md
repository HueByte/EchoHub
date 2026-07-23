# ChatMessageManager.cs

> **Source:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`

## Contents

- [ChatMessageManager](#chatmessagemanager)
  - [CurrentChannel](#currentchannel)
  - [CurrentUser](#currentuser)
  - [LastReadIds](#lastreadids)
  - [MentionChannels](#mentionchannels)
  - [ActionHeaderSegments](#actionheadersegments)
  - [AddMessage](#addmessage)
  - [AddStatusMessage](#addstatusmessage)
  - [AddSystemMessage](#addsystemmessage)
  - [AttachmentActionLine](#attachmentactionline)
  - [ClearAll](#clearall)
  - [ClearChannelMessages](#clearchannelmessages)
  - [ClearUnread](#clearunread)
  - [DateRule](#daterule)
  - [DateRuleLabel](#daterulelabel)
  - [FormatEmbed](#formatembed)
  - [FormatFileSize](#formatfilesize)
  - [FormatMessage](#formatmessage)
  - [FormatTime](#formattime)
  - [FormatWithDateRules](#formatwithdaterules)
  - [GetMessages](#getmessages)
  - [GetUnreadCount](#getunreadcount)
  - [GetUnreadCounts](#getunreadcounts)
  - [HeaderSegments](#headersegments)
  - [ImageActionLine](#imageactionline)
  - [LoadHistory](#loadhistory)
  - [MarkRead](#markread)
  - [PadNick](#padnick)
  - [PrependHistory](#prependhistory)
  - [RailPrefix](#railprefix)
  - [RemoveMessage](#removemessage)
  - [RemoveUnreadMarker](#removeunreadmarker)
  - [ReplyQuoteLine](#replyquoteline)
  - [SeedUnreadFromHistory](#seedunreadfromhistory)
  - [SetChatWidth](#setchatwidth)
  - [SetCurrentUser](#setcurrentuser)
  - [SystemHeaderSegments](#systemheadersegments)
  - [UnreadMarkerRule](#unreadmarkerrule)
  - [WordWrap](#wordwrap)
  - [ContentIndentCols](#contentindentcols)
  - [NickColWidth](#nickcolwidth)

---

## ChatMessageManager
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** class

```csharp
public sealed class ChatMessageManager
```


Manages in-memory storage, formatting and mutation of chat messages for the UI. Use `ChatMessageManager` when the UI needs a single authoritative source of formatted [`ChatLine`](ChatLine.cs.md) objects per channel (instead of rendering raw [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md)), together with built-in tracking for unread counts, @mentions, and the "new messages" anchor; the manager raises the `MessagesChanged` event to notify views after any change.

## Remarks
`ChatMessageManager` is the UI-layer message store and formatter: it converts incoming [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) instances into [`ChatLine`](ChatLine.cs.md) entries (including attachments, continuation lines and mention detection), keeps per-channel lists (`_channelMessages`), and maintains per-channel state such as `_channelUnread`, `_channelLastDate`, `_markedChannels`, `_markerAnchor`, `_mentionChannels`, `_lastRead` and `_channelNewestId`. It exposes read-only views like `LastReadIds` and `MentionChannels`, publishes `MessagesChanged` (the event handler receives the channel name) after mutations, and defines layout constants `NickColWidth` and `ContentIndentCols` used when preparing [`ChatLine`](ChatLine.cs.md) content. Leaving the active channel consumes the current "new messages" marker and treats visible messages as read (see `CurrentChannel` behavior). System and status messages are added via `AddSystemMessage`/`AddStatusMessage` with colored styling (the implementation uses color attributes to build [`ChatLine`](ChatLine.cs.md) segments).

## Notes
- `ChatMessageManager` has no internal synchronization in the implementation; treat it as single-thread/UI-thread affinity or ensure callers serialize access to avoid race conditions.
- The internal `GetUnreadCounts()` returns the live `_channelUnread` dictionary (not a defensive copy); callers outside the defining assembly should not mutate it and consumers inside the assembly should treat it as the authoritative store.
- `LastReadIds` is exposed as an `IReadOnlyDictionary<string, Guid>` and is intended to be persisted/seeded by the orchestrator so unread/mention state can be restored across reconnects.

---

### CurrentChannel
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public string CurrentChannel
```


The `CurrentChannel` property tracks the actively viewed chat channel for unread tracking and `@mention` detection. When set to a different channel, it calls `RemoveUnreadMarker` and `MarkRead` on the old channel and then updates `_currentChannel`. This irssi-like behavior causes leaving a channel to consume its unread marker so the next burst starts fresh, while messages seen so far are considered read.

## Remarks
This property centralizes per-channel unread-state transitions, preventing scattered logic across the UI. It encapsulates the behavior that leaving a channel marks it as read and clears its unread marker, aligning channel navigation with message visibility and mention detection.

---

### CurrentUser
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public string CurrentUser => _currentUser
```


CurrentUser is a read-only property that returns the value of the private `_currentUser` field. It offers a simple accessor to retrieve the identifier of the user associated with the current chat message context, without allowing mutation. Use it when you need to display, log, or branch logic based on the active user.

---

### LastReadIds
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public IReadOnlyDictionary<string, Guid> LastReadIds => _lastRead
```


LastReadIds is a read-only dictionary that maps each channel identifier to the GUID of the last message the user has read in that channel. It is persisted by the orchestrator so unread/mention state can be seeded from history on the next connect via the underlying `_lastRead` store.

## Remarks
Because this property type is `IReadOnlyDictionary<string, Guid>`, callers can read per-channel last-read IDs but cannot mutate them directly. Updates to this state are performed by the orchestrator that owns `_lastRead`, ensuring a single source of truth for read progress. The dictionary's keys are channel IDs and the values are the corresponding message GUIDs used to determine which messages are considered unread or mentioned on reconnection.

## Example
```csharp
// Safe access: check if a channel has a recorded last read
if (LastReadIds.TryGetValue("general", out Guid lastReadGeneral))
{
    // use lastReadGeneral
}
```

## Notes
- Accessing a channel that has no entry via the indexer can throw `KeyNotFoundException`; prefer `TryGetValue` or check `ContainsKey` before indexing.
- This property is read-only; to update the last-read information, update the underlying store through the orchestrator that manages `_lastRead`.

---

### MentionChannels
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public IReadOnlySet<string> MentionChannels => _mentionChannels
```


MentionChannels is a read-only view of the channels that currently have an unread @mention for the current user. It exposes the internal `_mentionChannels` as an `IReadOnlySet<string>` so UI code can display mention indicators without mutating internal state; the underlying collection is cleared by `ClearUnread` when the user acknowledges those mentions.

## Remarks
This property serves as a stable projection of unread-mention state to the UI, decoupling presentation from private state. It keeps mutation confined to internal logic while exposing a safe, read-only view of the channels requiring attention.

## Notes
- The collection is exposed as an `IReadOnlySet<string>`; callers should not attempt to mutate it. Any updates must go through internal logic that updates `_mentionChannels` and raises the appropriate UI refresh.


---

### ActionHeaderSegments
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static List<ChatSegment> ActionHeaderSegments(string time) =>
    [
        new($"{time} ", ChatColors.TimestampAttr),
        new(PadNick("*"), ChatColors.TimestampAttr),
        new(" │ ", ChatColors.RailAttr),
    ]
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `time` | `string` | — |

**Returns:** `List<ChatSegment>`


Builds the header for /me action messages by composing three chat segments: the provided time string styled as a timestamp, a starred nickname via `PadNick("*")` styled with the same timestamp color, and a rail divider styled with `ChatColors.RailAttr`. It returns a new `List<ChatSegment>` that callers pass to the chat renderer to produce a consistent header for /me actions.

## Remarks
This helper encapsulates the exact header layout for action messages, so changes to styling or ordering are centralized. By consistently using `ChatColors.TimestampAttr` for the time and `ChatColors.RailAttr` for the divider, it ensures a uniform appearance with other header variants. Returning a fresh list preserves the header construction as an explicit, side-effect-free operation for callers.


---

### AddMessage
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void AddMessage(MessageDto message)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `message` | [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** `void`


Formats and stores a received [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) into per-channel history, applying day-boundary separators, updating read/unread state, and notifying listeners. It formats the message with `FormatMessage(message)`, ensures a per-channel list exists in `_channelMessages`, and inserts a date rule via `DateRule` whenever the message's local date differs from the last recorded date for that channel (derived from `message.SentAt`). It marks the message as read when it belongs to the active channel (`_currentChannel`), updates `_lastRead` and the channel's newest id, and, for inactive channels, adds an initial unread marker anchored to this message. The method then appends all formatted lines, increments the per-channel unread count, tracks mentions by checking `IsMention` on any line, and finally raises the `MessagesChanged` event for the affected channel.

## Remarks
Centralizes the ingestion of incoming messages, coupling formatting, date segmentation, unread bookkeeping, and event propagation into a single place. This reduces scattered updates across the UI and ensures consistent behavior when messages arrive for either the active or inactive channels. It relies on internal per-channel dictionaries and sets (e.g. `_channelMessages`, `_channelLastDate`, `_currentChannel`, `_lastRead`, `_markedChannels`, `_markerAnchor`, `_channelUnread`, `_mentionChannels`, and the `MessagesChanged` event) to maintain state and emit notifications.

## Notes
- Be mindful of concurrency: `_channelMessages`, `_channelUnread`, and related state are mutated here without explicit synchronization; callers streaming messages for the same channel concurrently should serialize updates to avoid races.

---

### AddStatusMessage
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void AddStatusMessage(string channelName, string username, string status)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `username` | `string` | — |
| `status` | `string` | — |

**Returns:** `void`


Adds a status change message to a chat channel by composing a time-stamped system message that declares a user’s new status. It builds a header via `FormatTime(DateTimeOffset.Now)` and `SystemHeaderSegments`, appends a system-colored segment with the content "{username} is now {status}" using `ChatColors.SystemAttr`, ensures the target channel exists in `_channelMessages`, and stores a new [`ChatLine`](ChatLine.cs.md) (with its `ContinuationPrefixSegments` set by `RailPrefix()`) in that channel. If the affected channel is currently active (`_currentChannel`), it raises `MessagesChanged` to prompt the UI to refresh. This method centralizes status updates as consistently styled system messages within the chat history, shielding callers from the details of message construction and channel management.

---

### AddSystemMessage
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void AddSystemMessage(string channelName, string text)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `text` | `string` | — |

**Returns:** `void`


Adds a system/informational message to a named chat channel, styling the header and body with the system color attribute. It ensures the channel's message list exists, builds a timestamp with `FormatTime`, and renders multi-line text by placing the first line in a header and each subsequent non-empty line as a continuation line prefixed with `RailPrefix` and colored via `ChatColors.SystemAttr`. If the targeted channel is currently active (`_currentChannel`), it raises the `MessagesChanged` event to refresh the UI.

## Remarks
This method centralizes the rendering policy for system messages, ensuring consistent visual treatment across channels. By composing [`ChatLine`](ChatLine.cs.md) instances from a header built with `SystemHeaderSegments(time)` and per-line continuation segments via `RailPrefix()`, it enforces a cohesive, rail-prefixed block that clearly marks informational notices. It also isolates the UI update trigger to the active channel through `MessagesChanged`.

## Notes
- It uses a direct `DateTimeOffset.Now` for the timestamp, which can affect testability and determinism.
- Blank lines in the input text after the header are ignored; only non-empty lines after the first are rendered.
- The code assumes `_channelMessages` can be mutated by adding lists; thread-safety is not shown.

---

### AttachmentActionLine
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static ChatLine AttachmentActionLine(string text, Attribute color, AttachmentDto attachment)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `text` | `string` | — |
| `color` | `Attribute` | — |
| `attachment` | [`AttachmentDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** [`ChatLine`](ChatLine.cs.md)


Builds a clickable attachment line carrying the metadata the message list uses to route activation (play audio, download file, save original image). It constructs a [`ChatLine`](ChatLine.cs.md) by starting with `RailPrefix()` for its segments, adds a colored `text` segment, and returns a [`ChatLine`](ChatLine.cs.md) initialized with those segments. The returned object populates `AttachmentUrl`, `AttachmentFileName`, and [`AttachmentKind`](../../../EchoHub.Core/Models/AttachmentKind.cs.md) from the provided `attachment`, and sets `ContinuationPrefixSegments` to a fresh `RailPrefix()` so continuation rails render consistently.

## Remarks
This helper centralizes how attachment actions are rendered in the chat UI. By wrapping the segment construction and attachment-metadata binding in one place, it guarantees consistent appearance and reliable routing for actions like playing, downloading, or saving attachments across the message list.

## Notes
- Assumes a non-null [`AttachmentDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) for `attachment`; passing null will throw a `NullReferenceException` when accessing `attachment.Url`, `attachment.FileName`, or `attachment.Kind`.


---

### ClearAll
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void ClearAll()
```

**Returns:** `void`


Resets all message state by clearing internal caches and resetting the current context. This method is intended to be called on disconnect to guarantee a clean slate for the next session, by clearing per-channel stores such as `_channelMessages`, `_channelUnread`, `_channelLastDate`, `_markedChannels`, `_markerAnchor`, `_mentionChannels`, `_lastRead`, and `_channelNewestId`, and by resetting `_currentChannel` and `_currentUser` to `string.Empty`.

## Remarks
By centralizing the teardown logic in `ClearAll`, the class avoids scattered cleanup code across multiple paths. It encapsulates what it means to reset message state, so after a disconnect the object is in a well-defined, initial state ready for a new connection. This helps prevent subtle bugs caused by leftover state persisting between sessions and simplifies future maintenance.

## Notes
- Calling `ClearAll` while message processing is ongoing may cause transient inconsistencies if concurrent access occurs; coordinate with any ongoing operations or ensure proper synchronization before disconnect.

---

### ClearChannelMessages
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void ClearChannelMessages(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


Clears all messages associated with the specified channel (`channelName`) and resets the per-channel state by clearing the collection in `_channelMessages` and removing related metadata from `_channelLastDate`, `_markedChannels`, and `_markerAnchor`. If the cleared channel matches `_currentChannel`, it triggers the `MessagesChanged` event to notify listeners to refresh the UI.

## Remarks
Centralizes per-channel cleanup so callers don’t manually touch `_channelMessages`, `_channelLastDate`, `_markedChannels`, or `_markerAnchor`, reducing duplication and the risk of inconsistent state. By only raising the `MessagesChanged` event when the cleared channel is the active one (`_currentChannel`), it keeps UI updates efficient and scoped to the currently viewed channel.

---

### ClearUnread
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void ClearUnread(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


Resets the unread state for a given channel by setting its unread counter to zero, removing any pending mention for that channel, and applying the read-state via `MarkRead`.

## Remarks
This is the centralized operation used when a user acknowledges messages in a channel. It ensures unread indicators and mention flags stay in sync by updating `_channelUnread`, removing the channel from `_mentionChannels`, and delegating to `MarkRead` for any additional read-state side effects.

---

### DateRule
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static ChatLine DateRule(DateTime date)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `date` | `DateTime` | — |

**Returns:** [`ChatLine`](ChatLine.cs.md)


DateRule takes a `DateTime` and returns a [`ChatLine`](ChatLine.cs.md) that renders a date-based separator in the chat UI. It computes a label with `DateRuleLabel(date)` and uses a single segment containing the decorative string `── {label} ──` colored by `ChatColors.DateRuleAttr`. The returned [`ChatLine`](ChatLine.cs.md) is tagged with `RuleLabel = label` and `RuleAttr = ChatColors.DateRuleAttr` for downstream styling and identification. This internal helper is used to insert consistent date separators into the chat stream.

## Remarks
By funneling date-separator creation through this helper, the UI ensures all date rules share the same label-generation point (`DateRuleLabel`) and styling (`ChatColors.DateRuleAttr`). It constructs a new [`ChatLine`](ChatLine.cs.md) without mutating existing state, acting purely as a formatter/renderer within the chat assembly process.

## Example
```csharp
var line = DateRule(DateTime.Today);
```

## Notes
- It relies on `DateRuleLabel(date)` for the label; any change to that method changes all date separators generated by `DateRule`.
- As a private helper, it's only callable from within its containing type; external code cannot call it directly, which is intentional to keep the formatting internal.

---

### DateRuleLabel
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
internal static string DateRuleLabel(DateTime date) => date.ToString("ddd, MMM d yyyy")
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `date` | `DateTime` | — |

**Returns:** `string`


Formats the provided `DateTime` as a compact label using the pattern `ddd, MMM d yyyy` and returns the resulting string. This internal helper centralizes date-label formatting for the UI (for example, chat message headers) to ensure consistency and avoid duplicating formatting logic across call sites.

## Remarks
This small helper centralizes the specific date-label format in one place, ensuring consistent UI labeling across chat-related components. Because it relies on `DateTime.ToString` with a culture-aware format specifier, the output respects the current culture's short day and month names; changing the style in one place will propagate wherever `DateRuleLabel` is used. It is an internal static method, so it's not part of the public API.

## Example
```csharp
string label = DateRuleLabel(new DateTime(2024, 5, 1)); // "Wed, May 1 2024"
```


---

### FormatEmbed
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static List<ChatLine> FormatEmbed(EmbedDto embed, int chatWidth)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `embed` | [`EmbedDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |
| `chatWidth` | `int` | — |

**Returns:** `List<ChatLine>`


Formats an embed payload into a vertical sequence of chat lines suitable for rendering in the UI. Given an [`EmbedDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) with `SiteName`, `Title`, `Description`, and `ThemeColor`, it returns a `List<ChatLine>` that visually represents the embed by prefixing each line with a left border and applying color attributes. The method computes the available text width as `chatWidth - ContentIndentCols - borderCols`, ensuring a minimum of 20 characters, then selects the border color by calling `HexColorHelper.ParseHexColor(embed.ThemeColor)` and falling back to `ChatColors.EmbedBorderAttr` if parsing fails. A local helper `AddTextLine` prefixes lines with a rail and the border attr, then appends the actual text as a [`ChatSegment`](ChatSegment.cs.md) with the appropriate color (title, description, etc.). It emits optional sections for `SiteName` (with the border color), `Title` (wrapped via `WordWrap` to the computed width and styled with `ChatColors.EmbedTitleAttr`), and `Description` (wrapped similarly and styled with `ChatColors.EmbedDescAttr`). The result is a cohesive, themed embed block ready to be rendered alongside other chat content.

---

### FormatFileSize
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
internal static string FormatFileSize(long? bytes)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `bytes` | `long?` | — |

**Returns:** `string`


Formats a file size given in bytes into a human-friendly string using `B`, `KB`, `MB`, and `GB`. If the input is `null` or `0`, it returns `?` to indicate an unknown size. This helper is used when rendering attachment sizes in the chat UI to ensure consistent units and formatting.

## Remarks
By centralizing the formatting logic, `FormatFileSize` ensures consistent thresholds and decimal precision across the UI, reducing duplication and easing future changes to unit boundaries or precision. It assumes non-negative input and surfaces unknown sizes as `?` for clarity in the display layer. This symbol acts as a small, focused utility within the chat message management area, decoupling size formatting from presentation concerns.

## Notes
- Negative values are not guarded and will format as negative sizes; callers should validate input or adapt the function before display.
- The `?` sentinel indicates unknown or unavailable size; ensure the consuming UI handles this gracefully to avoid confusing output.

---

### FormatMessage
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private List<ChatLine> FormatMessage(MessageDto message)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `message` | [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** `List<ChatLine>`


FormatMessage formats a [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) into a structured list of [`ChatLine`](ChatLine.cs.md)s that render a single chat message in the UI. It computes the display time with `FormatTime`, derives a display name from `SenderDisplayName` or `SenderUsername`, and selects a `senderColor` via `HexColorHelper.ParseHexColor` or `NickColorHelper.GetAttribute`. It prepends a `ReplyQuoteLine` if the message is a reply, and handles action messages by using `MessageConventions.TryParseAction` and rendering an action header via `ActionHeaderSegments`, followed by action content lines. For regular content, it processes emojis with `EmojiHelper.ReplaceEmoji`, builds a header via `HeaderSegments`, and appends content and any subsequent lines as continuation blocks using `RailPrefix`. If the message has no text but attachments exist, it renders a compact header with a summary like `[image]` or `[n attachments]`. Each attachment produces its own block; image attachments render ASCII previews when available, and colorized segments when appropriate. The method is a private helper used by the chat rendering flow to translate a [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) into the visual [`ChatLine`](ChatLine.cs.md)s shown in the chat.

---

### FormatTime
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static string FormatTime(DateTimeOffset timestamp) =>
        timestamp.ToLocalTime().ToString("HH:mm")
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `timestamp` | `DateTimeOffset` | — |

**Returns:** `string`


Formats a given `DateTimeOffset` into a compact local-time string by first converting to local time, then formatting with the `HH:mm` format specifier to produce hours and minutes in 24-hour form. This private helper is used wherever the UI needs a concise time-of-day display for timestamps (e.g., chat messages) and guarantees times near midnight land under the correct calendar day by applying local-time rules before formatting.

## Remarks
This abstraction centralizes locale-aware time formatting for timestamps, ensuring all UI paths render the same local time portion. It converts the `DateTimeOffset` to local time via `ToLocalTime()` before applying the `HH:mm` format, so near-midnight messages are assigned to the correct date bucket according to local rules. This reduces duplication and guards against inconsistent formatting or time-zone drift across the chat UI.

---

### FormatWithDateRules
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private List<ChatLine> FormatWithDateRules(List<MessageDto> messages, out DateTime? lastDate)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `messages` | `List<MessageDto>` | — |
| `lastDate` | `DateTime?` | — |

**Returns:** `List<ChatLine>`


Formats a chronological batch of messages into chat lines, inserting a date rule before the first message and at every day boundary, and returns the batch’s last local date. Use this private helper when rendering a chat thread to ensure date separators are consistently inserted; it encapsulates the day-boundary logic and per-message formatting, instead of duplicating this control flow across callers.

## Remarks

By centralizing date-boundary handling in `FormatWithDateRules`, the UI rendering path doesn't need to know how separators are produced. It exposes a simple contract: transform a list of [`MessageDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) into [`ChatLine`](ChatLine.cs.md)s while emitting `DateRule`s whenever the day changes and tracking the most recent local date. The method delegates the actual per-message line construction to `FormatMessage`, keeping concerns separated between date logic and message formatting.

## Notes

- Date boundaries are computed using `ToLocalTime()`, so the local time zone of the runtime determines when a new `DateRule` is inserted; messages in different time zones can shift separators accordingly.

---

### GetMessages
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public List<ChatLine>? GetMessages(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `List<ChatLine>?`


Retrieves the `List<ChatLine>` for a given channel from the internal `_channelMessages` store using `TryGetValue`; if found, it returns the list, otherwise it returns `null`. Use this method when you need to access the messages for a specific `channelName` without risking an exception if the channel is missing.

---

### GetUnreadCount
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public int GetUnreadCount(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `int`


`GetUnreadCount` returns the unread message count for the specified channel by querying the internal dictionary `_channelUnread`. If the channel has no entry, it yields 0. This method encapsulates the missing-key default handling so callers can rely on a non-null int even when the channel hasn't tracked unread messages yet.

---

### GetUnreadCounts
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
internal Dictionary<string, int> GetUnreadCounts() => _channelUnread
```

**Returns:** `Dictionary<string, int>`


Returns the internal mapping of unread message counts per channel by directly exposing the private field `_channelUnread`. This method is a minimal accessor with no additional logic, simply forwarding the reference to the underlying dictionary. Call it when you need to inspect (and potentially mutate) the live counts for all channels from within the same assembly, rather than creating a new dictionary.

## Remarks

By design, this is a direct forwarder to `_channelUnread`. It avoids copying for performance but couples callers to the concrete `Dictionary<string, int>` implementation and to the internal state. If you only need to observe values, prefer returning a read-only view such as an `IReadOnlyDictionary<string, int>` or provide a separate accessor that returns a defensive copy to preserve encapsulation.

## Notes

- Mutations to the returned `Dictionary<string, int>` modify the class's internal state immediately; callers should avoid assuming immutability.
- Be mindful of thread-safety: concurrent reads/writes to `_channelUnread` without synchronization can lead to race conditions or exceptions.

---

### HeaderSegments
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static List<ChatSegment> HeaderSegments(string time, string nick, Attribute? nickColor) =>
    [
        new($"{time} ", ChatColors.TimestampAttr),
        new(PadNick(nick), nickColor),
        new(" │ ", ChatColors.RailAttr),
    ]
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `time` | `string` | — |
| `nick` | `string` | — |
| `nickColor` | `Attribute?` | — |

**Returns:** `List<ChatSegment>`


HeaderSegments is a private static helper that constructs the leading portion of a chat message header. It takes a time string, a nickname, and an optional color attribute for the nickname, and returns a `List<ChatSegment>` with three segments: a timestamp segment created from `"{time} "` using `ChatColors.TimestampAttr`, a nickname segment produced by `PadNick(nick)` colored by `nickColor`, and a rail segment containing `" │ "` colored with `ChatColors.RailAttr`. The returned header prefix precedes the message body, whose content begins at `ContentIndentCols`.

---

### ImageActionLine
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static ChatLine ImageActionLine(AttachmentDto attachment)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `attachment` | [`AttachmentDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** [`ChatLine`](ChatLine.cs.md)


Builds the action line displayed under an image preview: a compact sequence like "[open] [↓ save original] name [size]" where each bracketed element is an [`AttachmentActionSpan`](ChatLine.cs.md) so it can be targeted by mouse clicks; keyboard activation (Enter) uses the default action, open. The method constructs this line by starting with a base rail prefix, incrementally adding actions with their width in columns, and finally returns a [`ChatLine`](ChatLine.cs.md) enriched with the attachment metadata and a list of action spans for interaction.

## Remarks
This helper encapsulates the visual semantics of an image-attachment action bar. By recording [`AttachmentActionSpan`](ChatLine.cs.md)s with exact column extents and pairing them with the base rail prefix, it guarantees that every image attachment presents clickable actions in a predictable layout, while the [`ChatLine`](ChatLine.cs.md) carries all metadata (URL, file name, size, kind) for downstream rendering or interaction.

---

### LoadHistory
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void LoadHistory(string channelName, List<MessageDto> messages, Guid? lastReadId = null)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `messages` | `List<MessageDto>` | — |
| `lastReadId` | `Guid?` | `null` |

**Returns:** `void`


Loads historical messages into a channel, replacing any existing messages. The messages are formatted with date-aware rules via `FormatWithDateRules`, and when a `lastReadId` is provided (persisted from a previous session), messages after it seed the unread count, `@mention` highlight, and the "new messages" marker — so activity that happened while offline still lights up.

## Remarks
This method centralizes the process of presenting a channel’s historical backlog and synchronizing the unread state. It coordinates with the marker system to preserve the unread marker position when history is reloaded, using `_markerAnchor` and `_markedChannels` to decide where (and whether) to insert the `UnreadMarkerRule()` in the freshly formatted history. If the anchor isn’t present in the newly loaded page, the marker is dropped and the anchor mapping is cleared. When there is no active anchor but a `lastReadId` is supplied, the backlog is seeded from history via `SeedUnreadFromHistory`. The operation updates per-channel caches (`_channelMessages`, `_channelNewestId`, `_channelLastDate`) and raises `MessagesChanged` to refresh the UI.

## Notes
- If the fetched `messages` list is empty, the method still replaces the channel’s history with an empty formatted sequence and clears any stored last date for the channel.
- The unread-marker behavior depends on the anchor being present in the current fetch window; otherwise, the marker is removed, which may affect how the UI highlights the unread portion.
- The method raises `MessagesChanged` after state updates, so listeners should be prepared for synchronous reentrancy during UI refresh.

---

### MarkRead
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private void MarkRead(string channelName)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |

**Returns:** `void`


MarkRead updates the per-channel read-tracking state by recording the latest known message id for the given channel. If the provided `channelName` is non-empty and `_channelNewestId` contains a value for that channel, it assigns that value to `_lastRead[channelName]`, effectively marking all messages up to that id as read. This method is typically invoked when a user opens a channel or after messages are loaded to refresh unread indicators without altering read state when the channel is unknown or there is no known newest id.

## Remarks
This small helper encapsulates read-state mutation, tying together `_channelNewestId` (the latest-known message id per channel) with `_lastRead` (the per-channel read pointer). It prevents updates for channels that have no known newest id and keeps the UI's unread indicators consistent as users navigate or when new messages arrive.

## Notes
- If `channelName` is null or empty, or `_channelNewestId` does not contain an entry for the channel, this method becomes a no-op.

---

### PadNick
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
internal static string PadNick(string nick)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `nick` | `string` | — |

**Returns:** `string`


PadNick right-aligns a nickname into the fixed nick column by measuring its display width via `nick.GetColumns()` and truncating long nicknames with a Unicode ellipsis. It is grapheme- and column-aware, iterating grapheme clusters with `GraphemeHelper.GetGraphemes(nick)` and using `g.GetColumns()` (clamped to at least 1) to respect visual widths, stopping before exceeding `NickColWidth - 1` and appending `…` when truncation occurs. If the nickname fits, the method pads on the left with spaces to reach `NickColWidth`.

## Remarks
This symbol encapsulates the alignment policy for chat nicknames: a grapheme- and column-aware truncation to a fixed width, followed by left-padding with spaces. It centralizes the logic that keeps the nick column visually stable across scripts and emoji, decoupling width calculations from rendering code.

## Notes
- Grapheme-aware truncation prevents splitting a grapheme or emoji when fitting within `NickColWidth`.
- An ellipsis `…` is appended when truncation occurs to signal omitted content and preserve readability.

---

### PrependHistory
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void PrependHistory(string channelName, List<MessageDto> olderMessages)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `olderMessages` | `List<MessageDto>` | — |

**Returns:** `void`


PrependHistory inserts a batch of `olderMessages` at the front of a channel's in-memory buffer, skipping any items that already exist by comparing their `Id` against the set of current `MessageId`s, and formats the remaining ones using `FormatWithDateRules` into `newLines` before insertion. If no new lines are produced, the method returns early. If `lastBatchDate` is non-null and the existing buffer's first line has a `RuleLabel` equal to `DateRuleLabel(batchDate)` (and that line is not an unread marker), the code removes that leading line to avoid duplicating date separators. Finally, the new lines are inserted at the front, and if the target channel is the currently active channel (`_currentChannel`), the `HistoryPrepended` event is fired to notify the UI.

---

### RailPrefix
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static List<ChatSegment> RailPrefix() =>
    [
        new(new string(' ', 6 + NickColWidth + 1), null),
        new("│ ", ChatColors.RailAttr),
    ]
```

**Returns:** `List<ChatSegment>`


RailPrefix builds the indentation rail used to align continuation/attachment/embed lines under the message text. It returns a new mutable `List<ChatSegment>` that begins with a padding string of length 6 + `NickColWidth` + 1, followed by a rail segment `│ ` colored with `ChatColors.RailAttr`.

## Remarks

This helper centralizes rail construction so all rendering paths share the same prefix, ensuring consistent alignment and color usage for continuation rails. It depends on `NickColWidth` to determine the padding width and on `ChatColors.RailAttr` for the rail color, keeping presentation concerns in one place.

## Notes

- This method produces a fresh `List<ChatSegment>` per call; callers can mutate it without affecting other render paths.
- The exact prefix width is tied to `NickColWidth`; changing it at runtime may alter alignment across rails.
- If the rail color theme changes, `ChatColors.RailAttr` will drive the rendered color automatically.

---

### RemoveMessage
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void RemoveMessage(string channelName, Guid messageId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `messageId` | `Guid` | — |

**Returns:** `void`


Removes all lines associated with a specific message ID from the channel's message collection. It looks up the channel in the internal store `_channelMessages` and, if found, calls `RemoveAll` on the channel's list to drop any entries whose `MessageId` matches the provided `messageId`. If the affected channel is the current one (`_currentChannel`), it invokes the `MessagesChanged` event to signal the UI to refresh for that channel.

## Remarks
This method centralizes the mutation of the in-memory per-channel message store and the corresponding UI update. It encapsulates the cleanup for a given `MessageId`, ensuring all related lines are removed in one operation, and it notifies listeners only for the active channel to avoid unnecessary redraws.

## Notes
- If the channel is not present in `_channelMessages`, the call is a no-op.
- Removing by `MessageId` may delete multiple lines if duplicates exist.
- The method does not return a value; UI refresh relies on the `MessagesChanged` event when the current channel is affected.

---

### RemoveUnreadMarker
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private void RemoveUnreadMarker(string channel)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channel` | `string` | — |

**Returns:** `void`


Removes the unread marker state for a specific channel. If the provided `channel` is null or empty, or the channel is not currently tracked in `_markedChannels`, the method returns early and makes no changes. When it proceeds, it removes the channel from `_markerAnchor` and, if there are messages stored for that channel in `_channelMessages`, clears all items where `IsUnreadMarker` is true.

## Remarks
RemoveUnreadMarker centralizes the cleanup of unread-marker state across internal collections. It relies on three collaborators: `_markedChannels` to determine if the channel currently has an unread marker, `_markerAnchor` to drop the visual or structural marker, and `_channelMessages` to scrub per-message flags. By encapsulating this logic, callers avoid inconsistent states where a channel might be marked as unread while the marker remains or vice versa.

## Notes
- This is a private helper; it is intended to be invoked by other methods within the same class when the unread state for a channel should be cleared.
- It mutates multiple internal structures, so ensure appropriate synchronization if called from multiple threads.
- If `_channelMessages` has no entry for the given `channel`, the per-message cleanup is skipped gracefully.

---

### ReplyQuoteLine
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static ChatLine ReplyQuoteLine(ReplyRefDto replyTo)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `replyTo` | [`ReplyRefDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md) | — |

**Returns:** [`ChatLine`](ChatLine.cs.md)


The `ReplyQuoteLine` method constructs the quoted, dim-lined representation of a replied message that appears above the original message in the chat UI. Given a [`ReplyRefDto`](../../../EchoHub.Core/DTOs/ChatDtos.cs.md), it builds a single-line rail segment that shows the sender’s username and a truncated snippet of the original content, while carrying the original message id so activating the line jumps back to that message. The snippet is first normalized (newlines replaced), optionally rewritten into an action-format via `MessageConventions.TryParseAction`, and then passed through `EmojiHelper.ReplaceEmoji`. It truncates by grapheme width to fit within `maxSnippetCols` (60 columns) to avoid breaking grapheme clusters, appending a trailing ellipsis when needed. The final display uses the rail prefix and renders the sender name with `NickColorHelper.GetAttribute`, followed by the snippet in the system color (`ChatColors.SystemAttr`). The method returns a [`ChatLine`](ChatLine.cs.md) whose `JumpToMessageId` is set to `replyTo.MessageId` and whose `ContinuationPrefixSegments` are the rail prefix, enabling proper alignment for any following lines in the rail.

---

### SeedUnreadFromHistory
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private void SeedUnreadFromHistory(string channelName, List<MessageDto> messages,
        List<ChatLine> formatted, Guid lastReadId)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `channelName` | `string` | — |
| `messages` | `List<MessageDto>` | — |
| `formatted` | `List<ChatLine>` | — |
| `lastReadId` | `Guid` | — |

**Returns:** `void`


SeedUnreadFromHistory restores the UI unread-state after a history fetch for a given channel by locating the boundary between read and unread messages using the provided `lastReadId`, inserting an unread marker before the first unread message in the rendered `formatted` lines via `UnreadMarkerRule()`, and updating per-channel state such as `_markedChannels` and `_markerAnchor`. For channels other than the currently active one (`_currentChannel`), it also seeds the per-channel unread count (`_channelUnread`) and, if `_currentUser` is present, collects any mentions of the current user to highlight in background channels via `_mentionChannels`. If the `lastReadId` is not present in the fetched `messages` window, the first unread index becomes 0 and the entire window is treated as unread. The method is intended to be invoked during history loading to align the rendered chat with the user's last reading position.

## Remarks
SeedUnreadFromHistory centralizes unread-state reconstruction after history fetches, coordinating between the logical unread boundary, the rendered view, and channel-scoped UI hints. By inserting the marker at the exact position corresponding to the first unread message and storing an anchor, the UI can reliably indicate where unread content begins and support navigation to that point. The method differentiates the active channel (which does not accrue badges or mention highlights) from background channels, populating per-channel unread counts and optional @mention tracking to enhance visibility without cluttering the current reading experience.

## Notes
- If the `lastReadId` is not present in the fetched `messages`, the calculation yields an index of 0 and the entire window is marked as unread.
- If the anchor line cannot be found in `formatted`, no marker is inserted and no per-channel state is updated for that call.
- The operation mutates both the rendered view (`formatted`) and several per-channel state collections; callers should ensure it runs in a UI-context where such mutations are safe and up-to-date with the latest history fetch.
- Mention detection is case-insensitive and checks both message content for `@currentUser` and the sender of a replied-to message, if available.


---

### SetChatWidth
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void SetChatWidth(int width) => _chatWidth = width
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `width` | `int` | — |

**Returns:** `void`


Updates the internal `_chatWidth` field to the provided value, effectively setting the chat panel's width. Call this method when you need to adjust the chat area at runtime (e.g., in response to layout changes or user preferences) rather than modifying the field directly.

---

### SetCurrentUser
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void SetCurrentUser(string username) => _currentUser = username
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `username` | `string` | — |

**Returns:** `void`


Sets the current user by assigning the provided `username` to the internal `_currentUser` field. This simple mutator establishes the active user context for subsequent chat message operations that depend on the current user.

## Remarks
This is a straightforward mutator that updates internal state by assigning to `_currentUser`. It does not perform validation or trigger side effects beyond updating the active user; callers should ensure the correct sequencing of calls if the current user is relied upon by subsequent operations, especially in multi-threaded scenarios.

---

### SystemHeaderSegments
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static List<ChatSegment> SystemHeaderSegments(string time) =>
    [
        new($"{time} ", ChatColors.TimestampAttr),
        new(PadNick("--"), ChatColors.TimestampAttr),
        new(" │ ", ChatColors.RailAttr),
    ]
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `time` | `string` | — |

**Returns:** `List<ChatSegment>`


Constructs the header variant used for system/status lines in the chat UI. Given a `string time`, it returns a `List<ChatSegment>` containing three segments: the first renders the time with `ChatColors.TimestampAttr`, the second renders the padded nick placeholder via `PadNick("--")` using the same timestamp styling, and the third renders the rail separator as `" │ "` with `ChatColors.RailAttr`. This header is used to prefix system messages and provide a consistent visual cue for system status.

## Remarks
Centralizes header composition for system messages, enabling consistent styling and reduced duplication. By composing pre-styled segments instead of scattering formatting throughout callers, it makes maintenance easier and helps ensure system headers look the same across the chat surface.

---

### UnreadMarkerRule
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static ChatLine UnreadMarkerRule() =>
        new([new("── new messages ──", ChatColors.UnreadMarkerAttr)])
```

**Returns:** [`ChatLine`](ChatLine.cs.md)


This private helper constructs a [`ChatLine`](ChatLine.cs.md) that represents an unread-messages marker in the chat UI. It builds a single-token line containing the literal label `── new messages ──`, colored by `ChatColors.UnreadMarkerAttr`, and marks the line with `IsUnreadMarker = true` and `RuleLabel = `new messages``.

## Remarks
This factory encapsulates the visual convention for unread indicators, ensuring a consistent appearance across the UI without scattering literal tokens. By centralizing the construction, changes to the marker's label text or color attribute only need to be updated in one place. It also clearly communicates intent: lines produced by this helper are unread markers and should be treated accordingly by the rendering pipeline.

---

### WordWrap
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static List<string> WordWrap(string text, int maxCols)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `text` | `string` | — |
| `maxCols` | `int` | — |

**Returns:** `List<string>`


The `WordWrap` method transforms a block of text into a list of lines that fit within a specified maximum column width by wrapping at spaces. If `maxCols` is less than or equal to zero, wrapping is skipped and the original `text` is returned as a single line. The wrap logic uses `GetColumns()` to measure display width, ensuring truncation reflects actual rendered width rather than raw character count. This private helper centralizes line-breaking behavior for UI rendering (e.g., chat messages) so callers render consistently.

## Remarks
This private static helper encapsulates the core concern of rendering text within a fixed-width area. By delegating width calculation to `GetColumns()`, it remains resilient to character widths and potential emoji or wide characters, while keeping the wrapping policy consistent across the class. Centralizing this logic avoids ad-hoc wrapping scattered across call sites and makes future width-policy changes easier to propagate.

## Notes
- If a single word is longer than `maxCols`, the word is placed on its own line and may exceed the specified width; the function does not hyphenate or break long words.
- Wrapping relies on `StringSplitOptions.RemoveEmptyEntries`, so consecutive spaces are treated as a single separator and do not produce empty lines.
- Because the method is `private`, its reuse is restricted to its declaring type; if you need wrapping elsewhere, consider extracting it to a shared utility.

---

### ContentIndentCols
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** field

```csharp
public const int ContentIndentCols = 6 + NickColWidth + 3
```


ContentIndentCols is the left-padding width, in characters, for the chat message text. It is computed as `6 + NickColWidth + 3`, corresponding to the fixed time prefix `HH:mm `, the nickname column width `NickColWidth`, and the leading separator ` │ `. Use this constant whenever you render or measure the start column of the message body to ensure consistent alignment.

## Remarks
ContentIndentCols centralizes the left margin calculation for chat lines, ensuring message text starts at a single, predictable column regardless of nickname width. By deriving the indentation from `NickColWidth`, changes to nickname sizing propagate to the layout without scattering magic numbers. This constant is baked into compile-time calculations, so the layout remains stable across the codebase.

## Notes
- It is a compile-time constant; changing `NickColWidth` or `ContentIndentCols` requires a rebuild of the consuming code.

---

### NickColWidth
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** field

```csharp
public const int NickColWidth = 12
```


Defines the fixed width of the right-aligned nick column used in the chat message layout (WeeChat-style). The value `NickColWidth` reserves that many characters for the nick portion, ensuring consistent alignment of message text across lines.

---