namespace Engine.Core.Tests.Fixtures;

/// <summary>A hand-wound clock for profiler tests: time only moves when a test (or a <see cref="CostlySystem"/>) advances it.</summary>
internal sealed class FakeClock
{
    public long Ticks { get; private set; }

    public void Advance(long ticks) => Ticks += ticks;

    public long Read() => Ticks;
}
