using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Runtime.Editor.Undo;

namespace Engine.Runtime.Tests.Editor.Undo;

public class RevertToPrefabCommandTests
{
    private readonly World _world = new();
    private readonly EditContext _context;

    public RevertToPrefabCommandTests() => _context = new EditContext(_world);

    [Fact]
    public void Do_runs_the_revert_and_Undo_brings_back_the_edited_components()
    {
        var e = _world.CreateEntity();
        _world.Add(e, Name.Of("Edited"));
        _world.Add(e, Transform.At(new System.Numerics.Vector3(5, 0, 0)));
        var command = new RevertToPrefabCommand(_context, e, entity =>
        {
            _world.Set(entity, Name.Of("Original"));
            _world.Remove<Transform>(entity);
        }, "thing");

        command.Do(_context);
        Assert.Equal("Original", _world.Get<Name>(e).Value);
        Assert.False(_world.Has<Transform>(e));

        command.Undo(_context);
        Assert.Equal("Edited", _world.Get<Name>(e).Value);
        Assert.Equal(5f, _world.Get<Transform>(e).Position.X);

        command.Do(_context);
        Assert.Equal("Original", _world.Get<Name>(e).Value);
        Assert.Equal("Revert thing to prefab", command.Description);
    }
}
