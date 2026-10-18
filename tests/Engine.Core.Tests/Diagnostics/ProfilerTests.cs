using Engine.Core.Diagnostics;
using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Diagnostics;

public class ProfilerTests
{
    // 1000 ticks per second, so one tick is exactly one millisecond.
    private const long TicksPerSecond = 1000;

    private static (Profiler Profiler, FakeClock Clock) Create()
    {
        var clock = new FakeClock();
        return (new Profiler(clock.Read, TicksPerSecond), clock);
    }

    [Fact]
    public void GetSection_returns_the_same_id_for_the_same_name()
    {
        var (profiler, _) = Create();

        Assert.Equal(profiler.GetSection("a"), profiler.GetSection("a"));
        Assert.NotEqual(profiler.GetSection("a"), profiler.GetSection("b"));
        Assert.Equal(2, profiler.SectionCount);
    }

    [Fact]
    public void GetSection_rejects_an_empty_name()
    {
        var (profiler, _) = Create();

        Assert.Throws<ArgumentException>(() => profiler.GetSection(" "));
    }

    [Fact]
    public void Constructor_rejects_non_positive_tick_rate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Profiler(() => 0, 0));
    }

    [Fact]
    public void Measured_time_becomes_the_last_frame_value_after_BeginFrame()
    {
        var (profiler, clock) = Create();
        int section = profiler.GetSection("work");

        long start = profiler.Begin();
        clock.Advance(8);
        profiler.End(section, start);
        Assert.Equal(0f, profiler.LastMs(section)); // not rolled into a frame yet

        profiler.BeginFrame();

        Assert.Equal(8f, profiler.LastMs(section));
    }

    [Fact]
    public void Repeated_runs_in_one_frame_add_up()
    {
        var (profiler, clock) = Create();
        int section = profiler.GetSection("work");

        for (int i = 0; i < 3; i++)
        {
            long start = profiler.Begin();
            clock.Advance(2);
            profiler.End(section, start);
        }

        profiler.BeginFrame();

        Assert.Equal(6f, profiler.LastMs(section));
    }

    [Fact]
    public void A_new_frame_starts_from_zero()
    {
        var (profiler, clock) = Create();
        int section = profiler.GetSection("work");
        long start = profiler.Begin();
        clock.Advance(4);
        profiler.End(section, start);
        profiler.BeginFrame();

        profiler.BeginFrame(); // nothing measured in this frame

        Assert.Equal(0f, profiler.LastMs(section));
    }

    [Fact]
    public void Average_and_max_cover_the_recorded_frames()
    {
        var (profiler, clock) = Create();
        int section = profiler.GetSection("work");

        foreach (long cost in new long[] { 2, 4, 8 })
        {
            long start = profiler.Begin();
            clock.Advance(cost);
            profiler.End(section, start);
            profiler.BeginFrame();
        }

        Assert.Equal(14f / 3f, profiler.AverageMs(section), 4);
        Assert.Equal(8f, profiler.MaxMs(section));
    }

    [Fact]
    public void Average_is_zero_before_any_frame()
    {
        var (profiler, _) = Create();
        int section = profiler.GetSection("work");

        Assert.Equal(0f, profiler.AverageMs(section));
    }

    [Fact]
    public void History_wraps_and_reports_the_oldest_sample()
    {
        var (profiler, clock) = Create();
        int section = profiler.GetSection("work");

        // Record HistoryLength + 2 frames costing 1, 2, 3, ... ms: the two oldest (1 and 2) are overwritten.
        for (int frame = 1; frame <= Profiler.HistoryLength + 2; frame++)
        {
            long start = profiler.Begin();
            clock.Advance(frame);
            profiler.End(section, start);
            profiler.BeginFrame();
        }

        var history = profiler.History(section, out int oldest);

        Assert.Equal(3f, history[oldest]);
        Assert.Equal((float)(Profiler.HistoryLength + 2), history[(oldest + Profiler.HistoryLength - 1) % Profiler.HistoryLength]);
    }

    [Fact]
    public void History_starts_at_zero_until_the_buffer_is_full()
    {
        var (profiler, clock) = Create();
        int section = profiler.GetSection("work");
        long start = profiler.Begin();
        clock.Advance(5);
        profiler.End(section, start);
        profiler.BeginFrame();

        var history = profiler.History(section, out int oldest);

        Assert.Equal(0, oldest);
        Assert.Equal(5f, history[0]);
    }

    [Fact]
    public void SystemGroup_reports_each_system_under_its_own_section()
    {
        var (profiler, clock) = Create();
        var group = new SystemGroup()
            .Add(new CostlySystem(clock, 3))
            .Add(new LogSystem("log", new List<string>()));
        group.Profile(profiler, "Fixed");

        group.Run(new World(), 0.016);
        profiler.BeginFrame();

        Assert.Equal(3f, profiler.LastMs(profiler.GetSection("Fixed/CostlySystem")));
        Assert.Equal(0f, profiler.LastMs(profiler.GetSection("Fixed/LogSystem")));
    }

    [Fact]
    public void SystemGroup_profiles_systems_added_after_Profile()
    {
        var (profiler, clock) = Create();
        var group = new SystemGroup();
        group.Profile(profiler, "Frame");
        group.Add(new CostlySystem(clock, 5));

        group.Run(new World(), 0.016);
        profiler.BeginFrame();

        Assert.Equal(5f, profiler.LastMs(profiler.GetSection("Frame/CostlySystem")));
    }

    [Fact]
    public void SystemGroup_still_runs_systems_without_a_profiler()
    {
        var log = new List<string>();
        var group = new SystemGroup().Add(new LogSystem("only", log));

        group.Run(new World(), 0.016);

        Assert.Equal(new[] { "only" }, log);
    }
}
