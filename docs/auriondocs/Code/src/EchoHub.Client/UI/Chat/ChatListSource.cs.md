# ChatListSource

> **File:** `src/EchoHub.Client/UI/Chat/ChatListSource.cs`  
> **Kind:** class

```csharp
public class ChatListSource : IListDataSource
```


A list data source that stores [`ChatLine`](ChatLine.cs.md) instances and renders them into a UI list with per-segment coloring and mention highlighting. Reach for `ChatListSource` when you need an `IListDataSource` implementation that maintains chat-specific layout state (like `MaxItemLength`) and performs per-grapheme drawing of `ChatLine.Segments` so segment colors and mention backgrounds are respected during rendering.

## Remarks
`ChatListSource` maintains an internal `List<ChatLine>` (`_lines`) and exposes simple mutation operations (`Add`, `AddRange`, `InsertRange`, `Clear`) while tracking the longest item in `MaxItemLength`. It raises `CollectionChanged` (unless `SuspendCollectionChangedEvent` is set) so UI consumers can refresh efficiently; `AddRange`/`InsertRange` and `Clear` invoke `RaiseCollectionChanged` only once after the batch operation. The `Render` implementation iterates each `ChatLine.Segments`, chooses an `Attribute` per segment (falling back to the list's `VisualRole.Normal` attribute or applying `ChatColors.MentionHighlightAttr.Background` when `ChatLine.IsMention`), and draws graphemes using `GraphemeHelper` while respecting `viewportX` and `width`. The class intentionally leaves `IsMarked`/`SetMark` as no-ops and has an empty `Dispose`.

## Notes
- `MaxItemLength` is only increased when lines are added and reset only by `Clear`. There is no removal API that updates `MaxItemLength`, so it can become stale if items are removed or if existing `ChatLine.TextLength` values change externally.
- `GetLine(int)` returns `null` for out-of-range indexes, but `Render` accesses `_lines[item]` directly; callers must ensure the `item` index passed to `Render` is valid to avoid an `IndexOutOfRangeException`.
- Setting `SuspendCollectionChangedEvent` suppresses `CollectionChanged` invocations while mutations occur, but mutations still apply immediately to the internal list. Consumers that suppress events must ensure the UI is refreshed after re-enabling events (the next mutating call will raise `CollectionChanged` unless `SuspendCollectionChangedEvent` remains true).