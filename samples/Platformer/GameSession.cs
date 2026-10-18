namespace Platformer;

/// <summary>
/// The rules and score of one run: lives, coins, the clock, and whether the run is still going. Pure data and logic with no
/// engine types, so every rule is unit-tested; the systems that detect overlaps only report events to it
/// (<see cref="CollectCoin"/>, <see cref="TryHurt"/>, <see cref="StompKill"/>, <see cref="TryReachGoal"/>).
/// </summary>
public sealed class GameSession
{
    /// <summary>Lives at the start of a run.</summary>
    public const int StartingLives = 3;

    /// <summary>Seconds the hero cannot be hurt again after a hit, so one creature cannot drain every life at once.</summary>
    public const float HitInvulnerability = 1.5f;

    /// <summary>Seconds of safety at the start of a run, because creatures may be walking next to the hero's spawn point.</summary>
    public const float StartGrace = 2f;

    /// <summary>Points for stomping a creature.</summary>
    public const int StompPoints = 100;

    /// <summary>Seconds within which finishing earns a bonus; the bonus shrinks by <see cref="PointsPerSecondSaved"/> per second used.</summary>
    public const int BonusSeconds = 120;

    /// <summary>Bonus points for each second under <see cref="BonusSeconds"/>.</summary>
    public const int PointsPerSecondSaved = 10;

    /// <summary>Creates a session in its starting state.</summary>
    /// <param name="coinsTotal">How many coins the level holds.</param>
    public GameSession(int coinsTotal) => Reset(coinsTotal);

    /// <summary>Lives left; the run is lost at zero.</summary>
    public int Lives { get; private set; }

    /// <summary>Current score.</summary>
    public int Score { get; private set; }

    /// <summary>Coins picked up so far.</summary>
    public int CoinsCollected { get; private set; }

    /// <summary>Coins in the level when the run started.</summary>
    public int CoinsTotal { get; private set; }

    /// <summary>Seconds played this run (stops when the run ends).</summary>
    public double Elapsed { get; private set; }

    /// <summary>Seconds of invulnerability left.</summary>
    public double InvulnerableSeconds { get; private set; }

    /// <summary>Whether the run is going, won or lost.</summary>
    public GamePhase Phase { get; private set; }

    /// <summary>True while the hero cannot be hurt.</summary>
    public bool IsInvulnerable => InvulnerableSeconds > 0;

    /// <summary>True once every coin is collected, which opens the exit.</summary>
    public bool GoalUnlocked => CoinsCollected >= CoinsTotal;

    /// <summary>Bonus points a win would earn right now for speed.</summary>
    public int TimeBonus => Math.Max(0, BonusSeconds - (int)Elapsed) * PointsPerSecondSaved;

    /// <summary>Starts a fresh run.</summary>
    /// <param name="coinsTotal">How many coins the level holds.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    public void Reset(int coinsTotal)
    {
        if (coinsTotal < 0) throw new ArgumentOutOfRangeException(nameof(coinsTotal), coinsTotal, "A level cannot have a negative number of coins.");

        CoinsTotal = coinsTotal;
        CoinsCollected = 0;
        Lives = StartingLives;
        Score = 0;
        Elapsed = 0;
        InvulnerableSeconds = StartGrace;
        Phase = GamePhase.Playing;
    }

    /// <summary>Advances the clock and the invulnerability timer. Does nothing once the run is over.</summary>
    /// <param name="dt">Seconds since the last tick.</param>
    public void Tick(double dt)
    {
        if (Phase != GamePhase.Playing) return;

        Elapsed += dt;
        if (InvulnerableSeconds > 0) InvulnerableSeconds = Math.Max(0, InvulnerableSeconds - dt);
    }

    /// <summary>Records a picked-up coin.</summary>
    /// <param name="points">Score the coin is worth.</param>
    public void CollectCoin(int points)
    {
        if (Phase != GamePhase.Playing) return;

        CoinsCollected++;
        Score += points;
    }

    /// <summary>Records a stomped creature.</summary>
    public void StompKill()
    {
        if (Phase == GamePhase.Playing) Score += StompPoints;
    }

    /// <summary>Takes a life unless the hero is invulnerable or the run is over; loses the run when the last life goes.</summary>
    /// <returns>True when a life was taken (so the caller should knock the hero back and play the hurt sound).</returns>
    public bool TryHurt()
    {
        if (Phase != GamePhase.Playing || IsInvulnerable) return false;

        Lives--;
        InvulnerableSeconds = HitInvulnerability;
        if (Lives <= 0) Phase = GamePhase.Lost;
        return true;
    }

    /// <summary>Wins the run (adding the time bonus) if the exit is unlocked.</summary>
    /// <returns>True when the run was won by this call.</returns>
    public bool TryReachGoal()
    {
        if (Phase != GamePhase.Playing || !GoalUnlocked) return false;

        Score += TimeBonus;
        Phase = GamePhase.Won;
        return true;
    }

    /// <summary>
    /// Re-aligns the coin count with a world that was loaded from a save: the coins still in the world are the ones not collected.
    /// </summary>
    /// <param name="remaining">Coin entities currently in the world.</param>
    public void SyncCoinsRemaining(int remaining) =>
        CoinsCollected = Math.Clamp(CoinsTotal - remaining, 0, CoinsTotal);
}
