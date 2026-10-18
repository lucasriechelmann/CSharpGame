namespace Engine.Core.Logging;

/// <summary>One logged message, as handed to every <see cref="ILogSink"/>.</summary>
/// <param name="Time">When the message was logged (from the logger's clock).</param>
/// <param name="Level">Severity.</param>
/// <param name="Category">Which part of the engine or game wrote it ("Audio", "Scenes", "Game"), so output can be filtered by area.</param>
/// <param name="Message">The text.</param>
public readonly record struct LogEntry(DateTime Time, LogLevel Level, string Category, string Message);
