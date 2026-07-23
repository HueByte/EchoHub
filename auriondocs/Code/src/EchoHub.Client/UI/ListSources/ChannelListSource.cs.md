# ChannelListSource

> **File:** `src/EchoHub.Client/UI/ListSources/ChannelListSource.cs`  
> **Kind:** class

```csharp
public class ChannelListSource : IListDataSource
```


A specialized `IListDataSource` implementation that provides a colored, badge-capable channel list for the UI. Use `ChannelListSource` when you need a channel list that shows an active indicator, unread count badges, and visual differences for protected, private, mention, and system channels; call `Update` to replace the source data and rely on the `CollectionChanged` event to refresh the view.

## Remarks
`ChannelListSource` centralizes both the model and the presentation hints required to render a channel list: it stores the channel names (`_channelNames`), per-channel unread counts (`_unreadCounts`), categorical sets (`_protectedChannels`, `_mentionChannels`, `_privateChannels`, `_systemChannels`), and the `_activeChannel`. Visual presentation is driven by a small set of static attributes (`ActiveAttr`, `UnreadAttr`, `NormalAttr`, `BadgeAttr`, `MentionAttr`, `SystemAttr`) and the `Render` method composes the line prefix and decorations (active marker, system rule, protection/private markers, unread badge) before drawing to the provided `ListView`. The `Update` method replaces the internal collections, recomputes `MaxItemLength` (uses `channels.Max(c => c.Length + 6)` as a conservative width heuristic), and raises a `NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)` via the `CollectionChanged` event unless `SuspendCollectionChangedEvent` is set.

## Notes
- `ChannelListSource` is not synchronized: internal collections are not thread-safe. Callers must ensure updates happen on the UI thread or otherwise synchronize access to avoid races.
- Set `SuspendCollectionChangedEvent` to `true` to suppress the reset notification during bulk updates; remember to re-enable it if callers rely on the `CollectionChanged` event for redraws.
- The `IsMarked` and `SetMark` implementations are no-ops, so the `IListDataSource` marking contract is not supported by this source; consumers expecting persisted item marks will not get them from `ChannelListSource`.