namespace Engine.Core.Ecs;

/// <summary>A unit of behaviour that runs over the world's components.</summary>
public interface ISystem
{
    void Update(World world, double dt);
}

/// <summary>Runs systems in the order they were added.</summary>
public sealed class SystemGroup
{
    private readonly List<ISystem> _systems = new();

    public IReadOnlyList<ISystem> Systems => _systems;

    public SystemGroup Add(ISystem system)
    {
        _systems.Add(system);
        return this;
    }

    public void Run(World world, double dt)
    {
        foreach (var system in _systems) system.Update(world, dt);
    }
}
