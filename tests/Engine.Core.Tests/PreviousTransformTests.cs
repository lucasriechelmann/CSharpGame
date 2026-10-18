using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Core.Tests;

public class PreviousTransformTests
{
    [Fact]
    public void Default_has_no_history()
    {
        Assert.False(default(PreviousTransform).HasHistory);
    }

    [Fact]
    public void Interpolating_without_history_returns_the_current_transform()
    {
        var current = Transform.At(10f, 20f, 3f);

        var result = PreviousTransform.Interpolate(current, default, 0.5f);

        Assert.Equal(current.Position, result.Position);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.25f, 2.5f)]
    [InlineData(0.5f, 5f)]
    [InlineData(1f, 10f)]
    public void Interpolating_blends_position_between_previous_and_current(float alpha, float expectedX)
    {
        var previous = PreviousTransform.From(Transform.At(0f, 0f, 4f));
        var current = Transform.At(10f, -8f, 4f);

        var result = PreviousTransform.Interpolate(current, previous, alpha);

        Assert.Equal(expectedX, result.Position.X, 5);
        Assert.Equal(-8f * alpha, result.Position.Y, 5);
        Assert.Equal(4f, result.Position.Z, 5);
    }

    [Fact]
    public void Interpolating_leaves_scale_alone()
    {
        var previous = PreviousTransform.From(Transform.Identity);
        var current = Transform.Identity;
        current.Scale = new Vector3(2f, 3f, 1f);

        var result = PreviousTransform.Interpolate(current, previous, 0.5f);

        Assert.Equal(new Vector3(2f, 3f, 1f), result.Scale);
    }

    [Fact]
    public void Interpolating_blends_rotation()
    {
        var start = Transform.Identity;
        var end = Transform.Identity;
        end.SetRotationZ(MathF.PI / 2f);

        var result = PreviousTransform.Interpolate(end, PreviousTransform.From(start), 0.5f);

        Assert.Equal(MathF.PI / 4f, result.RotationZ, 4);
    }

    [Fact]
    public void System_records_the_transform_as_the_previous_one()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.Add(entity, Transform.At(5f, 6f, 7f));
        world.Add(entity, new PreviousTransform());

        new PreviousTransformSystem().Update(world, 1.0 / 60.0);

        var previous = world.Get<PreviousTransform>(entity);
        Assert.True(previous.HasHistory);
        Assert.Equal(new Vector3(5f, 6f, 7f), previous.Position);
    }

    [Fact]
    public void System_runs_before_movement_so_previous_lags_current_by_one_step()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.Add(entity, Transform.At(0f, 0f));
        world.Add(entity, new PreviousTransform());
        var recorder = new PreviousTransformSystem();

        recorder.Update(world, 1.0 / 60.0);
        world.Get<Transform>(entity).Position.X = 4f; // this step's movement
        recorder.Update(world, 1.0 / 60.0);
        world.Get<Transform>(entity).Position.X = 8f;

        Assert.Equal(4f, world.Get<PreviousTransform>(entity).Position.X);
        Assert.Equal(8f, world.Get<Transform>(entity).Position.X);
    }

    [Fact]
    public void System_ignores_entities_without_a_previous_transform()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.Add(entity, Transform.At(1f, 1f));

        new PreviousTransformSystem().Update(world, 1.0 / 60.0);

        Assert.False(world.Has<PreviousTransform>(entity));
    }
}
