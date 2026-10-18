using Engine.Core.Ecs;

namespace Engine.Runtime.Editor.Undo;

/// <summary>
/// An entity was created or deleted. Undoing a creation destroys the entity; undoing a deletion recreates it from a snapshot and
/// re-points its tracked id at the new entity, so earlier commands about it (a move, an edit) still find it.
/// </summary>
public sealed class EntityLifetimeCommand : IEditCommand
{
    private readonly int _entityId;
    private readonly bool _creates;
    private EntitySnapshot _snapshot;

    private EntityLifetimeCommand(int entityId, bool creates, EntitySnapshot snapshot, string description)
    {
        _entityId = entityId;
        _creates = creates;
        _snapshot = snapshot;
        Description = description;
    }

    /// <summary>
    /// Describes an entity that was <b>just created</b> (a new entity, a duplicate): record it with <see cref="CommandStack.Push"/>.
    /// Undo destroys it, redo recreates it from the snapshot taken here.
    /// </summary>
    /// <param name="context">The world and tracker.</param>
    /// <param name="entity">The new entity, with its components already added.</param>
    /// <param name="label">What to call it in the description.</param>
    public static EntityLifetimeCommand ForCreated(EditContext context, Entity entity, string label) =>
        new(context.Entities.IdOf(entity), creates: true, EntitySnapshot.Capture(context.World, entity, context.Entities), $"Create {label}");

    /// <summary>Describes deleting <paramref name="entity"/>: run it with <see cref="CommandStack.Execute"/>.</summary>
    /// <param name="context">The world and tracker.</param>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="label">What to call it in the description.</param>
    public static EntityLifetimeCommand ForDeleted(EditContext context, Entity entity, string label) =>
        new(context.Entities.IdOf(entity), creates: false, EntitySnapshot.Capture(context.World, entity, context.Entities), $"Delete {label}");

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context)
    {
        if (_creates) Spawn(context);
        else Destroy(context);
    }

    /// <inheritdoc />
    public void Undo(EditContext context)
    {
        if (_creates) Destroy(context);
        else Spawn(context);
    }

    /// <inheritdoc />
    public Entity Affected(EditContext context) => context.ResolveOrNull(_entityId);

    private void Spawn(EditContext context) => context.Entities.Bind(_entityId, _snapshot.Restore(context.World, context.Entities));

    private void Destroy(EditContext context)
    {
        var entity = context.Resolve(_entityId);
        _snapshot = EntitySnapshot.Capture(context.World, entity, context.Entities); // keep the latest state, so the entity comes back as it was
        context.World.DestroyEntity(entity);
        context.Entities.Unbind(_entityId);
    }
}
