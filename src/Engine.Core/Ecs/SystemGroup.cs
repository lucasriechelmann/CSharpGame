namespace Engine.Core.Ecs;

/// <summary>An ordered list of systems that run one after another.</summary>
public sealed class SystemGroup
{
    private readonly List<ISystem> _systems = new();

    /// <summary>The systems in execution order.</summary>
    public IReadOnlyList<ISystem> Systems => _systems;

    /// <summary>Appends a system; it runs after all previously added systems.</summary>
    /// <returns>This group, so calls can be chained.</returns>
    public SystemGroup Add(ISystem system)
    {
        _systems.Add(system);
        return this;
    }

    /// <summary>Runs every system in insertion order.</summary>
    /// <param name="world">The world passed to each system.</param>
    /// <param name="dt">Seconds elapsed, forwarded to each system.</param>
    public void Run(World world, double dt)
    {
        foreach (var system in _systems) system.Update(world, dt);
    }
}
