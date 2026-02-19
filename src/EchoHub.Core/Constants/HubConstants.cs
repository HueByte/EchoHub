namespace EchoHub.Core.Constants;

public static class HubConstants
{
    public const string ChatHubPath = "/hubs/chat";
    public const string DefaultChannel = "general";
    public const int DefaultHistoryCount = 100;
    public const int MaxMessageLength = 2000;
    public const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    public const int MaxAvatarSizeBytes = 2 * 1024 * 1024; // 2 MB
    public const int MaxMessageNewlines = 30;
    public const int MaxConsecutiveNewlines = 2;
    public const int AsciiArtWidth = 80;
    public const int AsciiArtHeight = 40;
    public const int AsciiArtHeightHalfBlock = 80;
}
