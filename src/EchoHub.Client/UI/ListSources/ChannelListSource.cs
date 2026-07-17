using System.Collections;
using System.Collections.Specialized;
using EchoHub.Client.UI.Chat;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.ListSources;

/// <summary>
/// Custom list data source for colored channel list rendering.
/// Active channel gets a > indicator, unread channels are bright with a count badge.
/// </summary>
public class ChannelListSource : IListDataSource
{
    private readonly List<string> _channelNames = [];
    private readonly Dictionary<string, int> _unreadCounts = [];
    private readonly HashSet<string> _protectedChannels = [];
    private readonly HashSet<string> _mentionChannels = [];
    private readonly HashSet<string> _privateChannels = [];
    private readonly HashSet<string> _systemChannels = [];
    private string _activeChannel = string.Empty;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public int Count => _channelNames.Count;
    public int MaxItemLength { get; private set; }
    public bool SuspendCollectionChangedEvent { get; set; }

    private static readonly Attribute ActiveAttr = new(Color.White, Color.None);
    private static readonly Attribute UnreadAttr = new(Color.BrightCyan, Color.None);
    private static readonly Attribute NormalAttr = new(Color.DarkGray, Color.None);
    private static readonly Attribute BadgeAttr = new(Color.BrightYellow, Color.None);
    private static readonly Attribute MentionAttr = new(new Color(230, 140, 60), Color.None);
    // System channels (e.g. the live log room) render in green to set them apart from user rooms.
    private static readonly Attribute SystemAttr = new(Color.BrightGreen, Color.None);

    public void Update(List<string> channels, Dictionary<string, int> unread, string activeChannel,
        IReadOnlySet<string>? protectedChannels = null, IReadOnlySet<string>? mentionChannels = null,
        IReadOnlySet<string>? privateChannels = null, IReadOnlySet<string>? systemChannels = null)
    {
        _channelNames.Clear();
        _channelNames.AddRange(channels);
        _unreadCounts.Clear();
        foreach (var kv in unread)
            _unreadCounts[kv.Key] = kv.Value;
        _protectedChannels.Clear();
        if (protectedChannels is not null)
            _protectedChannels.UnionWith(protectedChannels);
        _mentionChannels.Clear();
        if (mentionChannels is not null)
            _mentionChannels.UnionWith(mentionChannels);
        _privateChannels.Clear();
        if (privateChannels is not null)
            _privateChannels.UnionWith(privateChannels);
        _systemChannels.Clear();
        if (systemChannels is not null)
            _systemChannels.UnionWith(systemChannels);
        _activeChannel = activeChannel;
        MaxItemLength = channels.Count > 0 ? channels.Max(c => c.Length + 6) : 0;
        if (!SuspendCollectionChangedEvent)
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool IsMarked(int item) => false;
    public void SetMark(int item, bool value) { }
    public IList ToList() => _channelNames.Select(n => $"#{n}").ToList();

    public void Render(ListView listView, bool selected, int item, int col, int row, int width, int viewportX = 0)
    {
        listView.Move(Math.Max(col - viewportX, 0), row);

        var name = _channelNames[item];
        var isActive = name == _activeChannel;
        _unreadCounts.TryGetValue(name, out var unread);
        var hasUnread = unread > 0;

        var normalAttr = listView.GetAttributeForRole(VisualRole.Normal);
        var focusAttr = listView.GetAttributeForRole(VisualRole.Focus);
        var isSystem = _systemChannels.Contains(name);
        // System channels get a leading rule so they read as a pinned, separate group at the top.
        var prefix = isSystem ? (isActive ? "▌ " : "▎ ") : (isActive ? "> " : "  ");
        // Trailing * marks password-protected (+k) channels; ~ marks private (unlisted) ones
        var channelText = $"#{name}";
        if (_protectedChannels.Contains(name))
            channelText += "*";
        if (_privateChannels.Contains(name))
            channelText += "~";
        var badge = hasUnread ? $" ({unread})" : "";

        // Resolve Transparent backgrounds to the view's actual background
        Attribute Resolve(Attribute attr) =>
            attr.Background == Color.None ? attr with { Background = normalAttr.Background } : attr;

        int drawnChars = 0;

        if (selected)
        {
            listView.SetAttribute(focusAttr);
            drawnChars = RenderHelpers.WriteText(listView, prefix + channelText + badge, drawnChars, width);
        }
        else
        {
            // System channels are green so they stand apart from user rooms; that green also
            // colors the leading rule and (dimmer) prefix.
            listView.SetAttribute(Resolve(isSystem ? SystemAttr : isActive ? ActiveAttr : NormalAttr));
            drawnChars = RenderHelpers.WriteText(listView, prefix, drawnChars, width);

            // Mentions escalate above plain unread: the whole entry turns orange
            var hasMention = _mentionChannels.Contains(name);
            var nameAttr = isSystem ? SystemAttr
                : isActive ? ActiveAttr
                : hasMention ? MentionAttr
                : hasUnread ? UnreadAttr
                : NormalAttr;
            listView.SetAttribute(Resolve(nameAttr));
            drawnChars = RenderHelpers.WriteText(listView, channelText, drawnChars, width);

            if (hasUnread)
            {
                listView.SetAttribute(Resolve(isSystem ? SystemAttr : hasMention ? MentionAttr : BadgeAttr));
                drawnChars = RenderHelpers.WriteText(listView, badge, drawnChars, width);
            }
        }

        var fillAttr = selected ? focusAttr : normalAttr;
        listView.SetAttribute(fillAttr);
        for (int i = drawnChars; i < width; i++)
            listView.AddStr(" ");
    }

    public void Dispose() { }
}
