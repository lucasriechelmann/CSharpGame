namespace Platformer.Tests;

public class GameSessionTests
{
    [Fact]
    public void A_new_session_starts_playing_with_full_lives_and_a_short_grace_period()
    {
        var session = new GameSession(5);

        Assert.Equal(GamePhase.Playing, session.Phase);
        Assert.Equal(GameSession.StartingLives, session.Lives);
        Assert.Equal(0, session.Score);
        Assert.Equal(0, session.CoinsCollected);
        Assert.Equal(5, session.CoinsTotal);
        Assert.True(session.IsInvulnerable);
    }

    [Fact]
    public void Negative_coin_counts_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameSession(-1));
    }

    [Fact]
    public void Grace_period_ends_after_StartGrace_seconds()
    {
        var session = new GameSession(1);

        session.Tick(GameSession.StartGrace);

        Assert.False(session.IsInvulnerable);
    }

    [Fact]
    public void Collecting_coins_adds_score_and_count()
    {
        var session = new GameSession(3);

        session.CollectCoin(50);
        session.CollectCoin(25);

        Assert.Equal(2, session.CoinsCollected);
        Assert.Equal(75, session.Score);
    }

    [Fact]
    public void The_goal_unlocks_when_every_coin_is_collected()
    {
        var session = new GameSession(2);
        session.CollectCoin(1);
        Assert.False(session.GoalUnlocked);

        session.CollectCoin(1);

        Assert.True(session.GoalUnlocked);
    }

    [Fact]
    public void A_level_without_coins_has_its_goal_open_from_the_start()
    {
        Assert.True(new GameSession(0).GoalUnlocked);
    }

    [Fact]
    public void Hurting_takes_a_life_and_starts_invulnerability()
    {
        var session = new GameSession(1);
        session.Tick(GameSession.StartGrace);

        Assert.True(session.TryHurt());

        Assert.Equal(GameSession.StartingLives - 1, session.Lives);
        Assert.True(session.IsInvulnerable);
    }

    [Fact]
    public void Hurting_during_invulnerability_does_nothing()
    {
        var session = new GameSession(1); // starts in its grace period

        Assert.False(session.TryHurt());

        Assert.Equal(GameSession.StartingLives, session.Lives);
    }

    [Fact]
    public void Invulnerability_wears_off_so_the_hero_can_be_hurt_again()
    {
        var session = new GameSession(1);
        session.Tick(GameSession.StartGrace);
        session.TryHurt();

        session.Tick(GameSession.HitInvulnerability);

        Assert.False(session.IsInvulnerable);
        Assert.True(session.TryHurt());
    }

    [Fact]
    public void Losing_the_last_life_loses_the_run()
    {
        var session = new GameSession(1);
        for (int i = 0; i < GameSession.StartingLives; i++)
        {
            session.Tick(GameSession.HitInvulnerability + GameSession.StartGrace);
            Assert.True(session.TryHurt());
        }

        Assert.Equal(GamePhase.Lost, session.Phase);
        Assert.Equal(0, session.Lives);
    }

    [Fact]
    public void Stomping_adds_points()
    {
        var session = new GameSession(1);

        session.StompKill();

        Assert.Equal(GameSession.StompPoints, session.Score);
    }

    [Fact]
    public void Reaching_a_locked_goal_does_not_win()
    {
        var session = new GameSession(1);

        Assert.False(session.TryReachGoal());

        Assert.Equal(GamePhase.Playing, session.Phase);
    }

    [Fact]
    public void Reaching_the_unlocked_goal_wins_and_adds_the_time_bonus()
    {
        var session = new GameSession(1);
        session.CollectCoin(50);
        session.Tick(20); // 20 s used: 100 s saved at 10 points each
        int expected = 50 + (GameSession.BonusSeconds - 20) * GameSession.PointsPerSecondSaved;

        Assert.True(session.TryReachGoal());

        Assert.Equal(GamePhase.Won, session.Phase);
        Assert.Equal(expected, session.Score);
    }

    [Fact]
    public void The_time_bonus_never_goes_negative()
    {
        var session = new GameSession(0);
        session.Tick(GameSession.BonusSeconds + 500);

        Assert.Equal(0, session.TimeBonus);
    }

    [Fact]
    public void Nothing_changes_after_the_run_is_over()
    {
        var session = new GameSession(0);
        session.TryReachGoal();
        int score = session.Score;
        double elapsed = session.Elapsed;

        session.Tick(10);
        session.CollectCoin(50);
        session.StompKill();

        Assert.False(session.TryHurt());
        Assert.False(session.TryReachGoal());
        Assert.Equal(score, session.Score);
        Assert.Equal(elapsed, session.Elapsed);
    }

    [Fact]
    public void Reset_starts_a_fresh_run()
    {
        var session = new GameSession(1);
        session.CollectCoin(50);
        session.TryReachGoal();

        session.Reset(4);

        Assert.Equal(GamePhase.Playing, session.Phase);
        Assert.Equal(0, session.Score);
        Assert.Equal(0, session.CoinsCollected);
        Assert.Equal(4, session.CoinsTotal);
        Assert.Equal(0, session.Elapsed);
    }

    [Theory]
    [InlineData(10, 4, 6)]
    [InlineData(10, 10, 0)]
    [InlineData(10, 0, 10)]
    public void Syncing_from_a_loaded_world_derives_collected_from_remaining(int total, int remaining, int expectedCollected)
    {
        var session = new GameSession(total);

        session.SyncCoinsRemaining(remaining);

        Assert.Equal(expectedCollected, session.CoinsCollected);
    }

    [Fact]
    public void Syncing_clamps_when_the_world_has_more_coins_than_expected()
    {
        var session = new GameSession(3);

        session.SyncCoinsRemaining(99);

        Assert.Equal(0, session.CoinsCollected);
    }
}
