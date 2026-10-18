using Engine.Core.Ecs;
using Engine.Core.Tests.Fixtures;

namespace Engine.Core.Tests.Ecs;

public class CommandBufferTests
{
    [Fact]
    public void Applies_changes_after_the_query()
    {
        var world = new World();
        var doomed = world.CreateEntity();
        world.Add(doomed, new Position { X = -1 });
        var survivor = world.CreateEntity();
        world.Add(survivor, new Position { X = 1 });

        var buffer = new CommandBuffer();
        world.Query<Position>((Entity e, ref Position p) =>
        {
            if (p.X < 0) buffer.Destroy(e);
            else buffer.Add(e, new Velocity { X = 7 });
        });

        Assert.Equal(2, buffer.Count);
        buffer.Playback(world);

        Assert.Equal(0, buffer.Count);
        Assert.False(world.IsAlive(doomed));
        Assert.Equal(7, world.Get<Velocity>(survivor).X);
    }

    [Fact]
    public void Skips_entities_that_died_before_playback()
    {
        var world = new World();
        var e = world.CreateEntity();
        var buffer = new CommandBuffer();
        buffer.Add(e, new Position());
        buffer.Destroy(e);
        world.DestroyEntity(e);

        buffer.Playback(world); // must not throw
        Assert.Equal(0, world.EntityCount);
    }
}
