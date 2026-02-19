using System.Text.RegularExpressions;

namespace EchoHub.Core.Constants;

public static partial class ValidationConstants
{
    public const string UsernamePattern = @"^[a-zA-Z0-9_-]{3,50}$";
    public const string ChannelNamePattern = @"^[a-zA-Z0-9_-]{2,100}$";
    public const string HexColorPattern = @"^#[0-9a-fA-F]{6}$";

    public const int MaxPasswordLength = 128;
    public const int MaxDisplayNameLength = 100;
    public const int MaxBioLength = 500;
    public const int MaxStatusMessageLength = 100;
    public const int MaxChannelTopicLength = 500;
    public const int MaxHistoryCount = 100;

    [GeneratedRegex(UsernamePattern)]
    public static partial Regex UsernameRegex();

    [GeneratedRegex(ChannelNamePattern)]
    public static partial Regex ChannelNameRegex();

    [GeneratedRegex(HexColorPattern)]
    public static partial Regex HexColorRegex();
}
