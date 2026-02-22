using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace EchoHub.Client.UI;

public sealed class UpdateConfirmDialog
{
    public static bool Show(IApplication app, string currentVersion, string newVersion)
    {
        var confirmed = false;

        var dialog = new Dialog { Title = "Update Available", Width = 50, Height = 10 };

        var messageLabel = new Label
        {
            Text = $"A new version of EchoHub is available.\n\n  Current: {currentVersion}\n  Latest:  {newVersion}",
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 4
        };

        var updateButton = new Button
        {
            Text = "Update",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = 6
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = 6
        };

        updateButton.Accepting += (s, e) =>
        {
            confirmed = true;
            e.Handled = true;
            app.RequestStop();
        };

        cancelButton.Accepting += (s, e) =>
        {
            confirmed = false;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.Add(messageLabel, updateButton, cancelButton);
        app.Run(dialog);

        return confirmed;
    }
}
