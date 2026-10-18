using Engine.Core.Ecs;

namespace Engine.Runtime.Editor.Undo;

/// <summary>
/// One undoable change to the world. A command is a small record of what changed (not a closure over the world) so it can be
/// applied, reverted and applied again any number of times.
/// </summary>
/// <remarks>
/// Commands refer to entities by a tracked id (<see cref="EntityTracker"/>), never by <see cref="Entity"/>: undoing a delete
/// creates a new entity with a new handle, and every older command about it must still find it.
/// </remarks>
public interface IEditCommand
{
    /// <summary>Short text for the Edit menu, such as "Move Hero #4".</summary>
    string Description { get; }

    /// <summary>Applies the change (also used to redo it).</summary>
    /// <param name="context">The world and the entity tracker.</param>
    void Do(EditContext context);

    /// <summary>Reverts exactly what <see cref="Do"/> changed.</summary>
    /// <param name="context">The world and the entity tracker.</param>
    void Undo(EditContext context);

    /// <summary>
    /// The entity the editor should select once this command was undone or redone, or <see cref="Entity.Null"/> when it no longer exists
    /// (a created entity after undo, a deleted one after redo).
    /// </summary>
    /// <param name="context">The world and the entity tracker.</param>
    Entity Affected(EditContext context);
}
