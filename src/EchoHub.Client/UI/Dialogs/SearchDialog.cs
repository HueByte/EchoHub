using EchoHub.Client.UI.ListSources;

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.Text;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EchoHub.Client.UI.Dialogs;

public enum SearchResultType
{
    Channel,
    Action
}

public record SearchResult(SearchResultType Type, string Key, string Label);

/// <summary>
/// Command-palette style search dialog (Ctrl+K) for navigating channels and triggering app actions.
/// </summary>
public static class SearchDialog
{
    private static readonly IReadOnlyList<SearchResult> DefaultActions = [
        new(SearchResultType.Action, "connect",        "Connect to Server"),
        new(SearchResultType.Action, "disconnect",     "Disconnect"),
        new(SearchResultType.Action, "logout",         "Logout"),
        new(SearchResultType.Action, "profile",        "My Profile"),
        new(SearchResultType.Action, "status",         "Set Status"),
        new(SearchResultType.Action, "create-channel", "Create Channel"),
        new(SearchResultType.Action, "delete-channel", "Delete Channel"),
        new(SearchResultType.Action, "servers",        "Saved Servers"),
        new(SearchResultType.Action, "toggle-users",   "Toggle Users Panel"),
        new(SearchResultType.Action, "updates",        "Check for Updates"),
        new(SearchResultType.Action, "quit",           "Quit"),
    ];

    public static SearchResult? Show(IApplication app, IReadOnlyList<string> channels)
    {
        SearchResult? result = null;
        var source = new SearchListSource(BuildAllItems(channels));

        var dialog = new Dialog
        {
            Title = "Search",
            Width = 59,
            Height = 22,
        };

        var hintLabel = new Label
        {
            Text = "Channels and actions \u2502 \u2193 to navigate \u2502 Enter to select",
            X = 1,
            Y = 1,
        };

        var searchField = new TextField
        {
            X = 1,
            Y = 2,
            Title = "Search",
            Width = Dim.Fill(2),
        };

        var resultList = new ListView
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2),
            Height = Dim.Fill(3),
            Source = source
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(1),
        };

        if (source.Count > 0)
            resultList.SelectedItem = 0;

        searchField.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Key.K.WithCtrl)
            {
                e.Handled = true;
                app.RequestStop();
            }
        };

        searchField.TextChanged += (s, e) =>
        {
            source.Filter(searchField.Text ?? string.Empty);
            resultList.Source = source;
            if (source.Count > 0)
                resultList.SelectedItem = 0;
        };

        searchField.Accepting += (s, e) => TryConfirm(e);

        resultList.Accepting += (s, e) => TryConfirm(e);

        resultList.KeystrokeNavigator.SearchStringChanged += (s, e) =>
        {
            app.Invoke(() =>
            {
                searchField.SetFocus();
            });
        };

        cancelButton.Accepting += (s, e) =>
        {
            result = null;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Key.K.WithCtrl)
            {
                e.Handled = true;
                app.RequestStop();
            }
        };

        dialog.Add(hintLabel, searchField, resultList, cancelButton);
        searchField.SetFocus();
        app.Run(dialog);

        return result;

        void TryConfirm(CommandEventArgs e)
        {
            var idx = resultList.SelectedItem ?? 0;
            if (source.Count > 0 && idx >= 0 && idx < source.Count)
            {
                result = source.GetItem(idx);
                e.Handled = true;
                app.RequestStop();
            }
        }
    }

    private static List<SearchResult> BuildAllItems(IReadOnlyList<string> channels)
    {
        var items = new List<SearchResult>();
        foreach (var ch in channels)
            items.Add(new SearchResult(SearchResultType.Channel, ch, $"#{ch}"));
        items.AddRange(DefaultActions);
        return items;
    }
}
