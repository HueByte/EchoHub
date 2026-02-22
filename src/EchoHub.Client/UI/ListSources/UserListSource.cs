using System.Collections;
using System.Collections.Specialized;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.ListSources;

/// <summary>
/// Custom list data source for the online users panel with per-user nickname colors.
/// </summary>
public class UserListSource : IListDataSource
{
    private readonly List<(string Text, Attribute? NameColor)> _users = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public int Count => _users.Count;
    public int MaxItemLength { get; private set; }
    public bool SuspendCollectionChangedEvent { get; set; }

    public void Update(List<(string Text, Attribute? NameColor)> users)
    {
        _users.Clear();
        _users.AddRange(users);
        MaxItemLength = users.Count > 0 ? users.Max(u => u.Text.GetColumns()) : 0;
        if (!SuspendCollectionChangedEvent)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _users.Select(u => u.Text).ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var (text, nameColor) = _users[item];
        var normalAttr = listView.GetAttributeForRole(selected ? VisualRole.Focus : VisualRole.Normal);

        // Find where the name starts (after status icon + space + optional role badge)
        // Format: "● ★Username" or "● Username"
        var graphemes = GraphemeHelper.GetGraphemes(text).ToList();
        int nameStart = 0;
        while (nameStart < graphemes.Count)
        {
            var g = graphemes[nameStart];
            if (g.Length > 0 && (char.IsLetterOrDigit(g[0]) || g[0] == '_'))
                break;
            nameStart++;
        }

        int drawnChars = 0;

        // Draw prefix (status icon + role badge) in normal color
        listView.SetAttribute(normalAttr);
        for (int i = 0; i < nameStart; i++)
        {
            var cols = Math.Max(graphemes[i].GetColumns(), 1);
            if (drawnChars + cols > width) break;
            listView.AddStr(graphemes[i]);
            drawnChars += cols;
        }

        // Draw name in nickname color
        var userAttr = selected ? normalAttr : nameColor ?? normalAttr;
        listView.SetAttribute(userAttr);
        for (int i = nameStart; i < graphemes.Count; i++)
        {
            var cols = Math.Max(graphemes[i].GetColumns(), 1);
            if (drawnChars + cols > width) break;
            listView.AddStr(graphemes[i]);
            drawnChars += cols;
        }

        // Fill rest
        listView.SetAttribute(normalAttr);
        for (int i = drawnChars; i < width; i++)
            listView.AddStr(" ");
    }

    public void Dispose() { }
}
