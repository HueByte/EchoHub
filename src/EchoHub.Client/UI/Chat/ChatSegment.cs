using Attribute = Terminal.Gui.Drawing.Attribute;

namespace EchoHub.Client.UI.Chat;

/// <summary>
/// A colored text segment within a chat line.
/// </summary>
public record ChatSegment(string Text, Attribute? Color);
