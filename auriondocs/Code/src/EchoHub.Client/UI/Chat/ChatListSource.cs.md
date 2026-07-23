# ChatListSource

> **File:** `src/EchoHub.Client/UI/Chat/ChatListSource.cs`  
> **Kind:** class

```csharp
public class ChatListSource : IListDataSource
```


A list-backed data source for chat messages that implements IListDataSource and performs grapheme-aware rendering with per-segment coloring, mention-background highlighting, and a focus-based full-row highlight. Use this when supplying chat messages to a ListView-like control that expects the data source to manage items, raise collection-change notifications, and draw each row with segment-level attributes and correct column clipping.

## Remarks
ChatListSource maintains an internal `List<ChatLine>`, tracks the longest item via MaxItemLength, and raises a CollectionChanged (Reset) event whenever the collection is modified unless SuspendCollectionChangedEvent is set. Its Render implementation is grapheme-aware (uses GraphemeHelper.GetGraphemes and each grapheme's column width) and applies attributes per ChatSegment: a Focus attribute (when the row is selected and the list has focus) or the segment's color with fallbacks for missing backgrounds. If a ChatLine.IsMention is true the renderer uses ChatColors.MentionHighlightAttr.Background to override segment backgrounds and to fill the remainder of the row.

## Notes
- GetLine returns null for out-of-range indices; callers should validate the index first.
- IsMarked/SetMark are intentionally no-ops in this implementation and Dispose is a no-op — no per-item mark state or unmanaged cleanup is performed.
- MaxItemLength is updated only when lines are added/inserted; mutating a ChatLine.TextLength after insertion will not update MaxItemLength automatically. Use SuspendCollectionChangedEvent to batch updates and suppress the Reset event during bulk changes.