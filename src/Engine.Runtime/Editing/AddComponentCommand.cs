using Engine.Core.Ecs;

namespace Engine.Runtime.Editing;

/// <summary>Adds a component to an entity; undo removes it. Use <see cref="CommandStack.Execute"/>: the command makes the change itself.</summary>
public sealed class AddComponentCommand : IEditCommand
{
    private readonly int _entityId;
    private readonly Type _type;
    private readonly object? _value;

    /// <summary>Creates the command.</summary>
    /// <param name="context">Used to track the entity.</param>
    /// <param name="entity">The entity to add to.</param>
    /// <param name="type">The component type.</param>
    /// <param name="value">The starting value, or null for <c>default</c>.</param>
    /// <param name="label">What to call the entity in the description.</param>
    public AddComponentCommand(EditContext context, Entity entity, Type type, object? value, string label)
    {
        _entityId = context.Entities.IdOf(entity);
        _type = type;
        _value = value;
        Description = $"Add {type.Name} to {label}";
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context) => context.World.AddBoxed(context.Resolve(_entityId), _type, _value);

    /// <inheritdoc />
    public void Undo(EditContext context) => context.World.Remove(context.Resolve(_entityId), _type);

    /// <inheritdoc />
    public Entity Affected(EditContext context) => context.ResolveOrNull(_entityId);
}
