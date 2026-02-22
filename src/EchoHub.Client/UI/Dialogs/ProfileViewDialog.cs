using EchoHub.Client.UI.Chat;
using EchoHub.Client.UI.Helpers;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Dialogs;

/// <summary>
/// Action selected by the user in their own profile dialog.
/// </summary>
public enum ProfileAction
{
    Close,
    EditProfile,
    SetStatus
}

/// <summary>
/// Dialog for viewing a user's server profile.
/// Shows Edit Profile / Set Status buttons when viewing own profile.
/// </summary>
public sealed class ProfileViewDialog
{
    /// <summary>
    /// Show a read-only profile view for another user.
    /// </summary>
    public static void Show(IApplication app, UserProfileDto? profile)
    {
        ShowInternal(app, profile, isOwnProfile: false);
    }

    /// <summary>
    /// Show the profile view for the current user with action buttons.
    /// Returns the action the user selected.
    /// </summary>
    public static ProfileAction ShowOwn(
        IApplication app,
        UserProfileDto? profile,
        UserStatus currentStatus,
        string? currentStatusMessage)
    {
        return ShowInternal(app, profile, isOwnProfile: true, currentStatus, currentStatusMessage);
    }

    private static ProfileAction ShowInternal(
        IApplication app,
        UserProfileDto? profile,
        bool isOwnProfile,
        UserStatus? currentStatus = null,
        string? currentStatusMessage = null)
    {
        if (profile is null)
        {
            MessageBox.ErrorQuery(app, "Profile", "User not found.", "OK");
            return ProfileAction.Close;
        }

        var action = ProfileAction.Close;

        var dialog = new Dialog
        {
            Title = isOwnProfile ? "My Profile" : $"Profile \u2014 {profile.Username}",
            Width = 50,
            Height = 20
        };

        int row = 0;

        // Username
        var usernameLabel = new Label { Text = "Username:", X = 1, Y = row };
        var usernameValue = new Label { Text = profile.Username, X = 14, Y = row };
        usernameValue.SetScheme(new Scheme
        {
            Normal = new Attribute(Color.BrightYellow, Color.Blue)
        });
        dialog.Add(usernameLabel, usernameValue);
        row++;

        // Display Name
        var nameLabel = new Label { Text = "Name:", X = 1, Y = row };
        var nameValue = new Label { Text = profile.DisplayName ?? "-", X = 14, Y = row };
        dialog.Add(nameLabel, nameValue);
        row++;

        // Status — use live status for own profile, stored status for others
        var displayStatus = isOwnProfile && currentStatus.HasValue ? currentStatus.Value : profile.Status;
        var displayStatusMsg = isOwnProfile ? currentStatusMessage : profile.StatusMessage;

        var statusLabel = new Label { Text = "Status:", X = 1, Y = row };
        var statusText = FormatStatus(displayStatus);
        var statusValue = new Label { Text = statusText, X = 14, Y = row };
        statusValue.SetScheme(new Scheme
        {
            Normal = new Attribute(GetStatusColor(displayStatus), Color.Blue)
        });
        dialog.Add(statusLabel, statusValue);
        row++;

        // Status Message
        if (!string.IsNullOrWhiteSpace(displayStatusMsg))
        {
            var msgLabel = new Label { Text = "Message:", X = 1, Y = row };
            var msgValue = new Label { Text = displayStatusMsg, X = 14, Y = row, Width = Dim.Fill(2) };
            dialog.Add(msgLabel, msgValue);
            row++;
        }

        // Color
        var colorLabel = new Label { Text = "Color:", X = 1, Y = row };
        var colorValue = new Label { Text = profile.NicknameColor ?? "-", X = 14, Y = row };
        if (HexColorHelper.ParseHexColor(profile.NicknameColor) is { } colorAttr)
            colorValue.SetScheme(new Scheme { Normal = colorAttr });
        dialog.Add(colorLabel, colorValue);
        row++;

        // Bio
        row++;
        var bioLabel = new Label { Text = "Bio:", X = 1, Y = row };
        dialog.Add(bioLabel);
        row++;

        var bioView = new TextView
        {
            X = 1,
            Y = row,
            Width = Dim.Fill(2),
            Height = 3,
            Text = profile.Bio ?? "-",
            ReadOnly = true,
            WordWrap = true
        };
        bioView.SetScheme(new Scheme
        {
            Normal = new Attribute(Color.White, Color.DarkGray),
            Focus = new Attribute(Color.White, Color.DarkGray)
        });
        dialog.Add(bioView);
        row += 3;

        // ASCII Avatar — render with color tags
        if (!string.IsNullOrWhiteSpace(profile.AvatarAscii))
        {
            row++;
            var rawLines = profile.AvatarAscii.Split('\n');
            var avatarSource = new ChatListSource();
            foreach (var line in rawLines)
            {
                avatarSource.Add(ChatLine.HasColorTags(line)
                    ? ChatLine.FromColoredText(line)
                    : new ChatLine(line));
            }

            var avatarHeight = Math.Min(rawLines.Length + 2, 24);
            var avatarFrame = new FrameView
            {
                Title = "Avatar",
                X = 1,
                Y = row,
                Width = Dim.Fill(2),
                Height = avatarHeight
            };
            var avatarList = new ListView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Source = avatarSource
            };
            avatarFrame.Add(avatarList);
            dialog.Add(avatarFrame);

            // Grow dialog to fit avatar + widen for art
            var artWidth = rawLines.Max(l => ChatLine.HasColorTags(l)
                ? ChatLine.FromColoredText(l).TextLength
                : l.Length);
            dialog.Width = Math.Max(50, artWidth + 6);
            dialog.Height = row + avatarHeight + 4;
        }

