using EchoHub.Client.UI.Helpers;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Dialogs;

/// <summary>
/// Result returned from the profile edit dialog.
/// </summary>
public record ProfileEditResult(string? DisplayName, string? Bio, string? NicknameColor, string? AvatarPath, bool? NotificationSoundEnabled, byte? NotificationVolume);

/// <summary>
/// A Terminal.Gui dialog for editing the user's profile (display name, bio, nickname color).
/// </summary>
public sealed class ProfileEditDialog
{
    /// <summary>
    /// Shows the profile edit dialog and returns the result, or null if cancelled.
    /// </summary>
    public static ProfileEditResult? Show(IApplication app, string? currentDisplayName, string? currentBio, string? currentColor, bool notificationSoundEnabled = false, byte notificationVolume = 30)
    {
        ProfileEditResult? result = null;

        var dialog = new Dialog { Title = "Edit Profile", Width = 60, Height = 26 };

        // Display Name
        var nameLabel = new Label
        {
            Text = "Display Name:",
            X = 1,
            Y = 1
        };
        var nameField = new TextField
        {
            Text = currentDisplayName ?? "",
            X = 17,
            Y = 1,
            Width = Dim.Fill(2)
        };

        // Bio
        var bioLabel = new Label
        {
            Text = "Bio:",
            X = 1,
            Y = 3
        };
        var bioField = new TextField
        {
            Text = currentBio ?? "",
            X = 17,
            Y = 3,
            Width = Dim.Fill(2)
        };

        // Nickname Color
        var colorLabel = new Label
        {
            Text = "Nickname Color:",
            X = 1,
            Y = 5
        };
        var colorField = new TextField
        {
            Text = currentColor ?? "",
            X = 17,
            Y = 5,
            Width = Dim.Fill(2)
        };

        var colorHintLabel = new Label
        {
            Text = "(hex e.g. #FF5733)",
            X = 17,
            Y = 6
        };
        colorHintLabel.SetScheme(new Scheme
        {
            Normal = new Attribute(Color.DarkGray, Color.Blue)
        });

        // Color Preview
        var previewLabel = new Label
        {
            Text = "Preview:",
            X = 1,
            Y = 8
        };
        var colorPreview = new Label
        {
            Text = "\u2588\u2588\u2588\u2588\u2588\u2588",
            X = 17,
            Y = 8
        };

        UpdateColorPreview(colorPreview, colorField.Text);

        colorField.TextChanged += (sender, e) =>
        {
            UpdateColorPreview(colorPreview, colorField.Text);
        };

        // Avatar
        var avatarLabel = new Label
        {
            Text = "Avatar:",
            X = 1,
            Y = 10
        };
        var avatarField = new TextField
        {
            Text = "",
            X = 17,
            Y = 10,
            Width = Dim.Fill(12)
        };
        var browseButton = new Button
        {
            Text = "Browse",
            X = Pos.AnchorEnd(10),
            Y = 10
        };
        var avatarHintLabel = new Label
        {
            Text = "(file path or URL)",
            X = 17,
            Y = 11
        };
        avatarHintLabel.SetScheme(new Scheme
        {
            Normal = new Attribute(Color.DarkGray, Color.Blue)
        });

        browseButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            var openDialog = new OpenDialog
            {
                Title = "Select Avatar Image",
                OpenMode = OpenMode.File,
            };
            app.Run(openDialog);
            if (openDialog.FilePaths.Count > 0)
            {
                avatarField.Text = openDialog.FilePaths[0];
            }
        };

        // Notification Sound
        var notifCheckbox = new CheckBox
        {
            Text = "Notification sound on @mention",
            X = 1,
            Y = 13,
            Value = notificationSoundEnabled ? CheckState.Checked : CheckState.UnChecked
        };

        var volumeLabel = new Label
        {
            Text = "Volume:",
            X = 1,
            Y = 15
        };
        var volumeField = new TextField
        {
            Text = notificationVolume.ToString(),
            X = 17,
            Y = 15,
            Width = 6
        };
        var volumeHintLabel = new Label
        {
            Text = "(0-100)",
            X = 24,
            Y = 15
        };
        volumeHintLabel.SetScheme(new Scheme
        {
            Normal = new Attribute(Color.DarkGray, Color.Blue)
        });

        // Buttons
        var saveButton = new Button
        {
            Text = "Save",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = 18
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = 18
        };

        saveButton.Accepting += (s, e) =>
        {
            var displayName = NullIfEmpty(nameField.Text?.Trim());
            var bio = NullIfEmpty(bioField.Text?.Trim());
            var nicknameColor = NullIfEmpty(colorField.Text?.Trim());
            var avatarPath = NullIfEmpty(avatarField.Text?.Trim());

            byte? volume = byte.TryParse(volumeField.Text, out var v) ? Math.Min(v, (byte)100) : null;
            result = new ProfileEditResult(displayName, bio, nicknameColor, avatarPath, notifCheckbox.Value == CheckState.Checked, volume);
            e.Handled = true;
            app.RequestStop();
        };

        cancelButton.Accepting += (s, e) =>
        {
            result = null;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.Add(nameLabel, nameField, bioLabel, bioField, colorLabel, colorField,
            colorHintLabel, previewLabel, colorPreview,
            avatarLabel, avatarField, browseButton, avatarHintLabel,
            notifCheckbox, volumeLabel, volumeField, volumeHintLabel,
            saveButton, cancelButton);

        nameField.SetFocus();
        app.Run(dialog);

        return result;
    }

    /// <summary>
    /// Attempts to parse a hex color string and update the preview label color.
    /// </summary>
    private static void UpdateColorPreview(Label preview, string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            preview.SetScheme(new Scheme
            {
                Normal = new Attribute(Color.White, Color.Blue)
            });
            return;
        }

        var color = HexColorHelper.ParseHexToColor(hexColor.Trim(), Color.White);
        preview.SetScheme(new Scheme
        {
            Normal = new Attribute(color, Color.Blue)
        });
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
