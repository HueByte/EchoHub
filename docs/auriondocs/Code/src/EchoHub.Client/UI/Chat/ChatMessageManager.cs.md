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
  - [NickColWidth](#nickcolwidth)
- [ContentIndentCols](#contentindentcols)

---

## ChatMessageManager
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** class

```csharp
public sealed class ChatMessageManager
```


Manages in-memory chat message storage, formatting and mutation for per-channel chat views. Use this when you need a single place to append formatted ChatLine objects, track per-channel unread counts and mention state, and notify the UI layer of any message-list changes via the MessagesChanged event.

## Remarks
ChatMessageManager is the authoritative owner of channel message lists and the policies around unread/mention tracking and visual markers. It centralizes: formatting incoming MessageDto values into ChatLine instances (including insertion of day-boundary/date rules and continuation indentation), per-channel unread counters and "new messages" anchors, mention detection for the configured CurrentUser, and a persisted LastReadIds map that an external orchestrator can seed or store. The MessagesChanged event is fired with the channel name after any mutation so the UI can refresh only the affected view.

## Notes
- Changing CurrentChannel has side effects: leaving a channel consumes its "new messages" marker and marks messages visible up to that point as read (this mirrors irssi-like behavior). Subscribe to MessagesChanged to react to those updates.
- SetCurrentUser and SetChatWidth should be populated by the host before relying on mention highlighting or line wrapping/continuation; the manager uses the current user and the configured width when formatting lines.
- LastReadIds is exposed as a read-only dictionary but is expected to be persisted/seeded externally (the manager exposes per-channel last-read message IDs so the orchestrator can restore unread/mention state across restarts).

---

### CurrentChannel
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public string CurrentChannel
```


CurrentChannel exposes the actively selected chat channel and drives the UI-facing unread and mention-detection logic. When you switch channels, the setter clears the unread marker and marks the previous channel as read before updating the active channel reference, ensuring the old channel is considered read and the new channel becomes the current focus.

## Remarks
This property centralizes the channel-switch lifecycle, ensuring consistent unread-state handling and mention detection as users move between channels. By containing the transition effects (clearing unread markers and marking read) within the setter, it reduces the risk of scattered state mutations elsewhere in the codebase and clarifies the responsibilities of channel state management.

## Notes
- Switching channels clears unread markers for the old channel and marks it as read; the new channel's unread state remains unchanged until you leave it, which can be surprising if you expect an immediate clear on entry.
- Setting CurrentChannel to the same value is a no-op; no side effects run in that case.
- If _currentChannel is null (e.g., before any channel is selected), RemoveUnreadMarker(null) and MarkRead(null) will be invoked; depending on the implementations of those methods, this may be a no-op or require null handling.


---

### CurrentUser
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public string CurrentUser => _currentUser
```


Exposes the name of the user currently associated with the chat message manager as a read-only string. It simply returns the value of the private backing field _currentUser, providing a lightweight way to display or log the current user's identity without altering state. Use this property when you need to show who is sending a message, tag messages in the UI, or include the user in diagnostics; since it is backed by a field, there is no additional computation beyond a simple getter.

## Remarks
CurrentUser acts as a thin surface over the internal state representing the active user. By exposing it as a property, the class avoids leaking the backing field while still providing an ergonomic, strongly-typed access point for consumer code. This is useful for displaying the current user in the chat header or tagging messages; changes to _currentUser will be immediately visible through CurrentUser because the getter reads the field value at access time. Keep in mind that if _currentUser is null, CurrentUser will be null as well, so downstream code should handle nulls accordingly.

## Notes
- No public setter is provided; updates must occur by updating the backing field _currentUser within the class.
- The value can be null if _currentUser hasn't been assigned yet.
- There is no explicit thread-safety guarantee for this getter; if _currentUser may be updated from other threads, callers should ensure visibility.

---

### LastReadIds
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public IReadOnlyDictionary<string, Guid> LastReadIds => _lastRead
```


LastReadIds exposes, for each channel, the ID of the last message the user has read, as maintained by the orchestrator and persisted across connections. Use it to determine which messages are new and to seed unread/mention state when the client reconnects.

## Remarks
Conceptually, this property decouples read-tracking from the UI, centralizing per-channel state in a durable dictionary that survives restarts. It relies on the orchestrator to persist history so unread markers and mentions align with the user's activity after reconnecting.

## Example
```csharp
if (chat.LastReadIds.TryGetValue(channelId, out var lastReadId))
{
    // lastReadId is the ID of the last message the user has read in this channel
    // Use lastReadId to identify messages that are newer and should be highlighted as unread.
}
```

## Notes
- The dictionary is exposed as a read-only view; internal logic updates the underlying data. Do not attempt to mutate the collection from consumer code.
- If a channel isn't present in the dictionary, TryGetValue will return false; treat that as 'no stored last read' and consider all messages as potentially unread.

---

### MentionChannels
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** property

```csharp
public IReadOnlySet<string> MentionChannels => _mentionChannels
```


Exposes the set of chat channels that currently have unread mentions of the current user. The value is returned as an `IReadOnlySet<string>` and is backed by the internal _mentionChannels field. Callers typically rely on MentionChannels to drive UI indicators (such as per-channel badges or highlights) showing which channels require the user's attention. The unread-mention state is cleared when ClearUnread is invoked.

## Remarks

Represents a read-only view into the manager's internal tracking of unread mentions. By returning an IReadOnlySet, it prevents accidental mutation from consumer code while still letting the UI reflect up-to-date state. Updates to the set occur through internal logic; ClearUnread resets the collection to an empty state, removing all current unread mentions.

## Notes

- This property is a live view of internal state; external code cannot mutate it directly. If the internal collection is updated, the new contents will be visible on subsequent enumeration.

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


ActionHeaderSegments builds the header used when rendering /me action messages in the chat UI. It returns a `List<ChatSegment>` consisting of three parts: a timestamp segment created from the provided time string, a star-prefixed nickname segment produced by PadNick("*"), and a small rail separator. This header is meant to precede the actual action content, producing a visual like a timestamp, a leading "*" in the nick column, and a divider before the action text. The method relies on ChatColors.TimestampAttr for the time and star segments, and RailAttr for the separator, ensuring the header follows the established chat theming.

## Remarks
ActionHeaderSegments encapsulates the specific visuals for /me action headers, ensuring all such headers are rendered consistently across the UI. By composing the header from three standardized ChatSegment pieces and delegating nickname rendering to PadNick, it centralizes styling concerns and reduces duplication in the rendering path. The dependency on ChatColors and PadNick ties this header closely to the existing color theming and nickname formatting used elsewhere in the chat system.

## Notes
- This method is private and static, serving as an internal helper for header construction during message rendering. External code cannot call it directly.
- The time parameter must be a pre-formatted display string; the method does not perform formatting or validation of the time value.
- If the visual design for action headers changes (e.g., a different marker or separator), this single method should be updated to preserve consistency across all /me action headers.

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


Formats and stores a received message by first formatting it into display lines and then persisting those lines under the message’s ChannelName. It enforces a day-boundary rule by inserting a DateRule when the local date of the new message differs from the previous one, and it updates the latest message ID and, if applicable, the current-read pointer for the active channel. For inactive channels, it adds a one-time New Messages marker anchored to this message so a history reload can re-place it, and it increments the per-channel unread count while noting any mentions for later highlighting. Finally, it raises the MessagesChanged event to notify observers that the channel’s messages have updated.

## Remarks

This method centralizes all per-message mutations for the chat UI, ensuring consistent channel-state updates when new data arrives. It relies on the message.SentAt timestamp (converted to local time) to decide day boundaries and uses internal dictionaries (e.g., _channelMessages, _channelUnread, _markedChannels) to keep unread counts, markers, and last-read state in sync across channels. By anchoring an unread marker to the first unread message in inactive channels, it enables reliable re-placement on history reloads, while emitting MessagesChanged keeps the UI responsive to changes.

## Notes

- The method assumes message.ChannelName is non-null; otherwise an exception could be thrown when using dictionary keys.
- It uses ToLocalTime; time zone implications depend on the runtime environment and MessageDto's SentAt value.
- The internal state mutations are not protected by synchronization; callers should ensure serial access or add locking if called from multiple threads.

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


Adds a status change message to a channel with colored styling. It captures a timestamp via FormatTime(DateTimeOffset.Now), constructs header segments with SystemHeaderSegments, appends a segment describing the status change in the username’s color via ChatColors.SystemAttr, ensures the channel entry exists in the internal _channelMessages store, appends a new ChatLine built from the assembled segments (including a ContinuationPrefixSegments from RailPrefix), and, if the updated channel is the currently viewed one, fires the MessagesChanged event to refresh the UI.

## Remarks

This method centralizes the presentation of user status updates as timestamped, system-colored messages within a per-channel chat history. By encapsulating the formatting (time header, system-colored status text) and the mutation of the channel’s message list, it promotes consistent visual styling across channels and keeps UI updates synchronized with data changes.

## Notes

- Be mindful of thread-safety: _channelMessages is mutated without explicit synchronization, so concurrent calls could race in a multi-threaded context.
- Time formatting depends on the system clock; for deterministic tests, consider controlling FormatTime/DateTimeOffset.Now or abstracting time retrieval.


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


Adds a system/informational message to a channel with colored styling. When invoked, it ensures the channel's message list exists, formats the current time, splits multi-line text so the first line appears in the header and subsequent lines are added as separate lines with a rail-style continuation prefix; if the target channel is the currently visible one, it triggers a UI refresh via MessagesChanged.

## Remarks
System messages are rendered with a header line that includes a timestamp, followed by body segments styled with SystemAttr. This method centralizes the formatting of such messages, so callers don't need to assemble headers or manage continuation prefixes themselves. It relies on ChatLine, ChatColors, and RailPrefix to produce a consistent visual treatment across channels.

## Notes
- Potential lack of thread-safety if called from multiple threads; the internal _channelMessages dictionary is mutated without locking.
- Only the UI refresh is raised when posting to the currently active channel (otherwise the message is updated silently).
- Lines after the first are treated as separate ChatLine entries with their own continuation prefix; blank lines are ignored.

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


The AttachmentActionLine method constructs a ChatLine that renders as a clickable attachment action within the chat list. It prefixes the display text with a standardized rail segment, colors the text using the provided color attribute, and attaches the underlying attachment metadata (URL, file name, and kind) so the UI can route activation to the correct behavior (play audio, download, or save the original image).

## Remarks
AttachmentActionLine centralizes how attachment-based actions are presented in the chat. By bundling the styling prefix, action text, and attachment metadata in a single factory, it keeps rendering and activation logic cohesive and easier to maintain. The method relies on RailPrefix to ensure consistent visual grouping and populates ChatLine's attachment properties so downstream UI and activation code can locate the URL, file name, and kind without reassembling them.

## Notes
- The method is private and static, so it is only callable within its containing type and from a known, fixed entry point.
- It sets ContinuationPrefixSegments to RailPrefix(), ensuring continuation lines align with the same action prefix; altering RailPrefix behavior might affect line wrapping or click target consistency.

---

### ClearAll
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
public void ClearAll()
```

**Returns:** `void`


Clears all internal message state maintained by the chat message manager. This method empties all per-channel data stores and resets the current context, providing a clean slate when disconnecting or reinitializing the chat UI. It is used during disconnect sequences to prevent stale data from persisting across sessions.

## Remarks
By encapsulating reset logic here, the class guarantees a consistent baseline state after disconnection. It reduces the risk of partially cleared state being left behind when disconnects occur in various code paths, and it centralizes lifecycle management for chat state.

## Notes
- Not inherently thread-safe: callers should ensure synchronization if the ChatMessageManager is accessed concurrently during disconnect.
- After invocation, there is no active channel or user until reinitialization occurs; _currentChannel and _currentUser are set to empty strings.
- This method only clears in-memory state; any external resources or persisted data are unaffected.

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


Clears all messages for a specific channel from the client-side chat state. If the channel exists in the internal message map, it empties that channel's message list and removes the per-channel metadata: the last date, marked channels, and marker anchor for that channel. If the channel being cleared is currently active, it raises the MessagesChanged event to notify the UI to refresh for that channel. If the channel does not exist in the map, this method is a no-op. The operation affects only in-memory state and does not touch persistent storage or other channels.

## Remarks
This method centralizes the cleanup of per-channel UI state, ensuring that clearing a channel leaves the rest of the UI in a consistent state. By clearing the per-channel dictionaries and lists alongside the messages, it prevents stale metadata from lingering after a channel's history is purged. The MessagesChanged event invocation for the current channel decouples UI refresh logic from the data update, allowing subscribers to re-render the channel view as needed.

## Notes
- No persistence: only in-memory state is cleared.
- Safe-to-call-no-op: if the channel is missing from _channelMessages, the method returns without side effects.
- Assumes non-null per-channel message list: a null collection would cause a NullReferenceException on Clear, so callers should ensure the data is initialized.
- If multiple components listen for MessagesChanged, the event will fire only when the cleared channel is the current channel; other channels won't trigger an automatic refresh from this call.

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


Clears the unread state for the specified channel by resetting its unread count, removing mention highlights, and marking the channel as read. This is typically invoked when the user opens or explicitly reads a channel, ensuring the UI and internal state reflect that there are no remaining unread messages for that channel.

## Remarks

Clears three facets of unread state in a single operation: it updates the internal unread counter for the channel, removes the channel from the active mention-tracking collection, and delegates to MarkRead to apply the persisted read-state. This centralizes the read-clearing behavior so the rest of the UI can rely on a single, consistent method rather than duplicating logic at multiple call sites. The exact effects depend on the implementations of _channelUnread, _mentionChannels, and MarkRead; for example, if the channel is not yet present, the first assignment will create an entry with 0 unread, and Remove will be a no-op if the channel is not in _mentionChannels.

## Example

```csharp
// Assuming 'manager' is an instance of ChatMessageManager
manager.ClearUnread("general");
```

## Notes
- If ClearUnread is invoked for a channel that did not previously exist in the internal structures, the first line will create or overwrite an entry with a value of 0.
- The behavior of MarkRead is relied upon to finalize the read-state side effects; if MarkRead triggers additional side effects (e.g., persistence or events), those will occur as part of this call.


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


DateRule constructs a stylized date separator line for a given date within the chat UI. It derives a label from DateRuleLabel(date) and returns a ChatLine containing a single segment that renders as "── {label} ──" using ChatColors.DateRuleAttr. The returned ChatLine also has its RuleLabel set to the label and its RuleAttr set to the same color attribute. Use this helper whenever you need a consistent, date-bounded visual divider between messages rather than composing lines manually.

## Remarks
By encapsulating the creation of the date rule, DateRule provides a single point of change for how date separators look and behave. It coordinates the label generation with the chat coloring to ensure separators match other UI rule lines and follow the project's styling conventions for date-related cues. This abstraction sits alongside ChatLine and ChatColors, reinforcing a uniform approach to rendering non-message chrome in the chat.

## Notes
- Changes to DateRuleLabel or the decorative glyphs will affect every date separator; tests that assert exact separator text should be updated if the label generation changes.
- DateRule is private static, so its reuse is limited to the containing class; if external customization is needed, consider elevating the helper to a more accessible API or adjusting the color attribute usage in ChatColors.DateRuleAttr.

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


Converts a DateTime to a short, human-friendly label using the pattern 'ddd, MMM d yyyy'. This helper returns a string such as 'Tue, Jul 23 2024' and is used by the chat UI to display date labels consistently instead of formatting dates ad-hoc at each call site.

## Remarks
This method centralizes the exact format used across the chat components, ensuring consistent date labels. It is declared internal and static, indicating it's intended for internal use within the ChatMessageManager's UI rendering flow rather than as part of the public API.

## Notes
- This formatting respects the current culture; for stable, culture-independent output, supply a culture-invariant format (e.g., date.ToString("ddd, MMM d yyyy", CultureInfo.InvariantCulture)) and add a using System.Globalization.

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


Formats an embed into a vertical sequence of chat lines with a left rail and colored text, suitable for rendering inside the chat UI. It accepts an EmbedDto and the current chat width, computes the available text area, and assembles lines that begin with a fixed border segment colored by the embed border color, followed by the actual text colored per section (title or description). If present, SiteName is emitted first using the border color; Title is wrapped to the computed text width and emitted with EmbedTitleAttr; Description is wrapped similarly with EmbedDescAttr. The method returns a `List<ChatLine>` that can be rendered as part of a larger message.

## Remarks
FormatEmbed centralizes the formatting decisions for embeds in the chat UI, ensuring a consistent look by deriving the border color from the embed ThemeColor or falling back to a default border color, and by applying distinct styling to the title and description. It relies on shared utilities (WordWrap and RailPrefix) to wrap text to the computed width and to align lines with a left rail, respectively. Because this is a private helper, its usage is confined to the containing class, which helps encapsulate embed rendering and prevents drift from the surrounding chat presentation.

## Notes
- If embed.ThemeColor is an invalid hex string, the border color falls back to ChatColors.EmbedBorderAttr.
- The text width is computed from the provided chatWidth and is clamped to a minimum of 20 columns; very small chat widths may lead to tighter wrapping and more lines.


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


Formats a nullable file size into a concise, human-readable string. If the input is null or zero, it returns a single question mark to indicate an unknown or unavailable size. For any non-null value, it chooses the most appropriate unit among bytes (B), kilobytes (KB), megabytes (MB), and gigabytes (GB) and formats the result with a single decimal place for all units except bytes. The thresholds use binary units (1024 multipliers), producing strings like '512 B', '1.5 KB', '3.2 MB', or '1.2 GB'.

## Remarks
Consolidates the formatting logic so callers don’t duplicate range checks or string formatting, ensuring consistent display across the UI. The function intentionally treats null or zero as unknown ("?") rather than returning a numeric zero, which is useful when the size may not be known at the point of rendering.

## Notes
- Null or zero input yields "?" per the early guard.
- Uses binary thresholds: 1024 B for KB, 1024^2 B for MB, and 1024^3 B for GB.
- Non-byte units are shown with one decimal place (e.g., 1.5 KB, 3.2 MB, 1.2 GB); boundary values exactly at 1024, 1024^2, etc., switch units accordingly (e.g., 1024 B becomes 1.0 KB).


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


Formats a MessageDto into a list of ChatLine entries suitable for rendering in the chat UI. It resolves the timestamp via FormatTime, chooses a representative display name (SenderDisplayName when available, otherwise SenderUsername), and derives a nickname color using HexColorHelper or NickColorHelper. The method then builds a header line or a summarized header for attachments, handles reply quotes by inserting a preceding quote line, and supports CTCP-style /me actions by rendering a header that shows the action followed by additional lines. When content exists, the content is emoji-normalized and split into lines with mention highlighting; when there is no text, a compact header summarizes attachments. Each line receives a RailPrefix so subsequent content lines and per-attachment blocks align with the nick rail, and attachments produce their own blocks hanging off that rail (e.g., image previews).

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


Formats a DateTimeOffset timestamp into the user's local time and renders it as a compact 24-hour time string (HH:mm). By converting to local time before formatting, the method ensures that times align with the local calendar day rules, so messages near midnight are associated with the correct day in the UI. This helper is used wherever a concise, time-only indicator is needed for chat messages (for example, timestamps next to messages).

## Remarks
By centralizing locale-aware time formatting in a private helper, the code avoids duplicating ToLocalTime calls across the UI and guarantees a consistent display of chat timestamps. It is designed for presentation concerns rather than time arithmetic.

## Notes
- Relies on the system's local time zone via ToLocalTime; DST and locale settings affect the result.
- Only the time portion is produced (HH:mm); date and potential day-boundaries are resolved at a higher level in the UI.
- As a private method, its usage is confined to the containing class; if cross-cutting formatting is needed, consider extracting to a shared utility.

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


Formats a chronological batch of messages into a list of ChatLine objects, inserting a date rule before the first message and whenever the day changes. The method converts each message's SentAt to local time to determine day boundaries, delegates per-message formatting to FormatMessage, and returns the assembled lines while outputting the last processed local date via lastDate.

## Remarks
Day separators help users scan conversations by calendar date, providing clear visual breaks between days. By isolating the boundary logic in this function and delegating rendering to DateRule and FormatMessage, the code remains reusable and consistent across different chat views.

## Example
```csharp
// Example: format a batch of messages into chat lines with day separators
DateTime? lastDate;
List<ChatLine> lines = FormatWithDateRules(batchMessages, out lastDate);
```

## Notes
- No null-check on the input list; passing null for messages will throw.
- lastDate is null if there are no messages; callers should account for a possible null value.

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


Retrieves the current list of ChatLine entries for a specific channel by name from the internal message store. It returns the existing `List<ChatLine>` for the channel, or null if the channel has no messages. This is a lightweight accessor around the underlying storage and does not create a new list or clone data.

## Remarks
This method exposes the internal `List<ChatLine>` instance associated with the given channel. Callers should be aware that mutations to the returned list (adding/removing items) will affect the stored messages for that channel. If an immutable snapshot is required, consider copying the list before enumeration or modification. The method hides the details of how messages are stored, providing a single entry point that can be swapped out without changing call sites.

## Example
```csharp
var messages = chatMessageManager.GetMessages("general");
if (messages != null)
{
    Console.WriteLine($"General channel has {messages.Count} messages.");
}
```

## Notes
- Returning null indicates the channel has no messages or does not exist in the store; always null-check before accessing properties like Count.
- The returned `List<ChatLine>` is not cloned; modifications to it affect the internal store unless an external copy is created.

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


Returns the unread message count for the specified channel by querying the internal _channelUnread mapping. If the channel has no recorded count, it returns 0. This read-only helper encapsulates access to the underlying data and is typically used by the UI to display per-channel unread badges without exposing the dictionary directly.

## Remarks

Acts as a minimal abstraction over the unread-tracking store, hiding direct dictionary access and ensuring a zero default when a channel has no entry. The caller should understand that the value comes from the shared _channelUnread structure, so updates to unread counts elsewhere will be visible on subsequent calls; if the underlying storage is not thread-safe, callers must ensure proper synchronization.

## Notes

- Passing null as channelName will throw an ArgumentNullException from TryGetValue.

---

### GetUnreadCounts
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
internal Dictionary<string, int> GetUnreadCounts() => _channelUnread
```

**Returns:** `Dictionary<string, int>`


Returns the internal per-channel unread counts as a mutable dictionary backed by the _channelUnread field. Use this accessor when you need to read or react to per-channel unread tallies without recomputing them, noting that the returned dictionary is the live internal collection.

## Remarks
This accessor is intended as a lightweight bridge between the internal unread-count store and UI or coordination code that needs to display or react to those counts. It avoids copying data for performance and maintains synchronization with internal updates. However, because it returns the actual dictionary, external callers can mutate the collection, potentially breaking invariants or introducing subtle bugs. If you require a read-only view, consider returning `IReadOnlyDictionary<string,int>` or a defensive copy, and adjust the signature accordingly.

## Notes
- Mutability risk: Changes to the returned dictionary affect internal state.
- Thread-safety: Concurrent updates to _channelUnread may race with external mutations; consider synchronization.
- Initialization: Ensure _channelUnread is initialized before first access to avoid NullReferenceException.

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


HeaderSegments constructs the three leading pieces of a message header line: a dim timestamp, the (optionally) colored, padded nickname, and a fixed rail separator. It returns these as a `List<ChatSegment>` so the caller can render the header independently from the message body. The first segment renders the provided time string with the Timestamp attribute, the second applies a padded nickname using the supplied nickColor, and the third renders a static rail string with the Rail attribute. This centralized assembly ensures consistent header formatting across messages and keeps layout/color decisions isolated from the rest of the rendering logic. The header segments precede the actual message text, which begins after ContentIndentCols.

## Remarks
By encapsulating header composition, this method enforces consistent alignment and styling for all message headers. It isolates colorization and spacing concerns from the message content, making it easier to adjust the header's appearance in one place without touching rendering logic elsewhere.

## Example
```csharp
// Example usage within the same class context
var segments = HeaderSegments("12:34", "Alice", ChatColors.SystemAttr);
```

## Notes
- The method is private, so it cannot be called from outside its declaring type. If header construction is needed elsewhere, provide a public wrapper or move the logic to a shared utility.
- The nick color parameter is nullable, allowing callers to omit explicit coloring when desired; the rendering path should handle a null color accordingly.

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


Builds the action line displayed under an image preview, showing [open] and [↓ save original] as clickable actions and appending the file name with its size. Each bracketed label becomes an AttachmentActionSpan so the UI can map clicks to the corresponding action, while Enter triggers the default (open).

## Remarks
This symbol centralizes the rendering of image-related actions in chat messages, ensuring consistent spacing and interactivity across messages. It constructs the action regions by measuring segment widths from RailPrefix() and updating a running column index; the resulting ChatLine carries ActionSpans and attachment metadata for downstream rendering.

## Notes
- The clickable targets cover only the bracketed portions; the trailing file name and size text is not interactive.
- If you change the action labels or formatting, adjust the width calculation logic accordingly, since spans are derived from the label text width.

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


Loads historical messages into a channel, replacing any existing messages. When lastReadId is supplied (persisted from a previous session), the messages after that identifier are seeded into the unread count, @mention highlighting, and the `new messages` marker, ensuring activity from when the user was offline is surfaced when history is loaded.

## Remarks

This method is the central entry point for bringing a channel's history into the UI. It formats incoming messages, updates per-channel caches (such as the latest message id, the list of messages, and the last date), and raises the MessagesChanged event to refresh the view. A key concern it addresses is surfacing unread backlog: if the channel is not currently marked, and a lastReadId is provided, the code seeds unread state from the provided history so the user sees what they missed. If the channel is marked, the code attempts to preserve the unread marker by inserting UnreadMarkerRule() at a known anchor position; if the anchor cannot be located within the fetched batch, the marker is dropped and the anchor tracking for that channel is cleared.

The method keeps the display coherent across history loads by either re-anchoring the marker or seeding unread state, and it updates the channel's last date when available. This coordination helps maintain a stable user experience as history is navigated.

## Notes

- Marker anchor handling may drop the unread marker if the anchor falls outside the fetched history window; in that case the channel's marker tracking is cleared.
- When lastReadId is provided and there is history, unread state is seeded from history only if the channel is not currently marked with an anchor.
- There are internal caches being updated (_channelMessages, _channelNewestId, _channelLastDate, etc.) and a UI notification is raised via MessagesChanged; callers should ensure thread-safety or call this from a suitable thread to avoid races.

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


Updates the internal read-tracking state for a chat channel by setting the last-read marker to the channel's newest known message ID, if available. It is a small internal helper used when the user has effectively read up to the latest message in the specified channel.

## Remarks
This method serves as a concise read-tracking primitive within ChatMessageManager. It relies on two internal structures—_channelNewestId (the newest known message ID per channel) and _lastRead (the last-read position per channel)—to advance the read marker without exposing the internal collections to external callers. By performing a safe fetch and updating only when a newest ID exists, it provides a robust, side-effect-limited mechanism for synchronizing UI read state with the channel's latest activity.

## Notes
- No-op if channelName is null or empty, or if there is no entry for the channel in _channelNewestId; in these cases, no exception is thrown and the state remains unchanged.


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


Right-aligns a nickname into the fixed nickname column, truncating nicknames that exceed the available width with an ellipsis, while respecting grapheme boundaries and display column widths. This ensures consistent, visually aligned nicknames in the chat UI regardless of complex characters.

## Remarks
Right-aligns a nickname within a fixed-width column and centralizes the logic for width-aware truncation. By counting display columns per grapheme and never splitting a grapheme cluster, it preserves user-visible completeness (including emoji and combining characters) while maintaining a stable layout. The ellipsis is appended when truncation is necessary, and the result is padded on the left to exactly fill NickColWidth columns.

## Notes
- The truncation reserves one column for the ellipsis (NickColWidth - 1) to preserve the final width.
- Each grapheme's display width is obtained via g.GetColumns(), with a minimum of 1 column to avoid stalls on zero-width elements.
- NickColWidth should be a positive, reasonable value to ensure the UI remains legible; extreme values may produce unexpected padding.

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


PrependHistory prepends older messages to the front of a channel’s in-memory buffer, skipping any that are already present. It filters olderMessages to those not already in the buffer by MessageId, formats the new messages into display lines (respecting the channel’s date-rule conventions), and inserts them at the beginning of the buffer. If the channel isn’t tracked, or if no new lines are produced, the method returns without side effects. When the update targets the currently displayed channel, it raises the HistoryPrepended event to signal the UI to reflect the new history.

## Remarks
Conceptually, this method isolates the concerns of history retrieval, formatting, and UI notification from higher-level chat flow. It relies on MessageId to detect duplicates and on date-rule formatting to ensure the inserted lines align with existing visual rules. By conditionally removing a redundant leading date line when the batch ends on the same day as the current first line, it avoids duplicating date indicators at the top of the buffer.

## Notes
- The method mutates the in-memory channel buffer in place and may affect the UI; callers should be aware of in-memory state changes.
- Deduplication uses MessageId; messages without an Id will be treated as new and could be inserted if not already present.
- HistoryPrepended is raised only when the target channel is the currently active channel (_currentChannel); otherwise, no event is fired.

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


RailPrefix produces the indentation prefix used for lines that continue or attach to a chat message. It builds two ChatSegment entries: a leading blank-space block sized to accommodate the nickname column plus padding, and a rail segment rendering the vertical continuation rail. A fresh mutable `List<ChatSegment>` is returned on every call so callers can compose per-line prefixes without mutating shared state.

## Remarks
RailPrefix encapsulates the alignment rule used for multi-line messages, ensuring that continuation lines align consistently with the main message regardless of nickname width or color settings. The first segment accounts for the nickname column width (NickColWidth) plus a small padding, while the second segment draws the rail using ChatColors.RailAttr, producing a visually distinct vertical guide. Returning a new list on each call avoids cross-call mutations and keeps prefix construction side-effect free.

## Notes
- Changing NickColWidth or RailAttr will affect the resulting prefix, so coordinate styling changes to avoid misalignment.
- The method returns a new `List<ChatSegment>` that callers are free to mutate; it does not mutate any shared state.

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


Removes all lines associated with a specific message ID from the client's in-memory per-channel message collection. It locates the list for the given channelName, eliminates any entries whose MessageId matches the provided messageId, and, if the updated channel is the current one, raises the MessagesChanged event to trigger a UI refresh. This method is useful when you need to purge a message from the local view (for example after a retraction or client-side filtering) without affecting server-side state.

## Remarks
By centralizing the removal logic, this symbol ensures consistent mutation of the per-channel message lists and a single notification point for UI updates. The operation is scoped to a single channel, and the UI will only refresh when the target channel is currently active. Because the method operates purely on the client-side in-memory structure, there is no server communication performed by this call.

## Notes
- Not thread-safe as written; ensure marshaling to UI thread or proper synchronization when accessing _channelMessages or the channel's message list.
- Assumes MessageId uniquely identifies a line; if duplicates exist, all matching lines are removed.


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


Removes the unread marker for a given chat channel by validating the input, clearing the channel from the marked set, detaching its UI marker anchor, and purging any unread-marker flags from the channel’s messages.

## Remarks
As a private helper, it centralizes the unread-marker lifecycle in ChatMessageManager, coordinating _markedChannels, _markerAnchor, and _channelMessages to keep UI state and data in sync. The early return guards prevent unnecessary work when the channel is invalid or already cleared. The removal of IsUnreadMarker flags happens only after the channel is removed from the marked set, ensuring a consistent, single source of truth for whether a channel shows an unread indicator.

## Notes
- This method is private; external callers should not rely on its behavior. 
- There is no synchronization visible in the snippet, so concurrent invocations may require external synchronization. 
- If there are unread indicators outside the IsUnreadMarker flags, they will not be cleared by this method.

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


Constructs a compact, rail-prefixed quote line for an incoming reply. It carries the original message id (JumpToMessageId) so selecting the quote navigates to the source, and it truncates the displayed snippet to fit the UI width. The snippet is sanitized by replacing newline characters with spaces, optionally converted to an action-style prefix if a known action is detected, and transformed with emoji glyph replacement. The result is a ChatLine composed of three segments: a rail prefix, the sender's username (colored), and the snippet (system-colored). The line also exposes JumpToMessageId for navigation and ContinuationPrefixSegments to align any continued lines of the rail.

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


Reconstructs unread state from a persisted last-read message id for a channel by inserting an unread marker before the first unread message in the current fetch window and, for inactive channels, seeding the unread count and @mention highlight. If the last-read id is no longer present in the fetched window, the entire window is treated as unread.

## Remarks
This symbol centralizes how persisted read positions are translated into the UI's unread indicators. It updates internal trackers (_markedChannels, _markerAnchor, _channelUnread, and _mentionChannels) and mutates the formatted message list to place the visual cue that new messages are available. Because it only applies the badge/mention behavior to background channels, the active channel remains visually unaffected beyond the standard read state.

## Notes
- If the computed anchor line cannot be found in the current formatted list, no marker is inserted and the method returns.
- When lastReadId is not found in messages, firstUnread becomes 0, so the marker targets the very first message in the window.
- Mentions are evaluated only for non-active channels; active channels do not receive mention highlights from this method.

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


Sets the internal chat width used by the chat rendering logic. This method is a concise mutator that assigns the provided width to the private _chatWidth field. Use it when you need to programmatically adjust the chat area width, such as in response to layout changes or user actions that resize the chat panel.

## Remarks

Centralizes width mutations behind a single API, preserving encapsulation of layout state. It also paves the way for future side effects (for example, triggering a layout refresh or validating the value) without changing call sites. Keeping this logic in one place reduces duplication and makes behavior easier to evolve.

## Notes

- No validation on the input width; callers should ensure the value is non-negative and within reasonable bounds to avoid render glitches.

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


Sets the internal _currentUser field to the provided username, updating the chat subsystem's notion of who is the current user. This method should be used whenever the active user changes (for example, after a user logs in or switches accounts) so that subsequent messages can be attributed to the correct user in the UI.

## Remarks
Centralizes mutation of the current user state within ChatMessageManager, making it easier to add side effects (such as updating UI elements, tagging messages, or enforcing user-specific behavior) without changing call sites. By routing changes through SetCurrentUser, the class can evolve to perform validation, trigger events, or refresh displays in a single place.

## Notes
- No input validation or normalization is performed; the value is assigned directly to _currentUser. Passes such as null or empty strings may lead to an invalid or inconsistent state unless the caller ensures proper validation.

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


This private helper constructs the header segments for a system/status line. When given a formatted time string, it returns a three-segment header (ChatSegment list) that renders: the time, a padded system nickname placeholder, and a leading rail separator, all styled with the project's chat color attributes. The header segments correspond to the three elements in the returned list: the time string followed by a space colored with TimestampAttr, the PadNick(\"--\") value colored with TimestampAttr, and the literal rail \" │ \" colored with RailAttr. This utility is used by the chat header rendering logic to produce a consistent appearance for system messages.

## Remarks
This private method encapsulates the three-part system header used for status lines, anchoring time, nickname placeholder, and the rail separator in one place. It relies on PadNick for the nickname placeholder width and on ChatColors attributes to keep the look aligned with the rest of the chat chrome.

## Notes
- The time argument should already be formatted for display; the method does not parse or reformat it.
- The header relies on PadNick to produce a fixed-width nickname; changes to PadNick's output or width could affect alignment.

---

### UnreadMarkerRule
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** method

```csharp
private static ChatLine UnreadMarkerRule() =>
        new([new("── new messages ──", ChatColors.UnreadMarkerAttr)])
```

**Returns:** [`ChatLine`](ChatLine.cs.md)


Creates a ChatLine that renders the '── new messages ──' unread marker using the UnreadMarker color attribute. This private helper is used when building the chat line sequence to visually indicate that there are unread messages in the conversation.

## Remarks
To centralize the styling and labeling of the unread marker, this helper bundles the label ('new messages'), the color attribute (ChatColors.UnreadMarkerAttr), and the unread-marker flag (IsUnreadMarker = true). It keeps the construction logic in one place so changes to the marker's text or color propagate consistently across callers. The private visibility signals that this is an internal construction detail of the chat rendering pipeline.

## Notes
- This symbol is private; it cannot be called from outside its containing class. If you need to render unread markers elsewhere, consider exposing a public API or refactoring the helper into a shared utility.
- The returned ChatLine is explicitly marked as an unread marker; consumers should treat it as a UI cue rather than a regular chat message.

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


WordWrap is a private utility that converts a single string into a list of lines whose display width does not exceed a specified maxCols. It's designed for UI scenarios (for example, chat messages) where wrapping must be deterministic and centralized. If maxCols <= 0, the method returns a single-element list containing the original text.

Otherwise, it splits the input on spaces (collapsing multiple spaces) and greedily builds lines by appending words until adding the next word would exceed maxCols as measured by GetColumns. When a word would overflow the current line, the line is committed and a new one starts with that word. The final line is added after processing all words. The resulting lines use single spaces between words.

Note that a single word longer than maxCols will be placed on its own line and may exceed the requested width.

---

### NickColWidth
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** field

```csharp
public const int NickColWidth = 12
```


NickColWidth defines the fixed width of the nickname column in the chat UI, reserving 12 characters on the right to align nicknames in a WeeChat-style layout. It is used by the chat rendering logic in ChatMessageManager to keep nickname alignment consistent across messages.

## Remarks
Centralizes the presentation detail of the nickname column, avoiding scattered magic numbers across rendering code. By exposing this as a single public constant, it’s straightforward to tweak the overall alignment of the chat UI while keeping the rest of the layout logic unchanged. It also communicates intent clearly to future contributors who are adjusting how usernames appear in chat rows.

## Notes
- As a public compile-time constant, changing NickColWidth requires recompiling dependents to pick up the new value.
- Prefer referencing NickColWidth in formatting/layout code rather than using hard-coded numeric literals to maintain consistent alignment.

---

## ContentIndentCols
> **File:** `src/EchoHub.Client/UI/Chat/ChatMessageManager.cs`  
> **Kind:** field

```csharp
public const int ContentIndentCols = 6 + NickColWidth + 3
```


ContentIndentCols represents the total number of character columns that precede the actual message text in a chat line. It is computed as 6 (the length of the "HH:mm " timestamp prefix) plus NickColWidth (the width of the nickname column) plus 3 (the " │ " separator). Use ContentIndentCols when you need to align or wrap the message body so that it starts at a consistent column after the header.

## Remarks
This abstraction ties the content start position to the header region, ensuring consistent alignment across messages even if nickname width or the time prefix changes. By centralizing the indentation budget behind a single public constant, rendering code avoids scattered magic numbers and remains coherent when layout assumptions evolve.

## Example
```csharp
// Example: build an indented content line for a chat message
string line = new string(' ', ContentIndentCols) + body;
```

## Notes
- The constant is a compile-time value (public const int). If you need dynamic indentation per message or per theme, compute it at runtime instead of using ContentIndentCols.

---