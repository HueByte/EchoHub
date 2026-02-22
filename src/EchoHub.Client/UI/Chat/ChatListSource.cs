using System.Collections;
using System.Collections.Specialized;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Chat;

/// <summary>
/// Custom list data source for chat messages with per-segment coloring.
/// </summary>
public class ChatListSource : IListDataSource
{
    private readonly List<ChatLine> _lines = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _lines.Count;
    public int MaxItemLength { get; private set; }
    public bool SuspendCollectionChangedEvent { get; set; }

    public void Add(ChatLine line)
    {
        _lines.Add(line);
        UpdateMaxLength(line);
        RaiseCollectionChanged();
    }

    public void AddRange(IEnumerable<ChatLine> lines)
    {
        foreach (var line in lines)
        {
            _lines.Add(line);
            UpdateMaxLength(line);
        }
        RaiseCollectionChanged();
    }

    public void InsertRange(int index, IEnumerable<ChatLine> lines)
    {
        var items = lines.ToList();
        _lines.InsertRange(index, items);
        foreach (var line in items)
            UpdateMaxLength(line);
        RaiseCollectionChanged();
    }

    public void Clear()
    {
        _lines.Clear();
        MaxItemLength = 0;
        RaiseCollectionChanged();
    }

    public ChatLine? GetLine(int index) => index >= 0 && index < _lines.Count ? _lines[index] : null;

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _lines.Select(l => l.ToString()).ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var chatLine = _lines[item];
        var normalAttr = listView.GetAttributeForRole(VisualRole.Normal);
        var mentionBg = chatLine.IsMention ? ChatColors.MentionHighlightAttr.Background : (Color?)null;

        int charPos = 0;
        int drawnChars = 0;

        foreach (var segment in chatLine.Segments)
        {
            var attr = segment.Color ?? normalAttr;
            if (attr.Background == Color.None)
                attr = attr with { Background = normalAttr.Background };
            if (mentionBg.HasValue)
                attr = attr with { Background = mentionBg.Value };
            listView.SetAttribute(attr);

            foreach (var grapheme in GraphemeHelper.GetGraphemes(segment.Text))
            {
                var cols = Math.Max(grapheme.GetColumns(), 1);
                if (charPos >= viewportX && drawnChars + cols <= width)
                {
                    listView.AddStr(grapheme);
                    drawnChars += cols;
                }
                charPos += cols;
            }
        }

        var fillAttr = mentionBg.HasValue ? new Attribute(normalAttr.Foreground, mentionBg.Value) : normalAttr;
        listView.SetAttribute(fillAttr);
        for (int i = drawnChars; i < width; i++)
            listView.AddStr(" ");
    }

    private void UpdateMaxLength(ChatLine line)
    {
        if (line.TextLength > MaxItemLength)
            MaxItemLength = line.TextLength;
    }

    private void RaiseCollectionChanged()
    {
        if (!SuspendCollectionChangedEvent)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void Dispose() { }
}
