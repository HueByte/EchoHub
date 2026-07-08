# SearchListSource

> **File:** `src/EchoHub.Client/UI/ListSources/SearchListSource.cs`  
> **Kind:** class

```csharp
public class SearchListSource(List<SearchResult> items) : IListDataSource
```


Provides an IListDataSource implementation tailored for the search dialog: it wraps a fixed list of SearchResult items, supports runtime filtering by label or key, exposes collection-change notifications, and renders each item with type-specific foreground coloring while preserving the list's background/selection visuals. Use this when you need a ready-made, UI-aware source for a search list that handles filtering and colored rendering for SearchResult entries.

## Remarks
This class centralizes the search-list responsibilities for the dialog: maintaining the master item list, producing a filtered view, raising a Reset notification when the filter changes, and rendering each visible row with attributes selected by the SearchResultType. The rendering logic deliberately preserves the list view's background (selection or normal) when a type-specific attribute does not specify its own background so that selection highlighting remains visible.

## Notes
- Filtering is case-insensitive and uses StringComparison.OrdinalIgnoreCase for both the Label and Key properties.
- SuspendCollectionChangedEvent can be set to true to suppress the Reset notification during bulk updates; otherwise Filter(...) will raise a Reset via CollectionChanged when the result set changes.
- IsMarked and SetMark are no-ops (marking is not supported by this source); Dispose is also a no-op.
- MaxItemLength computes the maximum visible column width using each entry's Label.GetColumns(); it returns 0 when the filtered list is empty.