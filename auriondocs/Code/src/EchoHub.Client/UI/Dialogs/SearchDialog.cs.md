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


SearchDialog is a command-palette style search dialog used to quickly navigate channels and trigger common app actions from a single, keyboard-driven interface. Use it when you want fast, non-mouse access to channels and actions by filtering a combined list and selecting with Enter.

## Remarks
SearchDialog composes a modal dialog that presents both channel names and a predefined set of actions, merged into a single searchable list via a SearchListSource. It returns the selected SearchResult and signals completion to the hosting application by invoking RequestStop on IApplication, keeping the dialog logic decoupled from the rest of the UI. This abstraction enables a reusable, consistent navigation surface across different parts of the app.

## Example
```csharp
// Example
IApplication app = /* obtain your app instance */;
IReadOnlyList<string> channels = new[] { "general", "engineering" };
var result = SearchDialog.Show(app, channels);
if (result != null)
{
    // Handle the selected item (channel or action) here.
}
```

## Notes
- The dialog includes a hint, a text field for filtering, a list of results, and a Cancel button; selection is returned as a SearchResult, or null if cancelled.
- Ctrl+K handling in both the dialog and the search field cancels the operation by requesting stop from the application, so be aware that this combo acts as a cancel gesture rather than an open/search trigger.
- When items exist, the first item is pre-selected; filtering updates the source and may reset the selection.


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


Represents a single entry in search results, encapsulating the result's category (Type), a key (Key), and a user-facing label (Label). As a positional-record, it is immutable and compared by value, which makes it convenient to pass around and render in the search UI.

## Remarks
Use SearchResult to model a single outcome returned by the search feature. Type communicates the kind of item (as defined by SearchResultType), Key is the stable identifier for navigation or lookup, and Label is the display text shown in the results list. Because it is a deconstructible record, you can conveniently extract its fields with deconstruction or pattern matching, and equality checks are based on the content rather than the instance identity.

## Notes
- Immutability: SearchResult uses a primary constructor; properties are read-only and a modified instance must be created with a with-expression or a new constructor.
- Deconstruction: The positional constructor enables deconstruction: var (t, k, l) = result; or access via result.Type, result.Key, result.Label.
- Type relies on the SearchResultType enum; when consuming code, prefer switching on Type rather than comparing display strings.

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


Represents the category of a search result in the EchoHub client UI, distinguishing Channel results from Action results. Developers reach for this enum to branch rendering or navigation logic based on the result type, instead of using boolean flags or string comparisons.

## Remarks
Because it is a small discriminant, SearchResultType is typically consumed alongside a broader SearchResult structure. It enables simple pattern matching in switch expressions or if statements, guiding UI decisions such as which view to open or which icon to display when a user selects a result.

---