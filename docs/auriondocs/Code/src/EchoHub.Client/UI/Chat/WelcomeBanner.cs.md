# WelcomeBanner

> **File:** `src/EchoHub.Client/UI/Chat/WelcomeBanner.cs`  
> **Kind:** class

```csharp
internal static class WelcomeBanner
```


The `WelcomeBanner` class provides the MOTD-style splash shown in the chat pane when no channel is selected. It renders a gold-gradient ASCII logo by choosing between `BigLogo` (for wider viewports) and `SmallLogo` (for narrow panes), centers the logo within the given width, and appends a version tagline and quick-use hints. The static `Build` method returns a list of [`ChatLine`](ChatLine.cs.md) objects that the UI can render to display the branded welcome banner for a given `width` and `version` string.

The banner is designed to be self-contained: it composes ASCII art, a vertical color gradient (`Gradient`), and a small set of hints (`Hints`) into a sequence of renderable lines. This keeps the welcome experience consistent across sessions and isolates branding concerns from the main channel rendering logic.
