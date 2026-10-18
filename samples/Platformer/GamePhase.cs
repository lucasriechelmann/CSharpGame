namespace Platformer;

/// <summary>Where a run of the game stands.</summary>
public enum GamePhase
{
    /// <summary>The hero can be played; the clock runs.</summary>
    Playing,

    /// <summary>The hero reached the unlocked exit. The run is over and its score is final.</summary>
    Won,

    /// <summary>The hero ran out of lives. The run is over.</summary>
    Lost,
}
