namespace Engine.Core.Ecs;

/// <summary>
/// Records structural changes (create/destroy/add/remove) so they can be applied after a query
/// finishes, when it is safe to reshape the component storage.
/// </summary>
public sealed class CommandBuffer
{
    private readonly List<Action<World>> _commands = new();

    public int Count => _commands.Count;

    public void Destroy(Entity entity) => _commands.Add(w =>
    {
        if (w.IsAlive(entity)) w.DestroyEntity(entity);
    });

    public void Add<T>(Entity entity, T value) => _commands.Add(w =>
    {
        if (w.IsAlive(entity)) w.Set(entity, value);
    });

    public void Remove<T>(Entity entity) => _commands.Add(w =>
    {
        if (w.IsAlive(entity)) w.Remove<T>(entity);
    });

    /// <summary>Applies all recorded commands in order, then clears the buffer.</summary>
    public void Playback(World world)
    {
        foreach (var command in _commands) command(world);
        _commands.Clear();
    }
}
