using Engine.Core.Logging;

namespace Engine.Core.Tests.Logging;

public class MemoryLogSinkTests
{
    private static LogEntry Entry(string message) => new(new DateTime(2026, 1, 1), LogLevel.Info, "Test", message);

    [Fact]
    public void Entries_are_returned_oldest_first()
    {
        var sink = new MemoryLogSink(4);

        sink.Write(Entry("a"));
        sink.Write(Entry("b"));

        Assert.Equal(2, sink.Count);
        Assert.Equal("a", sink[0].Message);
        Assert.Equal("b", sink[1].Message);
    }

    [Fact]
    public void When_full_the_oldest_entries_are_overwritten()
    {
        var sink = new MemoryLogSink(3);

        foreach (string message in new[] { "a", "b", "c", "d", "e" }) sink.Write(Entry(message));

        Assert.Equal(3, sink.Count);
        Assert.Equal(new[] { "c", "d", "e" }, new[] { sink[0].Message, sink[1].Message, sink[2].Message });
    }

    [Fact]
    public void TotalWritten_counts_overwritten_entries_too()
    {
        var sink = new MemoryLogSink(2);

        for (int i = 0; i < 5; i++) sink.Write(Entry("x"));

        Assert.Equal(5, sink.TotalWritten);
        Assert.Equal(2, sink.Count);
    }

    [Fact]
    public void Clear_empties_the_sink_but_keeps_the_running_total()
    {
        var sink = new MemoryLogSink(2);
        sink.Write(Entry("a"));
        sink.Write(Entry("b"));
        sink.Write(Entry("c"));

        sink.Clear();
        sink.Write(Entry("d"));

        Assert.Equal(1, sink.Count);
        Assert.Equal("d", sink[0].Message);
        Assert.Equal(4, sink.TotalWritten);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(5)]
    public void Index_outside_the_held_entries_is_rejected(int index)
    {
        var sink = new MemoryLogSink(5);
        sink.Write(Entry("a"));
        sink.Write(Entry("b"));

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = sink[index]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Non_positive_capacity_is_rejected(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MemoryLogSink(capacity));
    }
}
