namespace Engine.Core.Logging;

/// <summary>
/// Sends log messages to a set of <see cref="ILogSink"/>s. Engine and game code take a <see cref="Logger"/> rather than
/// writing to the console, so output can also reach the in-game log panel (and later a file or the editor) without changing callers.
/// </summary>
/// <remarks>
/// Logging is for events, not per-frame work: each call builds a string. Guard anything chatty with <see cref="IsEnabled"/>
/// or log at <see cref="LogLevel.Debug"/>, which a default logger drops.
/// </remarks>
public sealed class Logger
{
    private readonly List<ILogSink> _sinks = new();
    private readonly Func<DateTime> _clock;

    /// <summary>Creates a logger with no sinks that stamps entries with the local time.</summary>
    public Logger() : this(() => DateTime.Now) { }

    /// <summary>Creates a logger with an injected clock (tests use a fixed time).</summary>
    /// <param name="clock">Supplies each entry's timestamp.</param>
    public Logger(Func<DateTime> clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <summary>Entries below this level are dropped before reaching any sink. Defaults to <see cref="LogLevel.Info"/>.</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    /// <summary>The sinks entries are sent to, in the order added.</summary>
    public IReadOnlyList<ILogSink> Sinks => _sinks;

    /// <summary>Adds a sink. The same sink may not be added twice.</summary>
    /// <param name="sink">Where to send entries from now on.</param>
    /// <returns>This logger, so calls can be chained.</returns>
    public Logger AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        if (_sinks.Contains(sink)) throw new ArgumentException("This sink is already added.", nameof(sink));
        _sinks.Add(sink);
        return this;
    }

    /// <summary>True when a message at <paramref name="level"/> would be kept; use it to skip building an expensive message.</summary>
    /// <param name="level">The level to test.</param>
    public bool IsEnabled(LogLevel level) => level >= MinimumLevel;

    /// <summary>Logs a message if <paramref name="level"/> passes <see cref="MinimumLevel"/>.</summary>
    /// <param name="level">Severity.</param>
    /// <param name="category">The area that is logging; must not be empty.</param>
    /// <param name="message">The text.</param>
    public void Log(LogLevel level, string category, string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(category);
        ArgumentNullException.ThrowIfNull(message);
        if (!IsEnabled(level)) return;

        var entry = new LogEntry(_clock(), level, category, message);
        foreach (var sink in _sinks) sink.Write(entry);
    }

    /// <summary>Logs at <see cref="LogLevel.Debug"/>.</summary>
    /// <param name="category">The area that is logging.</param>
    /// <param name="message">The text.</param>
    public void Debug(string category, string message) => Log(LogLevel.Debug, category, message);

    /// <summary>Logs at <see cref="LogLevel.Info"/>.</summary>
    /// <param name="category">The area that is logging.</param>
    /// <param name="message">The text.</param>
    public void Info(string category, string message) => Log(LogLevel.Info, category, message);

    /// <summary>Logs at <see cref="LogLevel.Warning"/>.</summary>
    /// <param name="category">The area that is logging.</param>
    /// <param name="message">The text.</param>
    public void Warning(string category, string message) => Log(LogLevel.Warning, category, message);

    /// <summary>Logs at <see cref="LogLevel.Error"/>.</summary>
    /// <param name="category">The area that is logging.</param>
    /// <param name="message">The text.</param>
    public void Error(string category, string message) => Log(LogLevel.Error, category, message);
}
