using Engine.Core.Logging;

namespace Engine.Core.Tests.Logging;

public class ConsoleLogSinkTests
{
    [Fact]
    public void Writes_one_formatted_line_per_entry()
    {
        var writer = new StringWriter();
        var logger = new Logger(() => new DateTime(2026, 1, 2, 9, 5, 7)).AddSink(new ConsoleLogSink(writer));

        logger.Info("Audio", "OpenAL ready");
        logger.Error("Scenes", "bad file");

        Assert.Equal(
            "09:05:07 INF Audio: OpenAL ready" + Environment.NewLine + "09:05:07 ERR Scenes: bad file" + Environment.NewLine,
            writer.ToString());
    }

    [Theory]
    [InlineData(LogLevel.Debug, "DBG")]
    [InlineData(LogLevel.Info, "INF")]
    [InlineData(LogLevel.Warning, "WRN")]
    [InlineData(LogLevel.Error, "ERR")]
    public void Tags_are_three_letters(LogLevel level, string expected)
    {
        Assert.Equal(expected, ConsoleLogSink.Tag(level));
    }

    [Fact]
    public void Unknown_level_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleLogSink.Tag((LogLevel)99));
    }
}
