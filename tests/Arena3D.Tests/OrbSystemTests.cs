using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Physics3D;

namespace Arena3D.Tests;

public class OrbSystemTests
{
    private readonly World _world = new();

    private Entity Player(Vector3 position)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, new CharacterController3D());
        return e;
    }

    private Entity NewOrb(Vector3 position, float radius = 1f)
    {
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(position));
        _world.Add(e, new Orb { Radius = radius });
        return e;
    }

    [Fact]
    public void An_orb_within_reach_is_removed_and_reported_once()
    {
        int collected = 0;
        var system = new OrbCollectSystem(() => collected++);
        Player(Vector3.Zero);
        var near = NewOrb(new Vector3(0.5f, 0f, 0f));
        var far = NewOrb(new Vector3(5f, 0f, 0f));

        system.Update(_world, 1.0 / 60);
        system.Update(_world, 1.0 / 60);

        Assert.Equal(1, collected);
        Assert.False(_world.IsAlive(near));
        Assert.True(_world.IsAlive(far));
    }

    [Fact]
    public void Several_orbs_in_reach_are_each_reported()
    {
        int collected = 0;
        var system = new OrbCollectSystem(() => collected++);
        Player(Vector3.Zero);
        NewOrb(new Vector3(0.3f, 0f, 0f));
        NewOrb(new Vector3(0f, 0.3f, 0f));

        system.Update(_world, 1.0 / 60);

        Assert.Equal(2, collected);
    }

    [Fact]
    public void Nothing_is_collected_without_a_player()
    {
        int collected = 0;
        var system = new OrbCollectSystem(() => collected++);
        var orb = NewOrb(Vector3.Zero);

        system.Update(_world, 1.0 / 60);

        Assert.Equal(0, collected);
        Assert.True(_world.IsAlive(orb));
    }

    [Fact]
    public void An_orb_exactly_at_its_radius_still_counts_but_one_just_outside_does_not()
    {
        int collected = 0;
        var system = new OrbCollectSystem(() => collected++);
        Player(Vector3.Zero);
        NewOrb(new Vector3(1f, 0f, 0f), radius: 1f);
        NewOrb(new Vector3(1.01f, 0f, 0f), radius: 1f);

        system.Update(_world, 1.0 / 60);

        Assert.Equal(1, collected);
    }

    [Fact]
    public void Bobbing_moves_an_orb_around_its_resting_height_and_turns_it()
    {
        var system = new OrbBobSystem();
        var e = _world.CreateEntity();
        _world.Add(e, Transform.At(new Vector3(2f, 3f, 4f)));
        _world.Add(e, new OrbBob { Amplitude = 0.5f, Speed = 2f, Spin = 1f });

        float highest = float.MinValue, lowest = float.MaxValue;
        for (int i = 0; i < 400; i++)
        {
            system.Update(_world, 0.02);
            float y = _world.Get<Transform>(e).Position.Y;
            highest = Math.Max(highest, y);
            lowest = Math.Min(lowest, y);
        }

        Assert.InRange(highest, 3.45f, 3.5001f);
        Assert.InRange(lowest, 2.4999f, 2.55f);
        Assert.Equal(2f, _world.Get<Transform>(e).Position.X);
        Assert.Equal(4f, _world.Get<Transform>(e).Position.Z);
        Assert.NotEqual(Quaternion.Identity, _world.Get<Transform>(e).Rotation);
    }
}
