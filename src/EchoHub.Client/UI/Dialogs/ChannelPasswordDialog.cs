using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace EchoHub.Client.UI.Dialogs;

/// <summary>
/// Prompts for a channel password when joining a protected channel.
/// Returns the entered password, or null if the user cancels.
/// </summary>
public sealed class ChannelPasswordDialog
{
    public static string? Show(IApplication app, string channelName, string? message = null)
    {
        string? result = null;

        var dialog = new Dialog { Title = $"Join #{channelName}", Width = 50, Height = 10, CommandsToBubbleUp = [] };

        var infoLabel = new Label
        {
            Text = message ?? $"#{channelName} is password protected.",
            X = 1,
            Y = 1
        };

        var passwordLabel = new Label { Text = "Password:", X = 1, Y = 3 };
        var passwordField = new TextField { X = 11, Y = 3, Width = Dim.Fill(2), Secret = true };

        var joinButton = new Button
        {
            Text = "Join",
            IsDefault = true,
            X = Pos.Center() - 9,
            Y = 5
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = 5
        };

        joinButton.Accepting += (s, e) =>
        {
            var password = passwordField.Text;
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.ErrorQuery(app, "Error", "Password is required.", "OK");
                return;
            }

            result = password;
            e.Handled = true;
            app.RequestStop();
        };

        cancelButton.Accepting += (s, e) =>
        {
            result = null;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.Add(infoLabel, passwordLabel, passwordField, joinButton, cancelButton);

        passwordField.SetFocus();
        app.Run(dialog);

        return result;
    }
}
