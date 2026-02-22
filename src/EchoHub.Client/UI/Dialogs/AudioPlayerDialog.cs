using EchoHub.Client.Services;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Dialogs;

public sealed class AudioPlayerDialog
{
    // Block characters for wave animation (increasing height)
    private static readonly string[] WaveBlocks = ["\u2581", "\u2582", "\u2583", "\u2584", "\u2585", "\u2586", "\u2587", "\u2588"];
    private const int WaveBarCount = 24;
    private const int AnimationIntervalMs = 150;

    private static readonly Attribute WaveActiveAttr = new(new Color(180, 100, 255), Color.None);
    private static readonly Attribute WaveIdleAttr = new(new Color(80, 50, 120), Color.None);
    private static readonly Attribute FileNameAttr = new(new Color(180, 100, 255), Color.None);
    private static readonly Attribute StatusPlayingAttr = new(new Color(0, 200, 0), Color.None);
    private static readonly Attribute StatusPausedAttr = new(new Color(220, 180, 0), Color.None);
    private static readonly Attribute StatusStoppedAttr = new(new Color(160, 160, 160), Color.None);

    public static void Show(IApplication app, AudioPlaybackService audioService, string filePath, string fileName)
    {
        var dialog = new Dialog { Title = "Audio Player", Width = 52, Height = 14 };

        // ── File name ──
        var fileLabel = new Label
        {
            Text = $"\u266a {TruncateFileName(fileName, 44)}",
            X = 2,
            Y = 1,
            Width = Dim.Fill(2)
        };

        // ── Wave visualization ──
        var waveLabel = new Label
        {
            X = 2,
            Y = 3,
            Width = Dim.Fill(2),
            Height = 1
        };

        // ── Status label ──
        var statusLabel = new Label
        {
            Text = "Stopped",
            X = 2,
            Y = 5,
            Width = 20
        };

        // ── Volume controls ──
        var volumeHeaderLabel = new Label
        {
            Text = "Volume:",
            X = 2,
            Y = 7
        };

        byte currentVolume = 50;
        var volumeBar = new ProgressBar
        {
            X = 14,
            Y = 7,
            Width = 20,
            Height = 1,
            Fraction = currentVolume / 100f,
            ProgressBarStyle = ProgressBarStyle.Continuous
        };

        var volumePercentLabel = new Label
        {
            Text = $"{currentVolume}%",
            X = 35,
            Y = 7,
            Width = 5
        };

        var volDownButton = new Button
        {
            Text = "-",
            X = 10,
            Y = 7,
            Width = 3
        };

        var volUpButton = new Button
        {
            Text = "+",
            X = 41,
            Y = 7,
            Width = 3
        };

        // ── Playback controls ──
        var playButton = new Button
        {
            Text = "\u25b6 Play",
            X = 2,
            Y = 10,
            IsDefault = true
        };

        var stopButton = new Button
        {
            Text = "\u25a0 Stop",
            X = Pos.Right(playButton) + 2,
            Y = 10
        };

        var closeButton = new Button
        {
            Text = "Close",
            X = Pos.Right(stopButton) + 2,
            Y = 10
        };

        // ── Animation state ──
        var animationOffset = 0;
        var random = new Random();
        // Pre-generate a repeating wave pattern
        var wavePattern = new int[WaveBarCount + 8];
        for (int i = 0; i < wavePattern.Length; i++)
            wavePattern[i] = random.Next(0, WaveBlocks.Length);

        Timer? animationTimer = null;
        var isDisposed = false;

        // ── Helper functions ──
        void UpdateWave(bool isActive)
        {
            if (isDisposed) return;

            var bars = new string[WaveBarCount];
            for (int i = 0; i < WaveBarCount; i++)
            {
                if (isActive)
                {
                    var idx = wavePattern[(i + animationOffset) % wavePattern.Length];
                    bars[i] = WaveBlocks[idx];
                }
                else
                {
                    bars[i] = WaveBlocks[1]; // low idle bars
                }
            }
            waveLabel.Text = string.Join(" ", bars);
        }

        void UpdateStatus()
        {
            if (isDisposed) return;

            if (audioService.IsPlaying && !audioService.IsPaused)
            {
                statusLabel.Text = "Playing";
                playButton.Text = "\u23f8 Pause";
            }
            else if (audioService.IsPaused)
            {
                statusLabel.Text = "Paused";
                playButton.Text = "\u25b6 Resume";
            }
            else
            {
                statusLabel.Text = "Stopped";
                playButton.Text = "\u25b6 Play";
            }
        }

        void StartAnimation()
        {
            animationTimer?.Dispose();
            animationTimer = new Timer(_ =>
            {
                if (isDisposed) return;
                animationOffset++;
                // Shuffle a few bars each tick for organic movement
                var idx = random.Next(0, wavePattern.Length);
                wavePattern[idx] = random.Next(0, WaveBlocks.Length);

                app.Invoke(() =>
                {
                    if (isDisposed) return;
                    UpdateWave(true);
                });
            }, null, 0, AnimationIntervalMs);
        }

        void StopAnimation()
        {
            animationTimer?.Dispose();
            animationTimer = null;
            if (!isDisposed)
                UpdateWave(false);
        }

        async Task UpdateVolume(byte newVolume)
        {
            currentVolume = Math.Clamp(newVolume, (byte)0, (byte)100);
            await audioService.SetVolumeAsync(currentVolume);
            if (!isDisposed)
            {
                volumeBar.Fraction = currentVolume / 100f;
                volumePercentLabel.Text = $"{currentVolume}%";
            }
        }

        // ── Custom drawing for colored elements ──
        fileLabel.DrawingContent += (s, e) =>
        {
            var normalAttr = fileLabel.GetAttributeForRole(VisualRole.Normal);
            var resolvedAttr = FileNameAttr.Background == Color.None
                ? FileNameAttr with { Background = normalAttr.Background }
                : FileNameAttr;
            fileLabel.SetAttribute(resolvedAttr);
            fileLabel.Move(0, 0);
            var text = fileLabel.Text ?? "";
            foreach (var g in Terminal.Gui.Drawing.GraphemeHelper.GetGraphemes(text))
                fileLabel.AddStr(g);
            // Fill remaining width
            var width = fileLabel.Viewport.Width;
            var textCols = Terminal.Gui.Text.StringExtensions.GetColumns(text);
            for (int i = textCols; i < width; i++)
                fileLabel.AddStr(" ");
            e.Cancel = true;
        };

        waveLabel.DrawingContent += (s, e) =>
        {
            var normalAttr = waveLabel.GetAttributeForRole(VisualRole.Normal);
            var attr = (audioService.IsPlaying && !audioService.IsPaused) ? WaveActiveAttr : WaveIdleAttr;
            var resolvedAttr = attr.Background == Color.None
                ? attr with { Background = normalAttr.Background }
                : attr;
            waveLabel.SetAttribute(resolvedAttr);
            waveLabel.Move(0, 0);
            var text = waveLabel.Text ?? "";
            foreach (var g in Terminal.Gui.Drawing.GraphemeHelper.GetGraphemes(text))
                waveLabel.AddStr(g);
            var width = waveLabel.Viewport.Width;
            var textCols = Terminal.Gui.Text.StringExtensions.GetColumns(text);
            for (int i = textCols; i < width; i++)
                waveLabel.AddStr(" ");
            e.Cancel = true;
        };

        statusLabel.DrawingContent += (s, e) =>
        {
            var normalAttr = statusLabel.GetAttributeForRole(VisualRole.Normal);
            Attribute attr;
            if (audioService.IsPlaying && !audioService.IsPaused)
                attr = StatusPlayingAttr;
            else if (audioService.IsPaused)
                attr = StatusPausedAttr;
            else
                attr = StatusStoppedAttr;

            var resolvedAttr = attr.Background == Color.None
                ? attr with { Background = normalAttr.Background }
                : attr;
            statusLabel.SetAttribute(resolvedAttr);
            statusLabel.Move(0, 0);
            var text = statusLabel.Text ?? "";
            foreach (var g in Terminal.Gui.Drawing.GraphemeHelper.GetGraphemes(text))
                statusLabel.AddStr(g);
            var width = statusLabel.Viewport.Width;
            var textCols = Terminal.Gui.Text.StringExtensions.GetColumns(text);
            for (int i = textCols; i < width; i++)
                statusLabel.AddStr(" ");
            e.Cancel = true;
        };

        // ── Event handlers ──
        playButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            Task.Run(async () =>
            {
                if (audioService.IsPaused)
                {
                    await audioService.ResumeAsync();
                    app.Invoke(() =>
                    {
                        UpdateStatus();
                        StartAnimation();
                    });
                }
                else if (audioService.IsPlaying)
                {
                    await audioService.PauseAsync();
                    app.Invoke(() =>
                    {
                        UpdateStatus();
                        StopAnimation();
                    });
                }
                else
                {
                    await audioService.SetVolumeAsync(currentVolume);
                    await audioService.PlayAsync(filePath);
                    app.Invoke(() =>
                    {
                        UpdateStatus();
                        StartAnimation();
                    });
                }
            });
        };

        stopButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            Task.Run(async () =>
            {
                await audioService.StopAsync();
                app.Invoke(() =>
                {
                    UpdateStatus();
                    StopAnimation();
                });
            });
        };

        closeButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            isDisposed = true;
            animationTimer?.Dispose();
            _ = audioService.StopAsync(); // fire-and-forget
            app.RequestStop();
        };

        volDownButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            var newVol = (byte)Math.Max(0, currentVolume - 10);
            Task.Run(async () =>
            {
                await UpdateVolume(newVol);
                app.Invoke(() =>
                {
                    volumeBar.SetNeedsDraw();
                    volumePercentLabel.SetNeedsDraw();
                });
            });
        };

        volUpButton.Accepting += (s, e) =>
        {
            e.Handled = true;
            var newVol = (byte)Math.Min(100, currentVolume + 10);
            Task.Run(async () =>
            {
                await UpdateVolume(newVol);
                app.Invoke(() =>
                {
                    volumeBar.SetNeedsDraw();
                    volumePercentLabel.SetNeedsDraw();
                });
            });
        };

        // Handle playback finishing naturally
        EventHandler? finishedHandler = null;
        finishedHandler = (s, e) =>
        {
            app.Invoke(() =>
            {
                if (isDisposed) return;
                UpdateStatus();
                StopAnimation();
            });
        };
        audioService.PlaybackFinished += finishedHandler;

        // ── Initial state ──
        UpdateWave(false);
        UpdateStatus();

        dialog.Add(fileLabel, waveLabel, statusLabel,
            volumeHeaderLabel, volDownButton, volumeBar, volumePercentLabel, volUpButton,
            playButton, stopButton, closeButton);

        playButton.SetFocus();
        app.Run(dialog);

        // Cleanup
        isDisposed = true;
        animationTimer?.Dispose();
        audioService.PlaybackFinished -= finishedHandler;
    }

    private static string TruncateFileName(string name, int maxLen)
    {
        if (name.Length <= maxLen)
            return name;

        var ext = Path.GetExtension(name);
        var stem = Path.GetFileNameWithoutExtension(name);
        var available = maxLen - ext.Length - 3; // 3 for "..."
        if (available < 1)
            return name[..maxLen];

        return stem[..available] + "..." + ext;
    }
}
