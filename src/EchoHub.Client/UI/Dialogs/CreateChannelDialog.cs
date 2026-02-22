using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace EchoHub.Client.UI;

public record CreateChannelResult(string Name, string? Topic, bool IsPublic);

public sealed class CreateChannelDialog
{
    public static CreateChannelResult? Show(IApplication app)
    {
        CreateChannelResult? result = null;

        var dialog = new Dialog { Title = "Create Channel", Width = 50, Height = 14 };

        var nameLabel = new Label { Text = "Name:", X = 1, Y = 1 };
        var nameField = new TextField { X = 10, Y = 1, Width = Dim.Fill(2) };

        var topicLabel = new Label { Text = "Topic:", X = 1, Y = 3 };
        var topicField = new TextField { X = 10, Y = 3, Width = Dim.Fill(2) };

        var publicCheckbox = new CheckBox
        {
            Text = "Public (visible to all users)",
            X = 1,
            Y = 5,
            Value = CheckState.Checked
        };

        var hintLabel = new Label
        {
            Text = "Lowercase letters, digits, hyphens, underscores (2-100 chars)",
            X = 1,
            Y = 7,
        };

        var createButton = new Button
        {
            Text = "Create",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = 9
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = 9
        };

        createButton.Accepting += (s, e) =>
        {
            var name = nameField.Text?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.ErrorQuery(app, "Error", "Channel name is required.", "OK");
                return;
            }

            var topic = topicField.Text?.Trim();
            if (string.IsNullOrWhiteSpace(topic))
                topic = null;

            var isPublic = publicCheckbox.Value == CheckState.Checked;
            result = new CreateChannelResult(name, topic, isPublic);
            e.Handled = true;
            app.RequestStop();
        };

        cancelButton.Accepting += (s, e) =>
        {
            result = null;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.Add(nameLabel, nameField, topicLabel, topicField, publicCheckbox, hintLabel, createButton, cancelButton);

        nameField.SetFocus();
        app.Run(dialog);

        return result;
    }
}
