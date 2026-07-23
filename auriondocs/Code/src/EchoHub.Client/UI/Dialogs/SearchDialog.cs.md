# SearchDialog.cs

> **Source:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`

## Contents

- [SearchDialog](#searchdialog)
- [SearchResult](#searchresult)
- [SearchResultType](#searchresulttype)

---

## SearchDialog
> **File:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`  
> **Kind:** class

```csharp
public static class SearchDialog
```


## Source Code
Static class `SearchDialog` provides a Ctrl+K-activated, command-palette style dialog for navigating channels and triggering app actions. It merges the current `IReadOnlyList<string>` of `channels` with a fixed set of default `SearchResult` actions into a single searchable list presented in a `Dialog` consisting of a `Label` hint, a `TextField` input, and a `ListView` of results; typing filters the list and Enter selects. The `Show` method returns the selected `SearchResult` or `null` if canceled, communicating through the provided `IApplication` instance.

## Remarks
By centralizing both channels and common actions, `SearchDialog` reduces context switching and speeds navigation from anywhere in the UI. The implementation delegates list rendering and filtering to [`SearchListSource`](../ListSources/SearchListSource.cs.md), decoupling the data shape from the presentation; adding new channels or actions simply extends the default actions or the input channels without altering the UI flow.

## Notes
- The dialog binds Ctrl+K to stop the dialog, so avoid conflicting hotkeys in the surrounding application.

## Dependency APIs (verified signatures)
The REAL, parser-verified API surface of this symbol's collaborators:

- record `SearchResult` (`src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`)
- class [`SearchListSource`](../ListSources/SearchListSource.cs.md) (`src/EchoHub.Client/UI/ListSources/SearchListSource.cs`)
  - field `Attribute ChannelAttribute`
  - field `Attribute ActionAttribute`
  - property `int Count`
  - property `int MaxItemLength`
  - property `bool SuspendCollectionChangedEvent`
  - `void Filter(string query)`
  - `SearchResult? GetItem(int index)`
  - `bool IsMarked(int item)`
  - `void SetMark(int item, bool value)`
  - `IList ToList()`
  - `void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX)`
  - `void Dispose()`
- enum `SearchResultType` (`src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`)

## Symbol To Document
- Name: `SearchDialog`
- Kind: class
- File: `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`
- Language: `csharp`
- ID: `7ba458ca-8e14-48c9-9536-988f98e9e83c`

---

## SearchResult
> **File:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`  
> **Kind:** record

```csharp
public record SearchResult(SearchResultType Type, string Key, string Label)
```

**Parameters:**

| Parameter | Type | Default |
|-----------|------|---------|
| `Type` | `SearchResultType` | — |
| `Key` | `string` | — |
| `Label` | `string` | — |


Represents a single item in search results as an immutable, value-based carrier. It groups the result kind (`SearchResultType`), an identifying `Key`, and a user-facing `Label` to display in the UI. As a `record`, it gains structural equality and convenient deconstruction, which makes it easy to compare results and extract its fields when handling selections in the search dialog.

## Remarks
This type serves as a stable data contract between the search logic and the UI layer, decoupling data shape from presentation. It uses `record` semantics to provide value equality and immutability, enabling straightforward deduplication and pattern-based handling of results. The three members (`Type`, `Key`, `Label`) collectively support both programmatic lookup and user-friendly rendering.

## Notes
- The `Key` should be stable and unique within a given `Type` to avoid ambiguity when presenting or selecting results.

---

## SearchResultType
> **File:** `src/EchoHub.Client/UI/Dialogs/SearchDialog.cs`  
> **Kind:** enum

```csharp
public enum SearchResultType
{
    Channel,
    Action
}
```


Represents the kind of item produced by a search in the UI, distinguishing [`Channel`](../../../EchoHub.Core/Models/Channel.cs.md) results from `Action` results. Use `SearchResultType` when rendering or handling search results in the `SearchDialog` flow to steer UI decisions without inspecting the raw payload.

## Remarks
This enum centralizes the UI's categorization of search results, enabling the dialog to select icons, labels, or handlers in a type-safe way. It decouples the results' payload from how they're displayed and makes it straightforward to extend with additional result kinds in the future.

## Example

```csharp
SearchResultType type = SearchResultType.Channel;
switch (type)
{
    case SearchResultType.Channel:
        Console.WriteLine("Render as channel");
        break;
    case SearchResultType.Action:
        Console.WriteLine("Render as action");
        break;
}
```


---