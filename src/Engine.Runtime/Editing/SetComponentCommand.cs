using Engine.Core.Ecs;

namespace Engine.Runtime.Editing;

/// <summary>
/// A component changed from one value to another: a gizmo drag, an inspector edit. Created with the values it already changed
/// (see <see cref="CommandStack.Push"/>), so a drag that updated the world every frame becomes one undo step.
/// </summary>
public sealed class SetComponentCommand : IEditCommand
{
    private readonly int _entityId;
    private readonly Type _type;
    private readonly object _before;
    private readonly object _after;

    /// <summary>Creates the command.</summary>
    /// <param name="context">Used to track the entity.</param>
    /// <param name="entity">The entity that was changed.</param>
    /// <param name="type">The component type.</param>
    /// <param name="before">The value before the change (a box that nothing else mutates).</param>
    /// <param name="after">The value after the change.</param>
    /// <param name="label">What to call the entity in the description (its name).</param>
    public SetComponentCommand(EditContext context, Entity entity, Type type, object before, object after, string label)
    {
        _entityId = context.Entities.IdOf(entity);
        _type = type;
        _before = before;
        _after = after;
        Description = type.Name == "Transform" ? $"Move {label}" : $"Edit {type.Name} of {label}";
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context) => context.SetComponent(context.Resolve(_entityId), _type, _after);

    /// <inheritdoc />
    public void Undo(EditContext context) => context.SetComponent(context.Resolve(_entityId), _type, _before);

    /// <inheritdoc />
    public Entity Affected(EditContext context) => context.ResolveOrNull(_entityId);
}
