# AudioPlayerDialog

> **File:** `src/EchoHub.Client/UI/Dialogs/AudioPlayerDialog.cs`  
> **Kind:** class

```csharp
public sealed class AudioPlayerDialog
```


AudioPlayerDialog is a sealed class that presents a modal Audio Player UI within the application's terminal UI. It assembles a compact layout with the current file name, a wave-like block visualization, playback status, and simple volume and playback controls, all exposed via a single Show method that binds an IApplication and an AudioPlaybackService to the dialog's lifecycle.

## Remarks
By encapsulating layout, colors, and animation in one place, it provides a reusable, cohesive UX for audio playback that can be dropped into different screens without duplicating UI code. The class relies on themed attributes (e.g. WaveActiveAttr, WaveIdleAttr, FileNameAttr, Status*Attr) to ensure consistent appearance, and uses a timer-driven animation loop to render the wave pattern while playback is active.

## Example
```csharp
AudioPlayerDialog.Show(app, audioService, "/path/to/song.mp3", "song.mp3");
```

## Notes
- The waveform visualization uses Unicode block characters; ensure your terminal font supports these glyphs for correct rendering.
- The dialog starts a background animation timer; dispose the dialog or stop the timer to avoid leaks when closing.