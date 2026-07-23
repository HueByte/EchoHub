# SearchListSource

> **File:** `src/EchoHub.Client/UI/ListSources/SearchListSource.cs`  
> **Kind:** class

```csharp
public class SearchListSource(List<SearchResult> items) : IListDataSource
```


List data source that feeds a search dialog's ListView: it maintains an original item list, supports case-insensitive filtering by label or key, raises a Reset collection-changed notification when the filter changes (unless suspended), and renders each row with color-coding depending on the SearchResultType.

## Remarks
This class combines two responsibilities commonly needed by a search dialog: fast, in-memory filtering of a fixed set of SearchResult records and rendering of those results into a ListView with per-type coloring. Consumers attach to CollectionChanged to refresh the UI when Filter(string) updates the visible set. Render uses RenderHelpers.WriteText to draw the label and then fills the remainder of the column; it chooses a highlight (selected) attribute from the ListView or a per-result attribute (channel/action) and preserves the list's background when a per-result attribute leaves the background as Color.None.

## Notes
- Filter is case-insensitive and matches either SearchResult.Label or SearchResult.Key.
- When Filter receives a null/whitespace query the visible list is reset to all items and a Reset event is raised (unless SuspendCollectionChangedEvent is true).
- IsMarked and SetMark are no-ops; this data source does not track per-item marks.
- Dispose is a no-op; there are no unmanaged resources to release.
- MaxItemLength returns 0 when there are no filtered items.
- This class does not provide internal synchronization; callers should ensure thread-safety when mutating the source list or calling Filter from multiple threads.