# UserListSource

> **File:** `src/EchoHub.Client/UI/ListSources/UserListSource.cs`  
> **Kind:** class

```csharp
public class UserListSource : IListDataSource
```


A list data source that provides items for an online users panel and renders each entry with an optional per-user nickname color. Use this when you need a simple, read-only data source that exposes usernames and draws user lines containing a status icon/role badge prefix plus a colored nickname; it handles measuring item width and raises collection change notifications on Update (unless suspended).

## Remarks
This class wraps an internal list of tuples (Text, NameColor, Username) and implements IListDataSource for use with the UI ListView. It centralizes the rendering logic for user rows: it splits a rendered string into a non-name prefix (status icon and optional role badge) and the nickname, applies a nickname color when not selected, and clips output to the available width. Update replaces the entire backing list and recomputes MaxItemLength (measured in terminal column widths), then raises a Reset collection-changed event unless SuspendCollectionChangedEvent is set.

## Notes
- Update replaces the entire list; callers that need fine-grained change notifications must manage those themselves (this class emits only a Reset action). 
- SuspendCollectionChangedEvent if true prevents raising CollectionChanged during Update — useful during batch updates to avoid UI churn.
- GetUsername returns null for out-of-range indices; callers should check for null.
- IsMarked, SetMark and Dispose are no-ops in this implementation; they exist to satisfy the IListDataSource contract but have no effect here.
- Render allocates grapheme segments each call (via GraphemeHelper.GetGraphemes) and iterates them to decide where the nickname starts and to draw within the provided width; this can be a performance hotspot if the view renders frequently for many items.
- When an item is selected, the selected visual attribute is used for the whole line (selection takes precedence over the per-user nickname color).
