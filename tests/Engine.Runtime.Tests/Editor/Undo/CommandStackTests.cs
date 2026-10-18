using Engine.Core.Ecs;
using Engine.Runtime.Editor.Undo;

namespace Engine.Runtime.Tests.Editor.Undo;

public class CommandStackTests
{
    private readonly World _world = new();
    private readonly CommandStack _stack;
    private readonly List<string> _log = new();

    public CommandStackTests() => _stack = new CommandStack(new EditContext(_world));

    private sealed class RecordingCommand(string name, List<string> log) : IEditCommand
    {
        public string Description => name;
        public void Do(EditContext context) => log.Add("do " + name);
        public void Undo(EditContext context) => log.Add("undo " + name);
        public Entity Affected(EditContext context) => Entity.Null;
    }

    private RecordingCommand Command(string name) => new(name, _log);

    [Fact]
    public void A_new_stack_has_nothing_to_undo_or_redo()
    {
        Assert.False(_stack.CanUndo);
        Assert.False(_stack.CanRedo);
        Assert.Null(_stack.UndoDescription);
        Assert.Null(_stack.RedoDescription);
        Assert.Null(_stack.Undo());
        Assert.Null(_stack.Redo());
    }

    [Fact]
    public void Execute_applies_the_command_and_makes_it_undoable()
    {
        _stack.Execute(Command("a"));

        Assert.Equal(new[] { "do a" }, _log);
        Assert.True(_stack.CanUndo);
        Assert.Equal("a", _stack.UndoDescription);
    }

    [Fact]
    public void Push_remembers_a_change_without_applying_it_again()
    {
        _stack.Push(Command("a"));

        Assert.Empty(_log);
        Assert.True(_stack.CanUndo);
    }

    [Fact]
    public void Undo_reverts_the_latest_command_first()
    {
        _stack.Execute(Command("a"));
        _stack.Execute(Command("b"));
        _log.Clear();

        var undone = _stack.Undo();

        Assert.Equal("b", undone!.Description);
        Assert.Equal(new[] { "undo b" }, _log);
        Assert.Equal("a", _stack.UndoDescription);
        Assert.Equal("b", _stack.RedoDescription);
    }

    [Fact]
    public void Redo_reapplies_the_undone_command()
    {
        _stack.Execute(Command("a"));
        _stack.Undo();
        _log.Clear();

        var redone = _stack.Redo();

        Assert.Equal("a", redone!.Description);
        Assert.Equal(new[] { "do a" }, _log);
        Assert.False(_stack.CanRedo);
        Assert.True(_stack.CanUndo);
    }

    [Fact]
    public void Undo_and_redo_walk_back_and_forth_in_order()
    {
        _stack.Execute(Command("a"));
        _stack.Execute(Command("b"));
        _stack.Execute(Command("c"));
        _log.Clear();

        _stack.Undo();
        _stack.Undo();
        _stack.Redo();

        Assert.Equal(new[] { "undo c", "undo b", "do b" }, _log);
    }

    [Fact]
    public void A_new_change_discards_the_redo_history()
    {
        _stack.Execute(Command("a"));
        _stack.Undo();

        _stack.Execute(Command("b"));

        Assert.False(_stack.CanRedo);
        Assert.Equal(1, _stack.UndoCount);
    }

    [Fact]
    public void Pushing_also_discards_the_redo_history()
    {
        _stack.Execute(Command("a"));
        _stack.Undo();

        _stack.Push(Command("b"));

        Assert.False(_stack.CanRedo);
    }

    [Fact]
    public void The_oldest_commands_are_dropped_beyond_the_capacity()
    {
        var stack = new CommandStack(new EditContext(_world), capacity: 3);

        foreach (string name in new[] { "a", "b", "c", "d" }) stack.Execute(Command(name));

        Assert.Equal(3, stack.UndoCount);
        stack.Undo();
        stack.Undo();
        stack.Undo();
        Assert.False(stack.CanUndo);
        Assert.Equal(new[] { "do a", "do b", "do c", "do d", "undo d", "undo c", "undo b" }, _log);
    }

    [Fact]
    public void Clear_forgets_history_and_tracked_entities()
    {
        var entity = _world.CreateEntity();
        _stack.Context.Entities.IdOf(entity);
        _stack.Execute(Command("a"));
        _stack.Undo();

        _stack.Clear();

        Assert.False(_stack.CanUndo);
        Assert.False(_stack.CanRedo);
        Assert.True(_stack.Context.Entities.Lookup(1).IsNull);
    }

    [Fact]
    public void Null_commands_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => _stack.Execute(null!));
        Assert.Throws<ArgumentNullException>(() => _stack.Push(null!));
    }

    [Fact]
    public void UndoMany_and_RedoMany_step_several_commands_and_stop_at_the_ends()
    {
        _stack.Execute(Command("a"));
        _stack.Execute(Command("b"));
        _stack.Execute(Command("c"));
        _log.Clear();

        var last = _stack.UndoMany(5);

        Assert.Equal(new[] { "undo c", "undo b", "undo a" }, _log);
        Assert.Equal("a", last!.Description);
        Assert.Null(_stack.UndoMany(2));

        _log.Clear();
        _stack.RedoMany(2);

        Assert.Equal(new[] { "do a", "do b" }, _log);
        Assert.Equal("c", _stack.RedoDescription);
    }
}