        // Buttons
        if (isOwnProfile)
        {
            var editButton = new Button
            {
                Text = "Edit Profile",
                X = Pos.Center() - 20,
                Y = Pos.AnchorEnd(2)
            };
            editButton.Accepting += (s, e) =>
            {
                action = ProfileAction.EditProfile;
                e.Handled = true;
                app.RequestStop();
            };

            var statusButton = new Button
            {
                Text = "Set Status",
                X = Pos.Center() - 4,
                Y = Pos.AnchorEnd(2)
            };
            statusButton.Accepting += (s, e) =>
            {
                action = ProfileAction.SetStatus;
                e.Handled = true;
                app.RequestStop();
            };

            var closeButton = new Button
            {
                Text = "Close",
                IsDefault = true,
                X = Pos.Center() + 13,
                Y = Pos.AnchorEnd(2)
            };
            closeButton.Accepting += (s, e) =>
            {
                action = ProfileAction.Close;
                e.Handled = true;
                app.RequestStop();
            };

            dialog.Add(editButton, statusButton, closeButton);
        }
        else
        {
            var closeButton = new Button
            {
                Text = "Close",
                IsDefault = true,
                X = Pos.Center(),
                Y = Pos.AnchorEnd(2)
            };
            closeButton.Accepting += (s, e) =>
            {
                e.Handled = true;
                app.RequestStop();
            };
            dialog.Add(closeButton);
        }

        app.Run(dialog);
        return action;
    }

    private static string FormatStatus(UserStatus status) => status switch
    {
        UserStatus.Online => "\u25cf Online",
        UserStatus.Away => "\u25cf Away",
        UserStatus.DoNotDisturb => "\u25cf Do Not Disturb",
        UserStatus.Invisible => "\u25cb Invisible",
        _ => "\u25cf Unknown"
    };

    private static Color GetStatusColor(UserStatus status) => status switch
    {
        UserStatus.Online => Color.BrightGreen,
        UserStatus.Away => Color.BrightYellow,
        UserStatus.DoNotDisturb => Color.BrightRed,
        UserStatus.Invisible => Color.Gray,
        _ => Color.White
    };
}
