using System.Diagnostics;

namespace Engine.Core.Diagnostics;

/// <summary>
/// Measures how long named pieces of work take each frame and keeps a short history of every one, so a debug panel can show
/// where the frame time goes. Sections are registered once by name (<see cref="GetSection"/>) and measured by integer id, so
/// timing a section allocates nothing and costs two clock reads.
/// </summary>
/// <remarks>
/// Time accumulates per section between calls to <see cref="BeginFrame"/>, which rolls it into the history. A section that runs
/// several times in one frame (a fixed system on a frame that took three steps) therefore shows the sum, which is the cost the
/// frame actually paid. The clock is injectable so tests can use exact tick values.
/// </remarks>
public sealed class Profiler
{
    /// <summary>Number of frames kept per section.</summary>
    public const int HistoryLength = 120;

    private readonly Func<long> _clock;
    private readonly List<string> _names = new();
    private readonly Dictionary<string, int> _ids = new(StringComparer.Ordinal);
    private readonly List<long> _accumulated = new();
    private readonly List<float[]> _history = new();
    private readonly List<float> _last = new();
    private int _historyHead;
    private int _frameCount;

    /// <summary>Creates a profiler that reads <see cref="Stopwatch"/> ticks.</summary>
    public Profiler() : this(Stopwatch.GetTimestamp, Stopwatch.Frequency) { }

    /// <summary>Creates a profiler with a custom clock (tests).</summary>
    /// <param name="clock">Returns a monotonically increasing tick count.</param>
    /// <param name="ticksPerSecond">How many ticks <paramref name="clock"/> advances per second.</param>
    public Profiler(Func<long> clock, long ticksPerSecond)
    {
        if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Ticks per second must be positive.");
        _clock = clock;
        TicksPerSecond = ticksPerSecond;
    }

    /// <summary>How many clock ticks make one second.</summary>
    public long TicksPerSecond { get; }

    /// <summary>Number of registered sections.</summary>
    public int SectionCount => _names.Count;

    /// <summary>Number of frames recorded so far (capped display length is <see cref="HistoryLength"/>).</summary>
    public int FramesRecorded => _frameCount;

    /// <summary>Registers a section, or returns the id of the existing one with that name. Call once and keep the id.</summary>
    /// <exception cref="ArgumentException">The name is empty.</exception>
    public int GetSection(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A profiler section needs a name.", nameof(name));
        if (_ids.TryGetValue(name, out int existing)) return existing;

        int id = _names.Count;
        _names.Add(name);
        _ids[name] = id;
        _accumulated.Add(0);
        _last.Add(0f);
        _history.Add(new float[HistoryLength]);
        return id;
    }

    /// <summary>The name a section was registered with.</summary>
    public string NameOf(int section) => _names[section];

    /// <summary>Starts timing; pass the result to <see cref="End"/>.</summary>
    public long Begin() => _clock();

    /// <summary>Adds the time since <paramref name="start"/> to the section's total for the current frame.</summary>
    /// <param name="section">Id from <see cref="GetSection"/>.</param>
    /// <param name="start">The value <see cref="Begin"/> returned.</param>
    public void End(int section, long start)
    {
        long elapsed = _clock() - start;
        _accumulated[section] += elapsed;
    }

    /// <summary>Closes the current frame: moves every section's accumulated time into its history and starts over.</summary>
    public void BeginFrame()
    {
        for (int i = 0; i < _accumulated.Count; i++)
        {
            float ms = (float)(_accumulated[i] * 1000.0 / TicksPerSecond);
            _last[i] = ms;
            _history[i][_historyHead] = ms;
            _accumulated[i] = 0;
        }

        _historyHead = (_historyHead + 1) % HistoryLength;
        _frameCount++;
    }

    /// <summary>Milliseconds the section took in the last closed frame.</summary>
    public float LastMs(int section) => _last[section];

    /// <summary>Average milliseconds over the recorded history (up to <see cref="HistoryLength"/> frames).</summary>
    public float AverageMs(int section)
    {
        int count = Math.Min(_frameCount, HistoryLength);
        if (count == 0) return 0f;

        float sum = 0f;
        var history = _history[section];
        for (int i = 0; i < count; i++) sum += history[i];
        return sum / count;
    }

    /// <summary>Largest value in the recorded history, in milliseconds.</summary>
    public float MaxMs(int section)
    {
        int count = Math.Min(_frameCount, HistoryLength);
        float max = 0f;
        var history = _history[section];
        for (int i = 0; i < count; i++) max = Math.Max(max, history[i]);
        return max;
    }

    /// <summary>
    /// The section's history as a ring buffer, plus the index of the oldest sample, which is the shape ImGui's plot widgets take.
    /// Slots not yet written read as zero.
    /// </summary>
    /// <param name="section">Id from <see cref="GetSection"/>.</param>
    /// <param name="oldest">Index of the oldest sample; plot with this offset so time runs left to right.</param>
    public float[] History(int section, out int oldest)
    {
        oldest = _frameCount < HistoryLength ? 0 : _historyHead;
        return _history[section];
    }
}
