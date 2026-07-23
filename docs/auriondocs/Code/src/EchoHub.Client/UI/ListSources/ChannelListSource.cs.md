# ChannelListSource

> **File:** `src/EchoHub.Client/UI/ListSources/ChannelListSource.cs`  
> **Kind:** class

```csharp
public class ChannelListSource : IListDataSource
```


A colored, list-backed IListDataSource that presents channel names with visual affordances: an active-channel indicator, unread count badges, and markers for protected, private and system channels. Use this when you need a ListView-compatible data source that maintains channel ordering, per-channel unread counts and simple visual state (active, mention, protected/private, system) instead of hand-rendering each row.

## Remarks
ChannelListSource centralizes the channel-list state required by a ListView: the ordered channel names, a per-channel unread count map and several role sets (protected, mention, private and system). It exposes a single Update method that replaces the in-memory collections in one operation and (unless suspended) raises a Reset collection-changed event so consumers can re-layout or refresh. The class also provides MaxItemLength to help the host compute layout and ToList to produce a display-friendly list of channel strings (each prefixed with '#'). Rendering is delegated to the ListView via the Render method; the class supplies attributes (ActiveAttr, UnreadAttr, NormalAttr, BadgeAttr, MentionAttr, SystemAttr) and simple prefix/marker rules so the view paints active items, unread badges and visual separation for system channels.

## Notes
- Update clears and replaces all internal collections; call it with the full desired state rather than trying to patch individual entries.
- The Count/MaxItemLength values are derived from the current channel list. MaxItemLength computes name.Length + 6 (reserved space for prefixes/badges), so layout logic should consider that padding when sizing the list column.
- CollectionChanged will be invoked with a NotifyCollectionChangedAction.Reset at the end of Update unless SuspendCollectionChangedEvent is true. SuspendCollectionChangedEvent is a simple in-memory flag — using it prevents the Reset event from being raised during an Update.
- IsMarked and SetMark are intentionally inert (IsMarked always returns false and SetMark is a no-op), so callers should not rely on marking support from this source.
- Render moves the ListView cursor using Math.Max(col - viewportX, 0) to account for horizontal scrolling (viewportX). Hosts should provide correct viewportX and width values so rendering and clipping behave as intended.