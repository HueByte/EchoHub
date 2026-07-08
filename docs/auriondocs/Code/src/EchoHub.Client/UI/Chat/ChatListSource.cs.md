# ChatListSource

> **File:** `src/EchoHub.Client/UI/Chat/ChatListSource.cs`  
> **Kind:** class

```csharp
public class ChatListSource : IListDataSource
```


A list data source that stores chat lines and renders them into a ListView with per-segment coloring and mention highlighting. Use this when you need a simple, mutable collection of ChatLine items that can be rendered with accurate grapheme/column handling and where the UI should be informed of collection resets via NotifyCollectionChanged.

## Remarks
ChatListSource centralizes two concerns for chat UI: maintaining an ordered collection of ChatLine objects (including a tracked MaxItemLength) and providing a renderer that walks each ChatLine's segments, applies per-segment colors, and draws grapheme-aware text while respecting a horizontal viewport. It intentionally exposes a SuspendCollectionChangedEvent flag so callers can batch updates without firing collection reset notifications on every mutation; when suspension is disabled the class raises a Reset notification to signal consumers to refresh.

## Notes
- Render assumes the caller passes a valid item index and indexes into the internal list directly; GetLine is bounds-safe but Render does not perform a bounds check — passing an out-of-range item to Render will throw an IndexOutOfRangeException.
- SuspendCollectionChangedEvent suppresses the Reset event while true; callers that suspend must ensure they either re-enable the event or manually notify observers after bulk changes.
- IsMarked and SetMark are no-ops in this implementation (they always return false / do nothing). They exist to satisfy IListDataSource but do not provide marking behavior.
- Rendering uses grapheme-aware iteration and column counting, so combining characters and wide glyphs are measured correctly; remaining horizontal space is filled with spaces using the current (possibly mention-highlighted) background attribute.
- MaxItemLength is updated on Add/AddRange/InsertRange and reset to 0 on Clear; it represents the largest ChatLine.TextLength seen so far.