namespace EchoHub.Core.Models;

/// <summary>
/// The kind of a message attachment. Determines how the client renders it
/// (ASCII preview for images, a play affordance for audio, a download line for files).
/// </summary>
public enum AttachmentKind
{
    Image,
    Audio,
    File
}
