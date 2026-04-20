using EchoHub.Client.UI.Chat;
using EchoHub.Client.UI.Dialogs;

using System.Collections;
using System.Collections.Specialized;

using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.Views;

using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.ListSources;

/// <summary>
/// List data source for the search dialog with filtering and colored rendering.
/// </summary>
public class SearchListSource(List<SearchResult> items) : IListDataSource
{
    private readonly List<SearchResult> _allItems = items;
    private List<SearchResult> _filtered = [.. items];

    private static readonly Attribute ChannelAttribute = new(Color.BrightCyan, Color.None);
    private static readonly Attribute ActionAttribute = new(Color.White, Color.None);

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public int Count => _filtered.Count;
    public int MaxItemLength => _filtered.Count > 0 ? _filtered.Max(i => i.Label.GetColumns()) : 0;
    public bool SuspendCollectionChangedEvent { get; set; }

    public void Filter(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _filtered = [.. _allItems];
        }
        else
        {
            _filtered = [.. _allItems.Where(i =>
                i.Label.Contains(query, StringComparison.OrdinalIgnoreCase)
                || i.Key.Contains(query, StringComparison.OrdinalIgnoreCase))];
        }

        if (!SuspendCollectionChangedEvent)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public SearchResult? GetItem(int index) => index >= 0 && index < _filtered.Count ? _filtered[index] : null;

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _filtered.Select(i => (object)i.Label).ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var entry = _filtered[item];
        var fillAttr = listView.GetAttributeForRole(selected ? VisualRole.Focus : VisualRole.Normal);

        Attribute itemAttr;
        if (selected)
        {
            itemAttr = fillAttr;
        }
        else
        {
            var raw = entry.Type switch
            {
                SearchResultType.Channel => ChannelAttribute,
                SearchResultType.Action => ActionAttribute,
                _ => fillAttr
            };
            itemAttr = raw.Background == Color.None ? raw with { Background = fillAttr.Background } : raw;
        }

        listView.SetAttribute(itemAttr);

        var drawn = RenderHelpers.WriteText(listView, entry.Label, 0, width);

        listView.SetAttribute(fillAttr);
        for (var i = drawn; i < width; i++)
            listView.AddStr(" ");
    }

    public void Dispose() { }
}
