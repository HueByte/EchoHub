# AudioPlayerDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/AudioPlayerDialog.cs`  
> **Kind:** class

```csharp
public sealed class AudioPlayerDialog
```


AudioPlayerDialog is a sealed UI helper that renders a compact, terminal-style audio player within the application. When `Show` is invoked, it builds a `Dialog` titled "Audio Player" containing a file name header, a wave visualization area, a status label, volume controls, and playback controls (Play, Stop, Close). It also orchestrates a simple block-wave animation using `WaveBlocks` and a timer to provide a visual indication of activity, while delegating actual playback logic to the provided [`AudioPlaybackService`](../../Services/AudioPlaybackService.cs.md).

## Remarks
AudioPlayerDialog centralizes the presentation of audio playback in a terminal UI. It encapsulates the layout and styling (via `FileNameAttr`, `WaveIdleAttr`, and status attributes such as `StatusPlayingAttr`, `StatusPausedAttr`, and `StatusStoppedAttr`) so callers can surface audio without constructing the controls themselves. It collaborates with `IApplication` to host the dialog in the UI thread and with [`AudioPlaybackService`](../../Services/AudioPlaybackService.cs.md) to reflect playback state and drive the actual audio logic while the dialog handles user interactions and visuals.

## Example
```csharp
AudioPlayerDialog.Show(app, audioService, "/path/to/song.mp3", "song.mp3");
```

## Notes
- The wave visualization relies on Unicode block characters from `WaveBlocks`; ensure the terminal/font supports these glyphs for proper rendering.
- The animation is driven by a timer using `AnimationIntervalMs`; changing the cadence affects how lively the waveform appears.
- The volume UI initializes with a local `currentVolume` and the wiring between the volume controls and [`AudioPlaybackService`](../../Services/AudioPlaybackService.cs.md) is not shown in the excerpt; connect changes to the service to affect real playback.