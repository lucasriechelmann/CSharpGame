using Engine.Core.Diagnostics;

namespace Engine.Core.Ecs;

/// <summary>An ordered list of systems that run one after another.</summary>
public sealed class SystemGroup
{
    private readonly List<ISystem> _systems = new();
    private readonly List<int> _sections = new();
    private Profiler? _profiler;

    /// <summary>The systems in execution order.</summary>
    public IReadOnlyList<ISystem> Systems => _systems;

    /// <summary>
    /// When set, each system's run time is recorded under a section named <c>group/SystemType</c>. Sections are resolved here and
    /// in <see cref="Add"/>, never while running, so profiling a frame allocates nothing.
    /// </summary>
    /// <param name="profiler">The profiler to report to.</param>
    /// <param name="groupName">Prefix for section names, e.g. "Fixed".</param>
    public void Profile(Profiler profiler, string groupName)
    {
        _profiler = profiler;
        GroupName = groupName;
        _sections.Clear();
        foreach (var system in _systems) _sections.Add(profiler.GetSection(SectionName(system)));
    }

    /// <summary>The prefix used for profiler section names; empty until <see cref="Profile"/> is called.</summary>
    public string GroupName { get; private set; } = string.Empty;

    /// <summary>Appends a system; it runs after all previously added systems.</summary>
    /// <returns>This group, so calls can be chained.</returns>
    public SystemGroup Add(ISystem system)
    {
        _systems.Add(system);
        if (_profiler != null) _sections.Add(_profiler.GetSection(SectionName(system)));
        return this;
    }

    /// <summary>Runs every system in insertion order.</summary>
    /// <param name="world">The world passed to each system.</param>
    /// <param name="dt">Seconds elapsed, forwarded to each system.</param>
    public void Run(World world, double dt)
    {
        var profiler = _profiler;
        for (int i = 0; i < _systems.Count; i++)
        {
            if (profiler == null)
            {
                _systems[i].Update(world, dt);
                continue;
            }

            long start = profiler.Begin();
            _systems[i].Update(world, dt);
            profiler.End(_sections[i], start);
        }
    }

    private string SectionName(ISystem system) => $"{GroupName}/{system.GetType().Name}";
}
