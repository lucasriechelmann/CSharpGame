namespace Platformer.Tests;

public class HighScoresTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "platformer-scores-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, recursive: true);
    }

    private static ScoreEntry Run(int score, double seconds = 60, bool won = true) => new(score, seconds, won);

    [Fact]
    public void A_new_table_is_empty()
    {
        Assert.Empty(new HighScores().Entries);
    }

    [Fact]
    public void The_first_score_takes_rank_zero()
    {
        var scores = new HighScores();

        Assert.Equal(0, scores.Submit(Run(500)));
        Assert.Single(scores.Entries);
    }

    [Fact]
    public void Entries_are_kept_best_first()
    {
        var scores = new HighScores();

        scores.Submit(Run(300));
        scores.Submit(Run(900));
        int rank = scores.Submit(Run(600));

        Assert.Equal(1, rank);
        Assert.Equal(new[] { 900, 600, 300 }, scores.Entries.Select(e => e.Score).ToArray());
    }

    [Fact]
    public void A_tie_ranks_below_the_older_run()
    {
        var scores = new HighScores();
        scores.Submit(Run(500, seconds: 30));

        int rank = scores.Submit(Run(500, seconds: 99));

        Assert.Equal(1, rank);
        Assert.Equal(30, scores.Entries[0].Seconds);
    }

    [Fact]
    public void Only_the_best_five_are_kept()
    {
        var scores = new HighScores();
        for (int i = 1; i <= 7; i++) scores.Submit(Run(i * 100));

        Assert.Equal(HighScores.MaxEntries, scores.Entries.Count);
        Assert.Equal(700, scores.Entries[0].Score);
        Assert.Equal(300, scores.Entries[^1].Score);
    }

    [Fact]
    public void A_score_too_low_for_a_full_table_is_rejected()
    {
        var scores = new HighScores();
        for (int i = 1; i <= 5; i++) scores.Submit(Run(i * 100));

        Assert.Equal(-1, scores.Submit(Run(50)));
        Assert.Equal(-1, scores.Submit(Run(100)));
        Assert.Equal(5, scores.Entries.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Zero_or_negative_scores_are_never_recorded(int score)
    {
        var scores = new HighScores();

        Assert.Equal(-1, scores.Submit(Run(score)));
        Assert.Empty(scores.Entries);
    }

    [Fact]
    public void Saving_and_loading_round_trips()
    {
        string path = Path.Combine(_folder, "scores.json");
        var scores = new HighScores();
        scores.Submit(new ScoreEntry(1200, 75.5, Won: true));
        scores.Submit(new ScoreEntry(300, 20.25, Won: false));

        scores.Save(path);
        var loaded = HighScores.Load(path);

        Assert.Equal(scores.Entries, loaded.Entries);
    }

    [Fact]
    public void Saving_creates_the_missing_folder()
    {
        string path = Path.Combine(_folder, "deep", "er", "scores.json");

        new HighScores().Save(path);

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Loading_a_missing_file_gives_an_empty_table()
    {
        Assert.Empty(HighScores.Load(Path.Combine(_folder, "nothing.json")).Entries);
    }

    [Fact]
    public void Loading_a_hand_edited_file_re_sorts_and_trims_it()
    {
        string path = Path.Combine(_folder, "scores.json");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(path, """
            [ {"score":100,"seconds":1,"won":true}, {"score":900,"seconds":1,"won":true}, {"score":500,"seconds":1,"won":true},
              {"score":400,"seconds":1,"won":true}, {"score":300,"seconds":1,"won":true}, {"score":200,"seconds":1,"won":true} ]
            """);

        var loaded = HighScores.Load(path);

        Assert.Equal(new[] { 900, 500, 400, 300, 200 }, loaded.Entries.Select(e => e.Score).ToArray());
    }

    [Fact]
    public void Loading_garbage_fails_naming_the_file()
    {
        string path = Path.Combine(_folder, "scores.json");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(path, "not json at all");

        var error = Assert.Throws<InvalidOperationException>(() => HighScores.Load(path));

        Assert.Contains("scores.json", error.Message);
    }

    [Fact]
    public void Loading_a_null_document_fails()
    {
        string path = Path.Combine(_folder, "scores.json");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(path, "null");

        Assert.Throws<InvalidOperationException>(() => HighScores.Load(path));
    }
}
