namespace EchoHub.Server.Config;

/// <summary>
/// Anti-spam thresholds, bound from the "Spam" config section. Defaults are lenient enough
/// that a fast typist never trips them; Mods and above are always exempt.
/// </summary>
public sealed class SpamOptions
{
    /// <summary>Master switch for all spam protection.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Max messages a user may send per <see cref="WindowSeconds"/> window.</summary>
    public int MaxMessagesPerWindow { get; set; } = 8;

    public int WindowSeconds { get; set; } = 5;

    /// <summary>
    /// How many identical messages in a row are tolerated; the next one is rejected.
    /// (End-to-end encrypted rooms are naturally exempt — identical plaintext produces
    /// different ciphertext per message, so repeats never look identical to the server.)
    /// </summary>
    public int MaxDuplicateMessages { get; set; } = 3;

    /// <summary>
    /// Auto-mute duration once a user accumulates <see cref="ViolationThreshold"/> rejected
    /// sends within <see cref="ViolationWindowMinutes"/>. 0 disables auto-mute (rejections
    /// still apply). The mute uses the normal timed-mute machinery, so moderators see it
    /// and <c>MuteExpirationService</c> lifts it.
    /// </summary>
    public int AutoMuteMinutes { get; set; } = 5;

    public int ViolationThreshold { get; set; } = 5;

    public int ViolationWindowMinutes { get; set; } = 5;

    /// <summary>
    /// Max *first-time* channel joins per <see cref="JoinWindowSeconds"/> window. Joins of
    /// channels the user already belongs to (reconnect/auto-join) never count, but a brand-new
    /// user's first connect joins every public channel at once — keep this above your public
    /// channel count.
    /// </summary>
    public int MaxJoinsPerWindow { get; set; } = 25;

    public int JoinWindowSeconds { get; set; } = 30;

    /// <summary>Max channel creations per <see cref="ChannelCreateWindowMinutes"/> window.</summary>
    public int MaxChannelCreatesPerWindow { get; set; } = 3;

    public int ChannelCreateWindowMinutes { get; set; } = 10;
}
