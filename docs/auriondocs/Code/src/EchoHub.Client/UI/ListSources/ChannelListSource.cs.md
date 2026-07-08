# ChannelListSource

> **File:** `src/EchoHub.Client/UI/ListSources/ChannelListSource.cs`  
> **Kind:** class

```csharp
public class ChannelListSource : IListDataSource
```


Provides an IListDataSource implementation that maps a collection of channel names, unread counts and an active channel into a colored, badge-aware line renderer for a ListView. Use this class when you need a ready-made data source that displays channel lines with a leading active indicator (" >"), a bright unread state, and an optional unread count badge.

## Remarks
This class keeps the smallest domain-to-UI mapping: it stores channel names, per-channel unread counts and which channel is active, computes a MaxItemLength for layout, and raises a CollectionChanged Reset when the set of channels changes (unless collection-changed events are suspended). Rendering is delegated to the ListView and RenderHelpers APIs: when not selected the renderer composes three segments (prefix, channel text, badge) and applies different attributes for active, unread and normal states; when selected it uses the view's Focus attribute for the whole line. Transparent attribute backgrounds are resolved at render time to the ListView's normal background to ensure consistent background filling.

## Example
```csharp
var src = new ChannelListSource();
var channels = new List<string> { "general", "dev", "random" };
var unread = new Dictionary<string,int> { ["dev"] = 3 };
src.Update(channels, unread, activeChannel: "general");
// src.MaxItemLength is set; src.CollectionChanged is raised (unless suspended)
var list = src.ToList(); // ["#general", "#dev", "#random"]
```

## Notes
- Calling Update replaces the internal lists and raises a NotifyCollectionChanged Reset unless SuspendCollectionChangedEvent is true.  
- MaxItemLength is computed as channel.Length + 6 when channels exist; callers that size columns should account for this padding.  
- IsMarked and SetMark are intentionally no-ops (the source does not support per-item marks); ToList returns channel names prefixed with '#'.