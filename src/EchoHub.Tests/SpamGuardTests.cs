using EchoHub.Core.Models;
using EchoHub.Server.Config;
using EchoHub.Server.Services;
using Xunit;

namespace EchoHub.Tests;

public class SpamGuardTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Alice = Guid.NewGuid();

    private static SpamGuard CreateGuard(Action<SpamOptions>? configure = null)
    {
        var options = new SpamOptions();
        configure?.Invoke(options);
        return new SpamGuard(options);
    }

    // ── Message flood ─────────────────────────────────────────────────

    [Fact]
    public void Flood_UnderLimit_Allowed()
    {
        var guard = CreateGuard();

        for (var i = 0; i < 8; i++)
        {
            var verdict = guard.CheckMessage(Alice, ServerRole.Member, $"msg {i}", T0.AddMilliseconds(i * 100));
            Assert.Equal(SpamVerdictKind.Allowed, verdict.Kind);
        }
    }

    [Fact]
    public void Flood_OverLimit_Rejected()
    {
        var guard = CreateGuard();

        for (var i = 0; i < 8; i++)
            guard.CheckMessage(Alice, ServerRole.Member, $"msg {i}", T0.AddMilliseconds(i * 100));

        var verdict = guard.CheckMessage(Alice, ServerRole.Member, "msg 9", T0.AddSeconds(1));

        Assert.Equal(SpamVerdictKind.Rejected, verdict.Kind);
        Assert.Contains("too fast", verdict.Reason);
    }

    [Fact]
    public void Flood_RecoversAfterWindow()
    {
        var guard = CreateGuard();

        for (var i = 0; i < 9; i++)
            guard.CheckMessage(Alice, ServerRole.Member, $"msg {i}", T0.AddMilliseconds(i * 100));

        // The whole window has passed with no attempts — allowed again
        var verdict = guard.CheckMessage(Alice, ServerRole.Member, "later", T0.AddSeconds(10));

        Assert.Equal(SpamVerdictKind.Allowed, verdict.Kind);
    }

    [Fact]
    public void Flood_IsPerUser()
    {
        var guard = CreateGuard();
        var bob = Guid.NewGuid();

        for (var i = 0; i < 9; i++)
            guard.CheckMessage(Alice, ServerRole.Member, $"msg {i}", T0.AddMilliseconds(i * 100));

        var verdict = guard.CheckMessage(bob, ServerRole.Member, "hello", T0.AddSeconds(1));

        Assert.Equal(SpamVerdictKind.Allowed, verdict.Kind);
    }

    // ── Duplicates ────────────────────────────────────────────────────

    [Fact]
    public void Duplicates_UpToLimit_Allowed_ThenRejected()
    {
        // Slow enough that the flood window never trips (one message per 10 s)
        var guard = CreateGuard();

        for (var i = 0; i < 3; i++)
        {
            var v = guard.CheckMessage(Alice, ServerRole.Member, "same thing", T0.AddSeconds(i * 10));
            Assert.Equal(SpamVerdictKind.Allowed, v.Kind);
        }

        var verdict = guard.CheckMessage(Alice, ServerRole.Member, "same thing", T0.AddSeconds(30));

        Assert.Equal(SpamVerdictKind.Rejected, verdict.Kind);
        Assert.Contains("Duplicate", verdict.Reason);
    }

    [Fact]
    public void Duplicates_CaseAndWhitespaceInsensitive()
    {
        var guard = CreateGuard(o => o.MaxDuplicateMessages = 1);

        guard.CheckMessage(Alice, ServerRole.Member, "Hello World", T0);
        var verdict = guard.CheckMessage(Alice, ServerRole.Member, "  hello world  ", T0.AddSeconds(10));

        Assert.Equal(SpamVerdictKind.Rejected, verdict.Kind);
    }

    [Fact]
    public void Duplicates_ResetByDifferentMessage()
    {
        var guard = CreateGuard(o => o.MaxDuplicateMessages = 1);

        guard.CheckMessage(Alice, ServerRole.Member, "same", T0);
        guard.CheckMessage(Alice, ServerRole.Member, "different", T0.AddSeconds(10));
        var verdict = guard.CheckMessage(Alice, ServerRole.Member, "same", T0.AddSeconds(20));

        Assert.Equal(SpamVerdictKind.Allowed, verdict.Kind);
    }

    // ── Escalation → auto-mute ────────────────────────────────────────

    [Fact]
    public void Escalation_EnoughViolations_ReturnsAutoMute()
    {
        var guard = CreateGuard(o => o.ViolationThreshold = 3);
        SpamVerdict verdict = default;

        // Keep hammering inside the flood window: 8 allowed, then rejections accumulate
        for (var i = 0; i < 8 + 3; i++)
            verdict = guard.CheckMessage(Alice, ServerRole.Member, $"m{i}", T0.AddMilliseconds(i * 50));

        Assert.Equal(SpamVerdictKind.AutoMute, verdict.Kind);
        Assert.Equal(TimeSpan.FromMinutes(5), verdict.MuteDuration);
    }

    [Fact]
    public void Escalation_ClearsViolations_NextRejectionIsPlain()
    {
        var guard = CreateGuard(o => o.ViolationThreshold = 3);

        for (var i = 0; i < 8 + 3; i++)
            guard.CheckMessage(Alice, ServerRole.Member, $"m{i}", T0.AddMilliseconds(i * 50));

        // Still inside the flood window — rejected, but the counter restarted
        var verdict = guard.CheckMessage(Alice, ServerRole.Member, "again", T0.AddSeconds(2));

        Assert.Equal(SpamVerdictKind.Rejected, verdict.Kind);
    }

    [Fact]
    public void Escalation_AutoMuteDisabled_StaysRejected()
    {
        var guard = CreateGuard(o => { o.ViolationThreshold = 3; o.AutoMuteMinutes = 0; });
        SpamVerdict verdict = default;

        for (var i = 0; i < 8 + 10; i++)
            verdict = guard.CheckMessage(Alice, ServerRole.Member, $"m{i}", T0.AddMilliseconds(i * 50));

        Assert.Equal(SpamVerdictKind.Rejected, verdict.Kind);
    }

    // ── Exemptions / master switch ────────────────────────────────────

    [Theory]
    [InlineData(ServerRole.Mod)]
    [InlineData(ServerRole.Admin)]
    [InlineData(ServerRole.Owner)]
    public void ModAndAbove_AlwaysAllowed(ServerRole role)
    {
        var guard = CreateGuard();

        for (var i = 0; i < 50; i++)
        {
            var verdict = guard.CheckMessage(Alice, role, "same spam", T0.AddMilliseconds(i * 10));
            Assert.Equal(SpamVerdictKind.Allowed, verdict.Kind);
        }
    }

    [Fact]
    public void Disabled_EverythingAllowed()
    {
        var guard = CreateGuard(o => o.Enabled = false);

        for (var i = 0; i < 50; i++)
            Assert.Equal(SpamVerdictKind.Allowed,
                guard.CheckMessage(Alice, ServerRole.Member, "same", T0.AddMilliseconds(i * 10)).Kind);

        Assert.Equal(SpamVerdictKind.Allowed, guard.CheckJoin(Alice, ServerRole.Member, T0).Kind);
        Assert.Equal(SpamVerdictKind.Allowed, guard.CheckChannelCreate(Alice, ServerRole.Member, T0).Kind);
        Assert.False(guard.Enabled);
    }

    // ── Join throttle ─────────────────────────────────────────────────

    [Fact]
    public void Joins_OverLimit_Rejected_ThenRecovers()
    {
        var guard = CreateGuard(o => { o.MaxJoinsPerWindow = 5; o.JoinWindowSeconds = 30; });

        for (var i = 0; i < 5; i++)
            Assert.Equal(SpamVerdictKind.Allowed, guard.CheckJoin(Alice, ServerRole.Member, T0.AddSeconds(i)).Kind);

        Assert.Equal(SpamVerdictKind.Rejected, guard.CheckJoin(Alice, ServerRole.Member, T0.AddSeconds(6)).Kind);
        Assert.Equal(SpamVerdictKind.Allowed, guard.CheckJoin(Alice, ServerRole.Member, T0.AddSeconds(60)).Kind);
    }

    // ── Channel-create throttle ───────────────────────────────────────

    [Fact]
    public void ChannelCreates_OverLimit_Rejected_ThenRecovers()
    {
        var guard = CreateGuard();

        for (var i = 0; i < 3; i++)
            Assert.Equal(SpamVerdictKind.Allowed, guard.CheckChannelCreate(Alice, ServerRole.Member, T0.AddSeconds(i)).Kind);

        Assert.Equal(SpamVerdictKind.Rejected, guard.CheckChannelCreate(Alice, ServerRole.Member, T0.AddSeconds(10)).Kind);
        Assert.Equal(SpamVerdictKind.Allowed, guard.CheckChannelCreate(Alice, ServerRole.Member, T0.AddMinutes(15)).Kind);
    }

    [Fact]
    public void JoinAndCreateRejections_NeverAutoMute()
    {
        var guard = CreateGuard(o => { o.MaxJoinsPerWindow = 1; o.ViolationThreshold = 2; });

        for (var i = 0; i < 20; i++)
        {
            var verdict = guard.CheckJoin(Alice, ServerRole.Member, T0.AddSeconds(i));
            Assert.NotEqual(SpamVerdictKind.AutoMute, verdict.Kind);
        }
    }
}
