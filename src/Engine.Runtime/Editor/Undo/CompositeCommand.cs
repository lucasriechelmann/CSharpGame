using Engine.Core.Ecs;

namespace Engine.Runtime.Editor.Undo;

/// <summary>
/// Several commands that undo and redo as one step: deleting or duplicating an entity together with its children is one
/// <see cref="EntityLifetimeCommand"/> per entity, grouped here. Undo runs them in reverse, so list them in the order that is valid
/// for <see cref="Do"/> (parents created first, children deleted first).
/// </summary>
public sealed class CompositeCommand : IEditCommand
{
    private readonly IReadOnlyList<IEditCommand> _commands;
    private readonly int _affectedIndex;

    /// <summary>Groups <paramref name="commands"/>.</summary>
    /// <param name="description">Text for the Edit menu.</param>
    /// <param name="commands">The steps, in the order <see cref="Do"/> runs them.</param>
    /// <param name="affectedIndex">Which step's <see cref="Affected"/> names the entity to select afterwards (usually the subtree root).</param>
    public CompositeCommand(string description, IReadOnlyList<IEditCommand> commands, int affectedIndex)
    {
        if (commands.Count == 0) throw new ArgumentException("A composite needs at least one command.", nameof(commands));
        if (affectedIndex < 0 || affectedIndex >= commands.Count) throw new ArgumentOutOfRangeException(nameof(affectedIndex));

        Description = description;
        _commands = commands;
        _affectedIndex = affectedIndex;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Do(EditContext context)
    {
        for (int i = 0; i < _commands.Count; i++) _commands[i].Do(context);
    }

    /// <inheritdoc />
    public void Undo(EditContext context)
    {
        for (int i = _commands.Count - 1; i >= 0; i--) _commands[i].Undo(context);
    }

    /// <inheritdoc />
    public Entity Affected(EditContext context) => _commands[_affectedIndex].Affected(context);
}
