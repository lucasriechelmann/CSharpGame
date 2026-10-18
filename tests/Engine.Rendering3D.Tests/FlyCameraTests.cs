using System.Numerics;
using Engine.Rendering3D;

namespace Engine.Rendering3D.Tests;

public class FlyCameraTests
{
    private static Vector3 ForwardOf(FlyCamera fly) => Vector3.Transform(-Vector3.UnitZ, fly.Rotation);

    [Fact]
    public void Zero_angles_look_down_negative_z()
    {
        var forward = ForwardOf(new FlyCamera());

        Assert.Equal(-1f, forward.Z, 5);
    }

    [Fact]
    public void Positive_yaw_turns_left()
    {
        var forward = ForwardOf(new FlyCamera { Yaw = MathF.PI / 2f });

        Assert.Equal(-1f, forward.X, 5);
        Assert.Equal(0f, forward.Z, 5);
    }

    [Fact]
    public void Positive_pitch_looks_up()
    {
        Assert.True(ForwardOf(new FlyCamera { Pitch = 0.5f }).Y > 0f);
    }

    [Fact]
    public void Pitch_is_clamped_short_of_straight_up_and_down()
    {
        var fly = new FlyCamera();

        fly.Look(0f, 10f);
        Assert.Equal(FlyCamera.MaxPitch, fly.Pitch);

        fly.Look(0f, -100f);
        Assert.Equal(-FlyCamera.MaxPitch, fly.Pitch);
    }

    [Fact]
    public void The_view_never_rolls()
    {
        var fly = new FlyCamera();
        fly.Look(1.3f, 0.4f);

        var right = Vector3.Transform(Vector3.UnitX, fly.Rotation);

        Assert.Equal(0f, right.Y, 5); // the right-hand axis stays horizontal
    }

    [Fact]
    public void Move_forward_follows_where_the_camera_looks()
    {
        var fly = new FlyCamera { Position = Vector3.Zero, Yaw = MathF.PI / 2f };

        fly.Move(forward: 1f, right: 0f, up: 0f, distance: 2f);

        Assert.Equal(-2f, fly.Position.X, 4);
        Assert.Equal(0f, fly.Position.Z, 4);
    }

    [Fact]
    public void Move_up_is_along_world_y_even_when_looking_down()
    {
        var fly = new FlyCamera { Pitch = -1f };

        fly.Move(forward: 0f, right: 0f, up: 1f, distance: 3f);

        Assert.Equal(new Vector3(0f, 3f, 0f), fly.Position);
    }

    [Fact]
    public void Diagonal_movement_is_not_faster_than_straight()
    {
        var fly = new FlyCamera();

        fly.Move(forward: 1f, right: 1f, up: 0f, distance: 5f);

        Assert.Equal(5f, fly.Position.Length(), 4);
    }

    [Fact]
    public void From_a_camera_reproduces_its_view_and_ApplyTo_writes_it_back()
    {
        var source = new Camera3D { Position = new Vector3(4f, 2f, 6f) };
        source.LookAt(new Vector3(1f, 1f, 0f));

        var fly = FlyCamera.From(source);
        var copy = new Camera3D();
        fly.ApplyTo(copy);

        Assert.Equal(source.Position, copy.Position);
        Assert.True((source.Forward - copy.Forward).Length() < 1e-4f);
        Assert.True((source.Up - copy.Up).Length() < 1e-4f);
    }
}
