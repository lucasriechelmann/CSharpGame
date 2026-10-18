namespace Engine.Core.Logging;

/// <summary>
/// Keeps the most recent entries in a fixed-size ring so the in-game log panel can show them. Old entries are overwritten
/// rather than the buffer growing, so a game that logs for hours uses a constant amount of memory.
/// </summary>
public sealed class MemoryLogSink : ILogSink
{
    private readonly LogEntry[] _entries;
    private int _head;

    /// <summary>Creates a sink that remembers the last <paramref name="capacity"/> entries.</summary>
    /// <param name="capacity">How many entries to keep; must be positive.</param>
    public MemoryLogSink(int capacity = 500)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        _entries = new LogEntry[capacity];
    }

    /// <summary>Number of entries currently held (at most the capacity).</summary>
    public int Count { get; private set; }

    /// <summary>How many entries have ever been written, including overwritten ones. A panel compares it between frames to know when to scroll.</summary>
    public long TotalWritten { get; private set; }

    /// <summary>The entry at <paramref name="index"/>, where 0 is the oldest one still held.</summary>
    /// <param name="index">Position from the oldest entry.</param>
    public LogEntry this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index), index, $"Only {Count} entries are held.");
            int start = Count == _entries.Length ? _head : 0;
            return _entries[(start + index) % _entries.Length];
        }
    }

    /// <inheritdoc />
    public void Write(in LogEntry entry)
    {
        _entries[_head] = entry;
        _head = (_head + 1) % _entries.Length;
        if (Count < _entries.Length) Count++;
        TotalWritten++;
    }

    /// <summary>Forgets every held entry. <see cref="TotalWritten"/> keeps counting.</summary>
    public void Clear()
    {
        Array.Clear(_entries);
        _head = 0;
        Count = 0;
    }
}
