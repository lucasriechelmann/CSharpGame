namespace Platformer;

// High scores: loaded at start, a finished run is submitted once, saved to saves/highscores.json (player data, like the quick-save).
public sealed partial class PlatformerGame
{
    private static readonly string HighScoresPath = Path.Combine(AppContext.BaseDirectory, "saves", "highscores.json");

    private HighScores _highScores = new();
    private bool _runRecorded;
    private int _lastRank = -1;

    /// <summary>
    /// Reads the saved table. A damaged file must not stop the game from starting (it is player data, not content), so it is
    /// reported in the log as an error and the game starts with an empty table; the next finished run overwrites the file.
    /// </summary>
    private void LoadHighScores()
    {
        try
        {
            _highScores = HighScores.Load(HighScoresPath);
            Log.Info("Game", $"High scores: {_highScores.Entries.Count} saved");
        }
        catch (InvalidOperationException error)
        {
            Log.Error("Game", error.Message);
        }
    }

    /// <summary>Submits the run once when it ends and saves the table if it earned a place.</summary>
    private void RecordFinishedRun()
    {
        if (_session.Phase == GamePhase.Playing || _runRecorded) return;

        _runRecorded = true;
        _lastRank = _highScores.Submit(new ScoreEntry(_session.Score, _session.Elapsed, _session.Phase == GamePhase.Won));
        if (_lastRank < 0) return;

        _highScores.Save(HighScoresPath);
        Log.Info("Game", $"New high score #{_lastRank + 1}: {_session.Score}");
    }

    /// <summary>Forgets the previous run's result when a new run starts.</summary>
    private void ResetRunRecord()
    {
        _runRecorded = false;
        _lastRank = -1;
    }
}
