# WelcomeBanner

> **File:** `src/EchoHub.Client/UI/Chat/WelcomeBanner.cs`  
> **Kind:** class

```csharp
internal static class WelcomeBanner
```


Renders a MOTD-style splash in the chat pane when no channel is selected — a gold-gradient ASCII logo accompanied by a version tagline and quick usage hints, evoking classic IRC greetings. Use WelcomeBanner.Build to generate the banner lines for a given viewport width and version string, then feed those lines into the chat UI.

## Remarks
WelcomeBanner encapsulates the presentation of the welcome banner: centering, padding, colorization, and the two-logo strategy are all handled here so the rest of the chat UI can simply render a sequence of lines. It selects between a full-width BigLogo and a compact SmallLogo based on the viewport width, scales the gradient across the chosen logo, and appends a version tagline plus a set of user hints. This keeps branding consistent across sizes and isolates banner-specific formatting from the broader rendering pipeline.

## Example
```csharp
var lines = WelcomeBanner.Build(80, "1.2.3");
// integrate 'lines' into the chat pane
```

## Notes
- The logo variant is chosen based on the provided width; very small panes will display SmallLogo to preserve legibility.
- The color attributes (Attributes on ChatSegment) require UI support in the chat renderer; without color support the banner falls back to plain text.