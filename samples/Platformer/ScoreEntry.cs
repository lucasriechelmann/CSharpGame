namespace Platformer;

/// <summary>One line of the high-score table.</summary>
/// <param name="Score">Final score of the run (including the time bonus when it was won).</param>
/// <param name="Seconds">How long the run lasted.</param>
/// <param name="Won">True when the run reached the exit, false when the hero ran out of lives.</param>
public readonly record struct ScoreEntry(int Score, double Seconds, bool Won);
