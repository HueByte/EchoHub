# SearchListSource

> **File:** `src/EchoHub.Client/UI/ListSources/SearchListSource.cs`  
> **Kind:** class

```csharp
public class SearchListSource(List<SearchResult> items) : IListDataSource
```


A list-data source implementation used by the search dialog that presents a filtered view of [`SearchResult`](../Dialogs/SearchDialog.cs.md) items and renders each entry with type-specific coloring. Use `SearchListSource` when you need a lightweight, read-only collection for a `ListView` that supports text filtering via `Filter` and per-item rendering via `Render`.

## Remarks
`SearchListSource` holds the full set of items in `_allItems` and maintains a filtered snapshot in `_filtered` that drives `Count`, `MaxItemLength`, `GetItem`, and `ToList`. Filtering is performed by `Filter` using `StringComparison.OrdinalIgnoreCase` against both the `Label` and `Key` of each [`SearchResult`](../Dialogs/SearchDialog.cs.md). Rendering delegates text layout to `RenderHelpers.WriteText` and chooses visual attributes based on the [`SearchResultType`](../Dialogs/SearchDialog.cs.md) (using `ChannelAttribute` and `ActionAttribute`); when a chosen attribute has no background color it inherits the `ListView` fill background so the entry blends with the surrounding cells. The `CollectionChanged` event is raised with a `NotifyCollectionChangedEventArgs` reset after `Filter` updates unless `SuspendCollectionChangedEvent` is set.

## Notes
- `Render` indexes into `_filtered` directly and assumes the caller supplies a valid `item` index; callers should use `Count` or `GetItem` to validate indices to avoid out-of-range access.
- `IsMarked` and `SetMark` are intentionally no-ops: this source does not track per-item marks, so code that expects marking behavior will need a wrapper or a different `IListDataSource` implementation.
- `Dispose` is a no-op; there are no native resources held by `SearchListSource`, but consumers that expect disposal semantics should be aware nothing is released by calling `Dispose`.