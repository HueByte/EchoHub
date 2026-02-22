namespace EchoHub.Core.Constants;

public static class HubConstants
{
    public const string ChatHubPath = "/hubs/chat";
    public const string DefaultChannel = "general";
    public const int DefaultHistoryCount = 100;
    public const int MaxMessageLength = 2000;
    public const int MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
    public const int MaxAudioFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    public const int MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
    public const int MaxAvatarSizeBytes = 2 * 1024 * 1024; // 2 MB
    public const int MaxMessageNewlines = 30;
    public const int MaxConsecutiveNewlines = 1;
    public const int AsciiArtWidth = 80;
    public const int AsciiArtHeight = 40;
    public const int AsciiArtHeightHalfBlock = 80;

    // Link embed constants
    public const int EmbedMaxDescriptionLength = 500;
    public const int EmbedMaxHtmlBytes = 64 * 1024; // 64 KB
    public const int EmbedFetchTimeoutSeconds = 5;
    public const int EmbedMaxUrlsPerMessage = 3;
}
