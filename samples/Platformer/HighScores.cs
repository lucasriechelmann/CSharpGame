using System.Text.Json;

namespace Platformer;

/// <summary>
/// The best few runs, highest score first, kept in a JSON file between sessions. Plain data and file access with no engine types,
/// so the ranking rules and the file round trip are unit-tested.
/// </summary>
public sealed class HighScores
{
    /// <summary>How many runs the table keeps.</summary>
    public const int MaxEntries = 5;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly List<ScoreEntry> _entries = new();

    /// <summary>The kept runs, best first.</summary>
    public IReadOnlyList<ScoreEntry> Entries => _entries;

    /// <summary>
    /// Adds a finished run if it earns a place. A run that ties an existing score ranks below it (the older run keeps its place),
    /// and a score of zero or less is never recorded.
    /// </summary>
    /// <param name="entry">The finished run.</param>
    /// <returns>The zero-based rank it took, or -1 when it did not make the table.</returns>
    public int Submit(ScoreEntry entry)
    {
        if (entry.Score <= 0) return -1;

        int rank = _entries.FindIndex(existing => entry.Score > existing.Score);
        if (rank < 0) rank = _entries.Count;
        if (rank >= MaxEntries) return -1;

        _entries.Insert(rank, entry);
        if (_entries.Count > MaxEntries) _entries.RemoveAt(MaxEntries);
        return rank;
    }

    /// <summary>Reads a table from <paramref name="path"/>; a missing file is an empty table (the first ever run).</summary>
    /// <param name="path">The JSON file.</param>
    /// <exception cref="InvalidOperationException">The file exists but is not a valid table; the message names the file.</exception>
    public static HighScores Load(string path)
    {
        var scores = new HighScores();
        if (!File.Exists(path)) return scores;

        List<ScoreEntry>? entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<ScoreEntry>>(File.ReadAllText(path), Json);
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException($"The high-score file '{path}' is not valid: {error.Message}", error);
        }

        if (entries is null) throw new InvalidOperationException($"The high-score file '{path}' is empty.");

        // Go through Submit so a hand-edited file is re-sorted and trimmed to the rules rather than trusted.
        foreach (var entry in entries) scores.Submit(entry);
        return scores;
    }

    /// <summary>Writes the table to <paramref name="path"/>, creating its folder if needed.</summary>
    /// <param name="path">The JSON file.</param>
    public void Save(string path)
    {
        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(path, JsonSerializer.Serialize(_entries, Json));
    }
}
