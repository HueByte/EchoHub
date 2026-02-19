using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Drawing;
using EchoHub.Core.DTOs;
using EchoHub.Core.Models;
using EchoHub.Client.Config;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI;

/// <summary>
/// Action selected by the user in the user panel dialog.
/// </summary>
public enum UserPanelAction
{
    Close,
    EditProfile,
    SetStatus
}

/// <summary>
/// A Terminal.Gui dialog for viewing the user panel -- profile info, saved servers, and status.
/// </summary>
public sealed class UserPanelDialog
{
    /// <summary>
    /// Shows the user panel dialog and returns the action the user selected.
    /// </summary>
    public static UserPanelAction Show(
        IApplication app,
        UserProfileDto? profile,
        List<SavedServer> savedServers,
        UserStatus currentStatus,
        string? currentStatusMessage)
    {
        var action = UserPanelAction.Close;

        var dialog = new Dialog { Title = "User Panel", Width = 70, Height = 24 };

        // -- Left side: Profile info ------------------------------------------
        var profileFrame = new FrameView
        {
            Title = "Profile",
            X = 0,
            Y = 0,
            Width = 35,
            Height = Dim.Fill(3)
        };

        int row = 0;

        // Username
        var usernameLabel = new Label
        {
            Text = "Username:",
            X = 1,
            Y = row
        };
        var usernameValue = new Label
        {
            Text = profile?.Username ?? "N/A",
            X = 12,
            Y = row
        };
        usernameValue.SetScheme(new Scheme
        {
            Normal = new Attribute(Color.BrightYellow, Color.Blue)
        });
        profileFrame.Add(usernameLabel, usernameValue);
        row += 1;

        // Display Name
        var displayLabel = new Label
        {
            Text = "Name:",
            X = 1,
            Y = row
        };
        var displayValue = new Label
        {
            Text = profile?.DisplayName ?? "-",
            X = 12,
            Y = row
        };
        profileFrame.Add(displayLabel, displayValue);
        row += 1;

        // Status
        var statusLabel = new Label
        {
            Text = "Status:",
            X = 1,
            Y = row
        };
        var statusText = FormatStatus(currentStatus);
        var statusValue = new Label
        {
            Text = statusText,
            X = 12,
            Y = row
        };
        statusValue.SetScheme(new Scheme
        {
            Normal = new Attribute(GetStatusColor(currentStatus), Color.Blue)
        });
        profileFrame.Add(statusLabel, statusValue);
        row += 1;

        // Status Message
        if (!string.IsNullOrWhiteSpace(currentStatusMessage))
        {
            var msgLabel = new Label
            {
                Text = "Message:",
                X = 1,
                Y = row
            };
            var msgValue = new Label
            {
                Text = Truncate(currentStatusMessage, 20),
                X = 12,
                Y = row
            };
            profileFrame.Add(msgLabel, msgValue);
            row += 1;
        }

        // Bio
        row += 1;
        var bioLabel = new Label
        {
            Text = "Bio:",
            X = 1,
            Y = row
        };
        profileFrame.Add(bioLabel);
        row += 1;

        var bioText = profile?.Bio ?? "-";
        var bioView = new TextView()
        {
            X = 1,
            Y = row,
            Width = Dim.Fill(1),
            Height = 3,
            Text = bioText,
            ReadOnly = true,
            WordWrap = true
        };
        bioView.SetScheme(new Scheme
        {
            Normal = new Attribute(Color.White, Color.DarkGray),
            Focus = new Attribute(Color.White, Color.DarkGray)
        });
        profileFrame.Add(bioView);
        row += 3;

        // Color
        var colorLabel = new Label
        {
            Text = "Color:",
            X = 1,
            Y = row
        };
        var colorValue = new Label
        {
            Text = profile?.NicknameColor ?? "-",
            X = 12,
            Y = row
        };
        profileFrame.Add(colorLabel, colorValue);
        row += 1;

        // ASCII Avatar
        if (!string.IsNullOrWhiteSpace(profile?.AvatarAscii))
        {
            row += 1;
            var avatarFrame = new FrameView
            {
                Title = "Avatar",
                X = 1,
                Y = row,
                Width = Dim.Fill(1),
                Height = 4
            };
            var avatarLabel = new Label
            {
                Text = profile.AvatarAscii,
                X = 0,
                Y = 0
            };
            avatarFrame.Add(avatarLabel);
            profileFrame.Add(avatarFrame);
        }

        dialog.Add(profileFrame);

        // -- Right side: Saved Servers ----------------------------------------
        var serversFrame = new FrameView
        {
            Title = "Saved Servers",
            X = 36,
            Y = 0,
            Width = Dim.Fill(1),
            Height = Dim.Fill(3)
        };

        var serverNames = savedServers.Select(s => s.Name).ToList();
        var serverList = new ListView
        {
            Source = new ListWrapper<string>(new ObservableCollection<string>(serverNames)),
            X = 0,
            Y = 0,
            Width = Dim.Fill(0),
            Height = Dim.Fill(4)
        };

        var serverUrlLabel = new Label
        {
            Text = "URL: -",
            X = 0,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(0)
        };
        var serverLastLabel = new Label
        {
            Text = "Last: -",
            X = 0,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(0)
        };
        var serverUserLabel = new Label
        {
            Text = "User: -",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(0)
        };

        serverList.ValueChanged += (sender, e) =>
        {
            var index = e.NewValue;
            if (index.HasValue && index.Value >= 0 && index.Value < savedServers.Count)
            {
                var server = savedServers[index.Value];
                serverUrlLabel.Text = $"URL:  {Truncate(server.Url, 25)}";
                serverLastLabel.Text = $"Last: {server.LastConnected:yyyy-MM-dd HH:mm}";
                serverUserLabel.Text = $"User: {server.Username ?? "-"}";
            }
        };

        // Show initial details if there are servers
        if (savedServers.Count > 0)
        {
            var first = savedServers[0];
            serverUrlLabel.Text = $"URL:  {Truncate(first.Url, 25)}";
            serverLastLabel.Text = $"Last: {first.LastConnected:yyyy-MM-dd HH:mm}";
            serverUserLabel.Text = $"User: {first.Username ?? "-"}";
        }

        serversFrame.Add(serverList, serverUrlLabel, serverLastLabel, serverUserLabel);
        dialog.Add(serversFrame);

        // -- Bottom buttons ---------------------------------------------------
        var editProfileButton = new Button
        {
            Text = "Edit Profile",
            X = Pos.Center() - 22,
            Y = Pos.AnchorEnd(2)
        };

        var setStatusButton = new Button
        {
            Text = "Set Status",
            X = Pos.Center() - 5,
            Y = Pos.AnchorEnd(2)
        };

        var closeButton = new Button
        {
            Text = "Close",
            IsDefault = true,
            X = Pos.Center() + 12,
            Y = Pos.AnchorEnd(2)
        };

        editProfileButton.Accepting += (s, e) =>
        {
            action = UserPanelAction.EditProfile;
            e.Handled = true;
            app.RequestStop();
        };

        setStatusButton.Accepting += (s, e) =>
        {
            action = UserPanelAction.SetStatus;
            e.Handled = true;
            app.RequestStop();
        };

        closeButton.Accepting += (s, e) =>
        {
            action = UserPanelAction.Close;
            e.Handled = true;
            app.RequestStop();
        };

        dialog.Add(editProfileButton, setStatusButton, closeButton);

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

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength - 3), "...");
}
