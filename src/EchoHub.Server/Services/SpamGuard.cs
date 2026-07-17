using EchoHub.Core.Models;
using EchoHub.Server.Config;

namespace EchoHub.Server.Services;

public enum SpamVerdictKind
{
    Allowed,
    Rejected,

    /// <summary>The user crossed the violation threshold — the caller should apply a timed mute.</summary>
    AutoMute,
}

public readonly record struct SpamVerdict(SpamVerdictKind Kind, string? Reason = null, TimeSpan MuteDuration = default)
{
    public static readonly SpamVerdict Allowed = new(SpamVerdictKind.Allowed);
}

/// <summary>
/// In-memory, per-user spam protection consulted from <see cref="ChatService"/> (messages,
/// joins) and <see cref="ChannelService"/> (channel creation), so every ingress protocol —
/// SignalR and IRC alike — shares the same limits. State is per-process and never persisted;
/// auto-mutes land in the normal mute store. Operates only on content the server already
/// stores (for end-to-end encrypted rooms that is ciphertext) — nothing is decrypted.
/// </summary>
public sealed class SpamGuard
{
    private readonly SpamOptions _options;
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, UserState> _users = [];

    // Lazy stale-state pruning so the dictionary can't grow unbounded on busy servers
    private const int PruneThreshold = 512;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);

    public SpamGuard(SpamOptions options)
    {
        _options = options;
    }

    public bool Enabled => _options.Enabled;

    private sealed class UserState
    {
        public readonly Queue<DateTimeOffset> MessageTimes = new();
        public readonly Queue<DateTimeOffset> JoinTimes = new();
        public readonly Queue<DateTimeOffset> CreateTimes = new();
        public readonly Queue<DateTimeOffset> Violations = new();
        public string? LastContent;
        public int RepeatCount;
        public DateTimeOffset LastSeen;
    }

    /// <summary>
    /// Checks a message send. Rejections count as violations; enough violations inside the
    /// violation window escalate to <see cref="SpamVerdictKind.AutoMute"/> (evaluated only on
    /// rejected messages, so a clean message never mutes anyone).
    /// </summary>
    public SpamVerdict CheckMessage(Guid userId, ServerRole role, string content, DateTimeOffset? nowOverride = null)
    {
        if (!_options.Enabled || role >= ServerRole.Mod)
            return SpamVerdict.Allowed;

        var now = nowOverride ?? DateTimeOffset.UtcNow;

        lock (_lock)
        {
            var state = GetState(userId, now);

            // Flood: attempts count too — a client hammering retries must actually pause
            Prune(state.MessageTimes, now - TimeSpan.FromSeconds(_options.WindowSeconds));
            state.MessageTimes.Enqueue(now);
            if (state.MessageTimes.Count > _options.MaxMessagesPerWindow)
                return Reject(state, now, "You're sending messages too fast — slow down.");

            // Duplicates: identical (trimmed, case-insensitive) content repeated back-to-back
            var normalized = content.Trim().ToLowerInvariant();
            if (normalized == state.LastContent)
            {
                if (state.RepeatCount >= _options.MaxDuplicateMessages)
                    return Reject(state, now, "Duplicate message — say something new.");
                state.RepeatCount++;
            }
            else
            {
                state.LastContent = normalized;
                state.RepeatCount = 1;
            }

            return SpamVerdict.Allowed;
        }
    }

    /// <summary>Checks a channel join. Rejections count as violations but never auto-mute.</summary>
    public SpamVerdict CheckJoin(Guid userId, ServerRole role, DateTimeOffset? nowOverride = null)
    {
        return CheckWindow(userId, role, nowOverride,
            s => s.JoinTimes,
            TimeSpan.FromSeconds(_options.JoinWindowSeconds),
            _options.MaxJoinsPerWindow,
            "You're joining channels too fast — slow down.");
    }

    /// <summary>Checks a channel creation. Rejections count as violations but never auto-mute.</summary>
    public SpamVerdict CheckChannelCreate(Guid userId, ServerRole role, DateTimeOffset? nowOverride = null)
    {
        return CheckWindow(userId, role, nowOverride,
            s => s.CreateTimes,
            TimeSpan.FromMinutes(_options.ChannelCreateWindowMinutes),
            _options.MaxChannelCreatesPerWindow,
            "You're creating channels too fast — try again later.");
    }

    private SpamVerdict CheckWindow(Guid userId, ServerRole role, DateTimeOffset? nowOverride,
        Func<UserState, Queue<DateTimeOffset>> queueSelector, TimeSpan window, int max, string reason)
    {
        if (!_options.Enabled || role >= ServerRole.Mod)
            return SpamVerdict.Allowed;

        var now = nowOverride ?? DateTimeOffset.UtcNow;

        lock (_lock)
        {
            var state = GetState(userId, now);
            var queue = queueSelector(state);

            Prune(queue, now - window);
            queue.Enqueue(now);
            if (queue.Count > max)
            {
                RecordViolation(state, now);
                return new SpamVerdict(SpamVerdictKind.Rejected, reason);
            }

            return SpamVerdict.Allowed;
        }
    }

    /// <summary>
    /// Records a violation and decides between plain rejection and auto-mute escalation.
    /// On escalation the violation history is cleared so the user starts fresh post-mute.
    /// </summary>
    private SpamVerdict Reject(UserState state, DateTimeOffset now, string reason)
    {
        RecordViolation(state, now);

        if (_options.AutoMuteMinutes > 0 && state.Violations.Count >= _options.ViolationThreshold)
        {
            state.Violations.Clear();
            return new SpamVerdict(SpamVerdictKind.AutoMute, reason,
                TimeSpan.FromMinutes(_options.AutoMuteMinutes));
        }

        return new SpamVerdict(SpamVerdictKind.Rejected, reason);
    }

    private void RecordViolation(UserState state, DateTimeOffset now)
    {
        Prune(state.Violations, now - TimeSpan.FromMinutes(_options.ViolationWindowMinutes));
        state.Violations.Enqueue(now);
    }

    private UserState GetState(Guid userId, DateTimeOffset now)
    {
        if (_users.Count > PruneThreshold)
        {
            foreach (var stale in _users.Where(kv => now - kv.Value.LastSeen > StaleAfter).Select(kv => kv.Key).ToList())
                _users.Remove(stale);
        }

        if (!_users.TryGetValue(userId, out var state))
        {
            state = new UserState();
            _users[userId] = state;
        }

        state.LastSeen = now;
        return state;
    }

    private static void Prune(Queue<DateTimeOffset> queue, DateTimeOffset cutoff)
    {
        while (queue.Count > 0 && queue.Peek() < cutoff)
            queue.Dequeue();
    }
}
