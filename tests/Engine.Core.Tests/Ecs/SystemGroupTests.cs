using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class SystemGroupTests
{
    [Fact]
    public void Runs_in_insertion_order()
    {
        var world = new World();
        var log = new List<string>();
        var group = new SystemGroup()
            .Add(new LogSystem("first", log))
            .Add(new LogSystem("second", log));

        group.Run(world, 0.016);

        Assert.Equal(new[] { "first", "second" }, log);
    }

    [Fact]
    public void Movement_system_integrates_velocity()
    {
        var world = new World();
        var e = world.CreateEntity();
        world.Add(e, new Position());
        world.Add(e, new Velocity { X = 4, Y = -2 });

        var group = new SystemGroup().Add(new MovementSystem());
        group.Run(world, 0.5);

        Assert.Equal(2, world.Get<Position>(e).X);
        Assert.Equal(-1, world.Get<Position>(e).Y);
    }
}
