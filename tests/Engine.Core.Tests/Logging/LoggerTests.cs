using Engine.Core.Logging;

namespace Engine.Core.Tests.Logging;

public class LoggerTests
{
    private static readonly DateTime Noon = new(2026, 1, 2, 12, 30, 5);

    private static (Logger Logger, MemoryLogSink Sink) Create()
    {
        var sink = new MemoryLogSink();
        return (new Logger(() => Noon).AddSink(sink), sink);
    }

    [Fact]
    public void Entry_reaches_every_sink_with_level_category_message_and_time()
    {
        var first = new MemoryLogSink();
        var second = new MemoryLogSink();
        var logger = new Logger(() => Noon).AddSink(first).AddSink(second);

        logger.Warning("Audio", "no device");

        foreach (var sink in new[] { first, second })
        {
            var entry = Assert.Single(Enumerate(sink));
            Assert.Equal(new LogEntry(Noon, LogLevel.Warning, "Audio", "no device"), entry);
        }
    }

    [Fact]
    public void Default_minimum_level_drops_debug_and_keeps_info()
    {
        var (logger, sink) = Create();

        logger.Debug("Game", "chatty");
        logger.Info("Game", "hello");

        Assert.Equal("hello", Assert.Single(Enumerate(sink)).Message);
    }

    [Fact]
    public void Lowering_the_minimum_level_lets_debug_through()
    {
        var (logger, sink) = Create();
        logger.MinimumLevel = LogLevel.Debug;

        logger.Debug("Game", "chatty");

        Assert.Single(Enumerate(sink));
    }

    [Fact]
    public void Raising_the_minimum_level_drops_lower_levels()
    {
        var (logger, sink) = Create();
        logger.MinimumLevel = LogLevel.Error;

        logger.Info("Game", "a");
        logger.Warning("Game", "b");
        logger.Error("Game", "c");

        Assert.Equal("c", Assert.Single(Enumerate(sink)).Message);
    }

    [Theory]
    [InlineData(LogLevel.Debug, false)]
    [InlineData(LogLevel.Info, true)]
    [InlineData(LogLevel.Error, true)]
    public void IsEnabled_follows_the_minimum_level(LogLevel level, bool expected)
    {
        Assert.Equal(expected, new Logger().IsEnabled(level));
    }

    [Fact]
    public void Each_level_helper_logs_at_its_own_level()
    {
        var (logger, sink) = Create();
        logger.MinimumLevel = LogLevel.Debug;

        logger.Debug("c", "1");
        logger.Info("c", "2");
        logger.Warning("c", "3");
        logger.Error("c", "4");

        Assert.Equal(
            new[] { LogLevel.Debug, LogLevel.Info, LogLevel.Warning, LogLevel.Error },
            Enumerate(sink).Select(e => e.Level).ToArray());
    }

    [Fact]
    public void Logging_with_no_sinks_is_harmless()
    {
        new Logger().Info("Game", "nobody listens");
    }

    [Fact]
    public void Empty_category_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Logger().Info("", "x"));
    }

    [Fact]
    public void Null_message_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Logger().Info("Game", null!));
    }

    [Fact]
    public void Adding_the_same_sink_twice_is_rejected()
    {
        var sink = new MemoryLogSink();
        var logger = new Logger().AddSink(sink);

        Assert.Throws<ArgumentException>(() => logger.AddSink(sink));
    }

    [Fact]
    public void A_throwing_sink_is_not_swallowed()
    {
        var logger = new Logger().AddSink(new ThrowingSink());

        Assert.Throws<InvalidOperationException>(() => logger.Info("Game", "x"));
    }

    private static List<LogEntry> Enumerate(MemoryLogSink sink)
    {
        var entries = new List<LogEntry>();
        for (int i = 0; i < sink.Count; i++) entries.Add(sink[i]);
        return entries;
    }

    private sealed class ThrowingSink : ILogSink
    {
        public void Write(in LogEntry entry) => throw new InvalidOperationException("disk full");
    }
}
