# AudioPlayerDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/AudioPlayerDialog.cs`  
> **Kind:** class

```csharp
public sealed class AudioPlayerDialog
```


AudioPlayerDialog is a sealed UI helper that presents a self-contained audio-player dialog within the application. It renders the target file name, an animated waveform visualization, a live status indicator, and playback/volume controls, all wired to an AudioPlaybackService to drive actual audio playback.

Calling Show builds a fixed-layout dialog with a filename label, a single-line wave visualization, a status line, volume controls (including a progress bar and +/- buttons), and Play/Stop/Close actions, plus a simple Unicode-block based animation that runs on a timer.

## Remarks
Encapsulates a focused piece of UI for audio playback, decoupling presentation from playback logic. The class uses themable color attributes (FileNameAttr, WaveIdleAttr, StatusPlayingAttr, StatusPausedAttr, StatusStoppedAttr, etc.) to ensure consistent theming, and a precomputed wave pattern plus a timer-driven updater to produce a lightweight, non-blocking animation while playback proceeds. It is sealed to prevent extension and to keep the UI behavior predictable across the app.

## Example
```csharp
AudioPlayerDialog.Show(app, myAudioService, "/path/to/track.mp3", "track.mp3");
```

## Notes
- The wave animation relies on Unicode block characters; render quality may vary depending on terminal/font support.
- The animation state uses a local timer and a generated wave pattern; ensure proper disposal when the dialog closes to avoid background timers.