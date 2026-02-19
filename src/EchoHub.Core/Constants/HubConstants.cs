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
    public const int MaxConsecutiveNewlines = 1;
    public const int AsciiArtWidth = 80;
    public const int AsciiArtHeight = 40;
    public const int AsciiArtHeightHalfBlock = 80;

    // Link embed constants
    public const int EmbedThumbnailWidth = 24;
    public const int EmbedThumbnailHeight = 12;
    public const int EmbedMaxDescriptionLength = 200;
    public const int EmbedMaxHtmlBytes = 64 * 1024; // 64 KB
    public const int EmbedFetchTimeoutSeconds = 3;
}
