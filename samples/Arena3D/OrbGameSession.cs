namespace Arena3D;

/// <summary>
/// The rules of one run of Orb Run: collect every orb before the clock runs out; falling off the arena costs time. Plain data and logic with
/// no engine types, so every rule is unit-tested; the systems that detect pick-ups and falls only report events to it.
/// </summary>
public sealed class OrbGameSession
{
    /// <summary>Seconds a run lasts.</summary>
    public const double TimeLimit = 90.0;

    /// <summary>Seconds taken off the clock each time the player falls off the arena.</summary>
    public const double FallPenalty = 5.0;

    /// <summary>Points for each orb.</summary>
    public const int PointsPerOrb = 100;

    /// <summary>Bonus points for each whole second left on the clock when the last orb is collected.</summary>
    public const int PointsPerSecondLeft = 10;

    /// <summary>Creates a session in its starting state.</summary>
    /// <param name="orbsTotal">How many orbs the arena holds.</param>
    public OrbGameSession(int orbsTotal) => Reset(orbsTotal);

    /// <summary>Orbs picked up so far.</summary>
    public int OrbsCollected { get; private set; }

    /// <summary>Orbs in the arena when the run started.</summary>
    public int OrbsTotal { get; private set; }

    /// <summary>Seconds left on the clock.</summary>
    public double TimeLeft { get; private set; }

    /// <summary>Current score.</summary>
    public int Score { get; private set; }

    /// <summary>How many times the player fell off the arena this run.</summary>
    public int Falls { get; private set; }

    /// <summary>Whether the run is going, won or lost.</summary>
    public OrbPhase Phase { get; private set; }

    /// <summary>Starts a fresh run.</summary>
    /// <param name="orbsTotal">How many orbs the arena holds.</param>
    /// <exception cref="ArgumentOutOfRangeException">There must be at least one orb, or the run would be won before it starts.</exception>
    public void Reset(int orbsTotal)
    {
        if (orbsTotal < 1) throw new ArgumentOutOfRangeException(nameof(orbsTotal), orbsTotal, "An arena needs at least one orb.");

        OrbsTotal = orbsTotal;
        OrbsCollected = 0;
        TimeLeft = TimeLimit;
        Score = 0;
        Falls = 0;
        Phase = OrbPhase.Playing;
    }

    /// <summary>Advances the clock. Does nothing once the run has ended.</summary>
    /// <param name="dt">Seconds since the last call.</param>
    public void Tick(double dt)
    {
        if (Phase != OrbPhase.Playing) return;

        TimeLeft -= dt;
        if (TimeLeft <= 0.0) Lose();
    }

    /// <summary>The player picked up an orb. The last one wins the run and adds a time bonus.</summary>
    public void CollectOrb()
    {
        if (Phase != OrbPhase.Playing) return;

        OrbsCollected++;
        Score += PointsPerOrb;
        if (OrbsCollected < OrbsTotal) return;

        Phase = OrbPhase.Won;
        Score += (int)TimeLeft * PointsPerSecondLeft;
    }

    /// <summary>The player fell off the arena: the clock loses <see cref="FallPenalty"/> seconds (which can end the run).</summary>
    public void PlayerFell()
    {
        if (Phase != OrbPhase.Playing) return;

        Falls++;
        TimeLeft -= FallPenalty;
        if (TimeLeft <= 0.0) Lose();
    }

    private void Lose()
    {
        TimeLeft = 0.0;
        Phase = OrbPhase.Lost;
    }
}
