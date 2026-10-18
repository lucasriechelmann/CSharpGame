using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Runtime.Editing;

/// <summary>
/// An entity was attached to a new parent (or detached). The entity keeps its place in the world, so its local transform is rewritten
/// to match its new space; both the old and the new transform are remembered, so undo restores the exact old values rather than
/// converting back through floating-point maths.
/// </summary>
public sealed class ReparentCommand : IEditCommand
{
    private readonly int _childId;
    private readonly int _oldParentId; // 0 = root
    private readonly int _newParentId;
    private readonly Transform? _oldLocal;
    private readonly Transform? _newLocal;

    private ReparentCommand(int childId, int oldParentId, int newParentId, Transform? oldLocal, Transform? newLocal, string description)
    {
        _childId = childId;
        _oldParentId = oldParentId;
        _newParentId = newParentId;
        _oldLocal = oldLocal;
        _newLocal = newLocal;
        Description = description;
    }

    /// <summary>
    /// Works out the change without applying it; run it with <see cref="CommandStack.Execute"/>.
    /// </summary>
    /// <param name="context">The world and tracker.</param>
    /// <param name="child">The entity to move.</param>
    /// <param name="newParent">The new parent, or <see cref="Entity.Null"/> to make it a root.</param>
    /// <param name="label">What to call the child in the description.</param>
    /// <returns>The command, or null when the entity already has that parent (nothing to do).</returns>
    /// <exception cref="InvalidOperationException">The new parent is the child or one of its descendants.</exception>
    public static ReparentCommand? Create(EditContext context, Entity child, Entity newParent, string label)
    {
        var world = context.World;
        var oldParent = TransformHierarchy.GetParent(world, child);
        if (oldParent == newParent) return null;
        if (!newParent.IsNull && TransformHierarchy.IsSelfOrDescendantOf(world, newParent, child))
            throw new InvalidOperationException($"{newParent} is {child} or inside it, so it cannot become its parent.");

        Transform? oldLocal = null;
        Transform? newLocal = null;
        if (world.TryGet<Transform>(child, out var current))
        {
            oldLocal = current;
            newLocal = TransformHierarchy.ToLocal(world, newParent, TransformHierarchy.GetWorldTransform(world, child));
        }

        string target = newParent.IsNull ? "the root" : Inspector.EntityLabel.Of(world, newParent);
        return new ReparentCommand(
            context.Entities.IdOf(child),
            oldParent.IsNull ? 0 : context.Entities.IdOf(oldParent),
            newParent.IsNull ? 0 : context.Entities.IdOf(newParent),
            oldLocal,
            newLocal,
            $"Move {label} to {target}");
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context) => Apply(context, _newParentId, _newLocal);

    /// <inheritdoc />
    public void Undo(EditContext context) => Apply(context, _oldParentId, _oldLocal);

    /// <inheritdoc />
    public Entity Affected(EditContext context) => context.ResolveOrNull(_childId);

    private void Apply(EditContext context, int parentId, Transform? local)
    {
        var child = context.Resolve(_childId);
        if (parentId == 0) context.World.Remove<Parent>(child);
        else context.World.Set(child, new Parent { Value = context.Resolve(parentId) });

        if (local is { } transform) context.SetComponent(child, typeof(Transform), transform);
    }
}
