# IChannelService.cs

> **Source:** `src/EchoHub.Core/Contracts/IChannelService.cs`

## Contents

- [IChannelService](#ichannelservice)
- [ChannelListItem](#channellistitem)

---

## IChannelService
> **File:** `src/EchoHub.Core/Contracts/IChannelService.cs`  
> **Kind:** interface

```csharp
public interface IChannelService
```


IChannelService defines a contract for asynchronous channel management in EchoHub. It exposes CRUD operations for channels, alongside topic queries, channel listings, and membership checks, enabling callers to manage chat channels without tying to a specific storage or UI implementation.

## Remarks
This interface serves as the architectural boundary for all channel-related functionality. It centralizes channel lifecycle actions (create, update, delete) and read-time queries (topic, list, by name) behind a single dependency, promoting testability and swapability of implementations. The ChannelOperationResult type standardizes outcomes with an IsSuccess flag and helper builders to create success or failure results, which callers should inspect rather than assuming success. The Exists flag in GetChannelTopicAsync and the nullable ChannelDto returned by GetChannelByNameAsync reflect real-world states (non-existence vs. empty data) that callers must handle.

## Notes
- All methods are asynchronous Task-returning; callers should await results and consider cancellation strategies if supported by the runtime.
- Authorization is not encoded in the interface; implementations must enforce access control according to callerUserId or creatorUserId semantics.

---

## ChannelListItem
> **File:** `src/EchoHub.Core/Contracts/IChannelService.cs`  
> **Kind:** record

```csharp
public record ChannelListItem(string Name, string? Topic, int OnlineCount)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Name` | `string` | — |
| `Topic` | `string?` | — |
| `OnlineCount` | `int` | — |


ChannelListItem is an immutable, small data object that represents a channel's essential metadata in a compact form: the channel's Name, an optional Topic, and the current OnlineCount. Use this symbol when you need to transport or display a lightweight channel summary (for example, in a channel list UI or API response) without querying full channel state.

## Remarks
ChannelListItem is declared as a positional record, which gives it value-based equality and makes instances immutable by default. This makes it safe to share across boundaries and to use as keys in collections. The nullable Topic communicates optional context; consumers should handle potential null values gracefully. With-expressions can produce modified copies without mutating the original.

## Notes
- The Topic field is nullable; a null topic indicates no topic is set for the channel.
- ChannelListItem uses value-based equality; two items with identical Name, Topic, and OnlineCount compare as equal.
- Because it is immutable, updates should be expressed via a with-expression or by constructing a new instance.

---