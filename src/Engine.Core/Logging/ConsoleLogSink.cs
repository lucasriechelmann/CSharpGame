namespace Engine.Core.Logging;

/// <summary>Writes one line per entry, <c>12:30:05 INF Audio: message</c>, to a <see cref="TextWriter"/> (the console by default).</summary>
/// <param name="writer">Where lines go; <see cref="Console.Out"/> when omitted.</param>
public sealed class ConsoleLogSink(TextWriter? writer = null) : ILogSink
{
    private readonly TextWriter _writer = writer ?? Console.Out;

    /// <summary>The fixed-width tag shown for <paramref name="level"/>, so columns line up.</summary>
    /// <param name="level">The level to name.</param>
    public static string Tag(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown log level."),
    };

    /// <summary>Formats an entry as a single line (no trailing newline).</summary>
    /// <param name="entry">The entry to format.</param>
    public static string Format(in LogEntry entry) => $"{entry.Time:HH:mm:ss} {Tag(entry.Level)} {entry.Category}: {entry.Message}";

    /// <inheritdoc />
    public void Write(in LogEntry entry) => _writer.WriteLine(Format(entry));
}
