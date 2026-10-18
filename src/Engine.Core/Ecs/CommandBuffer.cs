namespace Engine.Core.Ecs;

/// <summary>
/// Records structural changes (destroy/add/remove) so they can be applied after a query
/// finishes, when it is safe to reshape the component storage.
/// </summary>
public sealed class CommandBuffer
{
    private readonly List<Action<World>> _commands = new();

    /// <summary>Number of commands waiting to be played back.</summary>
    public int Count => _commands.Count;

    /// <summary>Queues destruction of an entity. Ignored at playback if it is already dead.</summary>
    public void Destroy(Entity entity) => _commands.Add(w =>
    {
        if (w.IsAlive(entity)) w.DestroyEntity(entity);
    });

    /// <summary>Queues adding (or overwriting) a component. Ignored at playback if the entity is dead.</summary>
    public void Add<T>(Entity entity, T value) => _commands.Add(w =>
    {
        if (w.IsAlive(entity)) w.Set(entity, value);
    });

    /// <summary>Queues removal of a component. Ignored at playback if the entity is dead.</summary>
    public void Remove<T>(Entity entity) => _commands.Add(w =>
    {
        if (w.IsAlive(entity)) w.Remove<T>(entity);
    });

    /// <summary>Applies all recorded commands in order, then clears the buffer.</summary>
    /// <param name="world">The world to modify. Must not be mid-query.</param>
    public void Playback(World world)
    {
        foreach (var command in _commands) command(world);
        _commands.Clear();
    }
}
