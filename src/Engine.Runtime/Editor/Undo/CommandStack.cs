namespace Engine.Runtime.Editor.Undo;

/// <summary>
/// The undo and redo history. New commands go on the undo stack; undoing moves one to the redo stack; making any new change throws the
/// redo stack away (the future it described no longer follows from the present).
/// </summary>
public sealed class CommandStack(EditContext context, int capacity = CommandStack.DefaultCapacity)
{
    /// <summary>How many commands are remembered unless another capacity is given; the oldest are dropped beyond it.</summary>
    public const int DefaultCapacity = 256;

    private readonly List<IEditCommand> _undo = new();
    private readonly List<IEditCommand> _redo = new();

    /// <summary>The world and tracker the commands act on.</summary>
    public EditContext Context { get; } = context;

    /// <summary>True when there is something to undo.</summary>
    public bool CanUndo => _undo.Count > 0;

    /// <summary>True when there is something to redo.</summary>
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Number of commands that can be undone.</summary>
    public int UndoCount => _undo.Count;

    /// <summary>Number of commands that can be redone.</summary>
    public int RedoCount => _redo.Count;

    /// <summary>Description of what <see cref="Undo"/> would revert, or null when empty.</summary>
    public string? UndoDescription => _undo.Count > 0 ? _undo[^1].Description : null;

    /// <summary>Description of what <see cref="Redo"/> would reapply, or null when empty.</summary>
    public string? RedoDescription => _redo.Count > 0 ? _redo[^1].Description : null;

    /// <summary>The commands that can be undone, oldest first (the last one is what <see cref="Undo"/> would revert).</summary>
    public IReadOnlyList<IEditCommand> UndoCommands => _undo;

    /// <summary>The commands that can be redone, in the order they would be redone: the first is what <see cref="Redo"/> would reapply.</summary>
    public IEnumerable<IEditCommand> RedoCommands => Enumerable.Reverse(_redo);

    /// <summary>Undoes up to <paramref name="count"/> commands, newest first (the History window's "go back to here").</summary>
    /// <param name="count">How many steps to revert.</param>
    /// <returns>The last command that was undone, or null when nothing was.</returns>
    public IEditCommand? UndoMany(int count)
    {
        IEditCommand? last = null;
        for (int i = 0; i < count; i++) last = Undo() ?? last;
        return last;
    }

    /// <summary>Redoes up to <paramref name="count"/> commands (the History window's "go forward to here").</summary>
    /// <param name="count">How many steps to reapply.</param>
    /// <returns>The last command that was redone, or null when nothing was.</returns>
    public IEditCommand? RedoMany(int count)
    {
        IEditCommand? last = null;
        for (int i = 0; i < count; i++) last = Redo() ?? last;
        return last;
    }

    /// <summary>Applies <paramref name="command"/> and remembers it.</summary>
    /// <param name="command">The change to make.</param>
    public void Execute(IEditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Do(Context);
        Push(command);
    }

    /// <summary>
    /// Remembers a change that has <b>already</b> been made, such as a drag that was applied live frame by frame and is recorded once
    /// when it ends. Does not call <see cref="IEditCommand.Do"/>.
    /// </summary>
    /// <param name="command">A command whose effect is already in the world.</param>
    public void Push(IEditCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _redo.Clear();
        _undo.Add(command);
        if (_undo.Count > capacity) _undo.RemoveAt(0);
    }

    /// <summary>Reverts the most recent command.</summary>
    /// <returns>The command that was undone, or null when there was nothing to undo.</returns>
    public IEditCommand? Undo()
    {
        if (_undo.Count == 0) return null;

        var command = _undo[^1];
        command.Undo(Context);
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(command);
        return command;
    }

    /// <summary>Reapplies the most recently undone command.</summary>
    /// <returns>The command that was redone, or null when there was nothing to redo.</returns>
    public IEditCommand? Redo()
    {
        if (_redo.Count == 0) return null;

        var command = _redo[^1];
        command.Do(Context);
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(command);
        return command;
    }

    /// <summary>Forgets all history and every tracked entity. Call when the world is replaced or changed outside the editor.</summary>
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        Context.Entities.Clear();
    }
}
