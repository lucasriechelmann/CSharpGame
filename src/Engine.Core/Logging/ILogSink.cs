namespace Engine.Core.Logging;

/// <summary>
/// A place log entries go: the console, the in-game log panel, a file. <see cref="Logger"/> calls every sink for each entry that
/// passes its level filter. Sinks must not throw for ordinary input; an exception from a sink propagates to the caller.
/// </summary>
public interface ILogSink
{
    /// <summary>Records <paramref name="entry"/>.</summary>
    /// <param name="entry">The message to record.</param>
    void Write(in LogEntry entry);
}
