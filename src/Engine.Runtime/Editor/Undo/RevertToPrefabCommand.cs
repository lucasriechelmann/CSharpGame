using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Runtime.Editor.Undo;

/// <summary>
/// Puts a prefab instance back to what its prefab says, undoably. The prefab work itself is a callback (the editor passes
/// <c>SceneSerializer.RevertToPrefab</c>), so this command only remembers the components before and after and swaps between the two states.
/// </summary>
public sealed class RevertToPrefabCommand : IEditCommand
{
    private readonly int _entityId;
    private readonly Action<Entity> _revert;
    private List<(Type Type, object Value)> _before = new();
    private List<(Type Type, object Value)> _after = new();

    /// <summary>Creates the command for one entity.</summary>
    /// <param name="context">The edit context, used to track the entity by id.</param>
    /// <param name="entity">The prefab instance to revert.</param>
    /// <param name="revert">Applies the prefab to the entity (fails by throwing, before changing anything).</param>
    /// <param name="label">The entity's name for the history text.</param>
    public RevertToPrefabCommand(EditContext context, Entity entity, Action<Entity> revert, string label)
    {
        _entityId = context.Entities.IdOf(entity);
        _revert = revert;
        Description = $"Revert {label} to prefab";
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context)
    {
        var entity = context.Resolve(_entityId);
        _before = Capture(context.World, entity);
        _revert(entity);
        _after = Capture(context.World, entity);
    }

    /// <inheritdoc />
    public void Undo(EditContext context) => Apply(context.World, context.Resolve(_entityId), _before);

    /// <inheritdoc />
    public Entity Affected(EditContext context) => context.ResolveOrNull(_entityId);

    /// <summary>The entity's components except the parent link, which a revert never changes.</summary>
    private static List<(Type Type, object Value)> Capture(World world, Entity entity)
    {
        var types = new List<Type>();
        world.GetComponentTypes(entity, types);
        var state = new List<(Type, object)>(types.Count);
        foreach (var type in types)
        {
            if (type != typeof(Parent)) state.Add((type, world.GetBoxed(entity, type)));
        }

        return state;
    }

    private static void Apply(World world, Entity entity, List<(Type Type, object Value)> target)
    {
        var current = new List<Type>();
        world.GetComponentTypes(entity, current);
        foreach (var type in current)
        {
            if (type != typeof(Parent) && !target.Exists(t => t.Type == type)) world.Remove(entity, type);
        }

        foreach (var (type, value) in target)
        {
            if (world.Has(entity, type)) world.SetBoxed(entity, type, value);
            else world.AddBoxed(entity, type, value);
        }
    }
}
