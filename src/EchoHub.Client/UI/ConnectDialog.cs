using System.Collections.ObjectModel;
using EchoHub.Client.Config;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace EchoHub.Client.UI;

/// <summary>
/// Result returned from the connect dialog.
/// </summary>
public record ConnectDialogResult(string ServerUrl, string Username, string Password, bool IsRegister);

/// <summary>
/// A Terminal.Gui dialog for entering server connection and authentication details.
/// Includes a saved servers selector when saved servers are available.
/// </summary>
public sealed class ConnectDialog
{
    /// <summary>
    /// Shows the connect dialog with an optional list of saved servers.
    /// Returns the result, or null if cancelled.
    /// </summary>
    public static ConnectDialogResult? Show(IApplication app, List<SavedServer>? savedServers = null)
    {
        ConnectDialogResult? result = null;
        savedServers ??= [];

        var hasSavedServers = savedServers.Count > 0;
        var dialogHeight = hasSavedServers ? 20 : 16;

        var dialog = new Dialog { Title = "Connect to Server", Width = 60, Height = dialogHeight };

        int yOffset = 0;

        // -- Saved Servers section (if any) -----------------------------------
        ListView? savedServerList = null;
        if (hasSavedServers)
        {
            var savedLabel = new Label
            {
                Text = "Saved Servers:",
                X = 1,
                Y = 1
            };
            dialog.Add(savedLabel);

            var serverDisplayNames = savedServers
                .Select(s => $"{s.Name} ({s.Username ?? "?"})")
                .ToList();

            savedServerList = new ListView
            {
                Source = new ListWrapper<string>(new ObservableCollection<string>(serverDisplayNames)),
                X = 1,
                Y = 2,
                Width = Dim.Fill(2),
                Height = 3
            };
            dialog.Add(savedServerList);

            // Visual separator
            var separator = new Label
            {
                Text = new string('-', 56),
                X = 1,
                Y = 5
            };
            dialog.Add(separator);

            yOffset = 5;
        }

        // -- Manual entry fields ----------------------------------------------
        var urlLabel = new Label
        {
            Text = "Server URL:",
            X = 1,
            Y = yOffset + 1
        };
        var urlField = new TextField
        {
            Text = "http://localhost:5000",
            X = 15,
            Y = yOffset + 1,
            Width = Dim.Fill(2)
        };

        var userLabel = new Label
        {
            Text = "Username:",
            X = 1,
            Y = yOffset + 3
        };
        var userField = new TextField
        {
            Text = "",
            X = 15,
            Y = yOffset + 3,
            Width = Dim.Fill(2)
        };

        var passLabel = new Label
        {
            Text = "Password:",
            X = 1,
            Y = yOffset + 5
        };
        var passField = new TextField
        {
            Text = "",
            X = 15,
            Y = yOffset + 5,
            Width = Dim.Fill(2),
            Secret = true
        };

        var displayLabel = new Label
        {
            Text = "Display Name:",
            X = 1,
            Y = yOffset + 7
        };
        var displayField = new TextField
        {
            Text = "",
            X = 15,
            Y = yOffset + 7,
            Width = Dim.Fill(2)
        };

        var loginButton = new Button
        {
            Text = "Login",
            IsDefault = true,
            X = Pos.Center() - 20,
            Y = yOffset + 9
        };

        var registerButton = new Button
        {
            Text = "Register",
            X = Pos.Center() - 5,
            Y = yOffset + 9
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 10,
            Y = yOffset + 9
        };

        // Wire saved server selection to auto-fill fields
        if (savedServerList is not null && savedServers.Count > 0)
        {
            savedServerList.ValueChanged += (sender, e) =>
            {
                var index = e.NewValue;
                if (index.HasValue && index.Value >= 0 && index.Value < savedServers.Count)
                {
                    var server = savedServers[index.Value];
                    urlField.Text = server.Url;
                    userField.Text = server.Username ?? "";
                }
            };

            // Pre-fill with the first saved server
            urlField.Text = savedServers[0].Url;
            userField.Text = savedServers[0].Username ?? "";
        }

        loginButton.Accepting += (s, e) =>
        {
            var url = urlField.Text?.Trim() ?? string.Empty;
            var user = userField.Text?.Trim() ?? string.Empty;
            var pass = passField.Text ?? string.Empty;

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.ErrorQuery(app, "Validation", "Server URL, username, and password are required.", "OK");
                e.Handled = true;
                return;
            }

            result = new ConnectDialogResult(url, user, pass, IsRegister: false);
            e.Handled = true;
            app.RequestStop();
        };

        registerButton.Accepting += (s, e) =>
        {
            var url = urlField.Text?.Trim() ?? string.Empty;
            var user = userField.Text?.Trim() ?? string.Empty;
            var pass = passField.Text ?? string.Empty;

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.ErrorQuery(app, "Validation", "Server URL, username, and password are required.", "OK");
                e.Handled = true;
                return;
            }

            result = new ConnectDialogResult(url, user, pass, IsRegister: true);
            e.Handled = true;
            app.RequestStop();
        };

        cancelButton.Accepting += (s, e) =>
        {
            result = null;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.Add(urlLabel, urlField, userLabel, userField, passLabel, passField,
            displayLabel, displayField, loginButton, registerButton, cancelButton);

        if (hasSavedServers && savedServerList is not null)
            savedServerList.SetFocus();
        else
            urlField.SetFocus();

        app.Run(dialog);

        return result;
    }
}
