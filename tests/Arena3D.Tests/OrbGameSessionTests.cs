namespace Arena3D.Tests;

public class OrbGameSessionTests
{
    [Fact]
    public void A_new_run_starts_with_the_full_clock_and_no_score()
    {
        var session = new OrbGameSession(12);

        Assert.Equal(OrbPhase.Playing, session.Phase);
        Assert.Equal(12, session.OrbsTotal);
        Assert.Equal(0, session.OrbsCollected);
        Assert.Equal(OrbGameSession.TimeLimit, session.TimeLeft);
        Assert.Equal(0, session.Score);
    }

    [Fact]
    public void Collecting_an_orb_scores_points()
    {
        var session = new OrbGameSession(3);

        session.CollectOrb();

        Assert.Equal(1, session.OrbsCollected);
        Assert.Equal(OrbGameSession.PointsPerOrb, session.Score);
        Assert.Equal(OrbPhase.Playing, session.Phase);
    }

    [Fact]
    public void The_last_orb_wins_and_adds_a_bonus_for_the_time_left()
    {
        var session = new OrbGameSession(2);
        session.Tick(30.0); // 60 seconds left

        session.CollectOrb();
        session.CollectOrb();

        Assert.Equal(OrbPhase.Won, session.Phase);
        Assert.Equal(2 * OrbGameSession.PointsPerOrb + 60 * OrbGameSession.PointsPerSecondLeft, session.Score);
    }

    [Fact]
    public void Running_out_of_time_loses_and_the_clock_stops_at_zero()
    {
        var session = new OrbGameSession(5);

        session.Tick(OrbGameSession.TimeLimit + 10.0);

        Assert.Equal(OrbPhase.Lost, session.Phase);
        Assert.Equal(0.0, session.TimeLeft);
    }

    [Fact]
    public void Falling_costs_time_and_can_lose_the_run()
    {
        var session = new OrbGameSession(5);

        session.PlayerFell();

        Assert.Equal(1, session.Falls);
        Assert.Equal(OrbGameSession.TimeLimit - OrbGameSession.FallPenalty, session.TimeLeft);

        session.Tick(OrbGameSession.TimeLimit - OrbGameSession.FallPenalty - 1.0); // one second left
        session.PlayerFell();

        Assert.Equal(OrbPhase.Lost, session.Phase);
        Assert.Equal(0.0, session.TimeLeft);
    }

    [Fact]
    public void Nothing_changes_once_the_run_has_ended()
    {
        var won = new OrbGameSession(1);
        won.CollectOrb();
        int score = won.Score;

        won.CollectOrb();
        won.PlayerFell();
        won.Tick(100.0);

        Assert.Equal(score, won.Score);
        Assert.Equal(0, won.Falls);
        Assert.Equal(OrbPhase.Won, won.Phase);

        var lost = new OrbGameSession(3);
        lost.Tick(1000.0);
        lost.CollectOrb();

        Assert.Equal(0, lost.OrbsCollected);
        Assert.Equal(OrbPhase.Lost, lost.Phase);
    }

    [Fact]
    public void Reset_starts_over_and_needs_at_least_one_orb()
    {
        var session = new OrbGameSession(2);
        session.CollectOrb();
        session.PlayerFell();
        session.Tick(10.0);

        session.Reset(7);

        Assert.Equal(7, session.OrbsTotal);
        Assert.Equal(0, session.OrbsCollected);
        Assert.Equal(0, session.Falls);
        Assert.Equal(0, session.Score);
        Assert.Equal(OrbGameSession.TimeLimit, session.TimeLeft);
        Assert.Equal(OrbPhase.Playing, session.Phase);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Reset(0));
    }
}
