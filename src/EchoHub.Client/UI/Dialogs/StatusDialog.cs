using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using EchoHub.Core.Models;

namespace EchoHub.Client.UI;

/// <summary>
/// Result returned from the status dialog.
/// </summary>
public record StatusDialogResult(UserStatus Status, string? StatusMessage);

/// <summary>
/// A Terminal.Gui dialog for setting the user's status and status message.
/// </summary>
public sealed class StatusDialog
{
    /// <summary>
    /// Shows the status dialog and returns the result, or null if cancelled.
    /// </summary>
    public static StatusDialogResult? Show(IApplication app, UserStatus currentStatus, string? currentMessage)
    {
        StatusDialogResult? result = null;

        var dialog = new Dialog { Title = "Set Status", Width = 50, Height = 12 };

        var statusLabel = new Label
        {
            Text = "Status:",
            X = 1,
            Y = 1
        };

        var optionSelector = new OptionSelector<UserStatus>
        {
            X = 12,
            Y = 1
        };
        optionSelector.Value = currentStatus;

        var messageLabel = new Label
        {
            Text = "Message:",
            X = 1,
            Y = 6
        };
        var messageField = new TextField
        {
            Text = currentMessage ?? "",
            X = 12,
            Y = 6,
            Width = Dim.Fill(2)
        };

        var saveButton = new Button
        {
            Text = "Save",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = 8
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = 8
        };

        saveButton.Accepting += (s, e) =>
        {
            var status = optionSelector.Value ?? UserStatus.Online;
            var message = messageField.Text?.Trim();
            if (string.IsNullOrWhiteSpace(message))
                message = null;

            result = new StatusDialogResult(status, message);
            e.Handled = true;
            app.RequestStop();
        };

        cancelButton.Accepting += (s, e) =>
        {
            result = null;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.Add(statusLabel, optionSelector, messageLabel, messageField, saveButton, cancelButton);

        optionSelector.SetFocus();
        app.Run(dialog);

        return result;
    }
}
