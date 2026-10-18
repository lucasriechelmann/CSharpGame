using Engine.Core.Ecs;

namespace Engine.Runtime.Editing;

/// <summary>Removes a component from an entity; undo puts back the value it had. Use <see cref="CommandStack.Execute"/>.</summary>
public sealed class RemoveComponentCommand : IEditCommand
{
    private readonly int _entityId;
    private readonly Type _type;
    private object? _removed;

    /// <summary>Creates the command.</summary>
    /// <param name="context">Used to track the entity.</param>
    /// <param name="entity">The entity to remove from.</param>
    /// <param name="type">The component type to remove.</param>
    /// <param name="label">What to call the entity in the description.</param>
    public RemoveComponentCommand(EditContext context, Entity entity, Type type, string label)
    {
        _entityId = context.Entities.IdOf(entity);
        _type = type;
        Description = $"Remove {type.Name} from {label}";
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context)
    {
        var entity = context.Resolve(_entityId);
        _removed = context.World.GetBoxed(entity, _type); // read at the moment of removal, so redo removes whatever the value is then
        context.World.Remove(entity, _type);
    }

    /// <inheritdoc />
    public void Undo(EditContext context) => context.World.AddBoxed(context.Resolve(_entityId), _type, _removed);

    /// <inheritdoc />
    public Entity Affected(EditContext context) => context.ResolveOrNull(_entityId);
}
