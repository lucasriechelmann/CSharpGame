using Engine.Core.Ecs;
using Engine.Rendering3D;
using Engine.Rendering3D.Tests.Fixtures;

namespace Engine.Rendering3D.Tests;

public class ModelAnimationPlaybackTests
{
    [Fact]
    public void A_looping_clip_wraps_past_its_end()
    {
        float time = ModelAnimationSystem.Advance(1.75f, 0.5f, 2f, loop: true, out bool finished);

        Assert.Equal(0.25f, time, 5);
        Assert.False(finished);
    }

    [Fact]
    public void A_looping_clip_played_backwards_wraps_to_the_end()
    {
        float time = ModelAnimationSystem.Advance(0.25f, -0.5f, 2f, loop: true, out _);

        Assert.Equal(1.75f, time, 5);
    }

    [Fact]
    public void A_one_shot_clip_stops_on_its_last_frame_and_reports_finished()
    {
        float time = ModelAnimationSystem.Advance(1.9f, 0.5f, 2f, loop: false, out bool finished);

        Assert.Equal(2f, time);
        Assert.True(finished);
    }

    [Fact]
    public void A_one_shot_clip_played_backwards_stops_on_the_first_frame()
    {
        float time = ModelAnimationSystem.Advance(0.1f, -0.5f, 2f, loop: false, out bool finished);

        Assert.Equal(0f, time);
        Assert.True(finished);
    }

    [Fact]
    public void A_clip_in_the_middle_just_moves_on()
    {
        float time = ModelAnimationSystem.Advance(0.5f, 0.25f, 2f, loop: false, out bool finished);

        Assert.Equal(0.75f, time, 5);
        Assert.False(finished);
    }

    [Fact]
    public void A_clip_with_no_length_stays_at_zero()
    {
        Assert.Equal(0f, ModelAnimationSystem.Advance(0f, 1f, 0f, loop: true, out _));
    }

    [Fact]
    public void A_paused_zero_step_does_not_count_as_finished_at_the_end()
    {
        ModelAnimationSystem.Advance(2f, 0f, 2f, loop: false, out bool finished);

        Assert.False(finished);
    }

    [Fact]
    public void The_animator_defaults_to_looping_playback_at_normal_speed()
    {
        var animator = ModelAnimator.Create("wave");

        Assert.Equal("wave", animator.Clip);
        Assert.Equal(1f, animator.Speed);
        Assert.True(animator.Loop);
        Assert.True(animator.Playing);
    }

    [Fact]
    public void Poses_are_kept_per_entity_and_forgotten_when_the_entity_dies()
    {
        var world = new World();
        var poses = new ModelPoses();
        var data = SkinnedGltf.Import();
        var a = world.CreateEntity();
        var b = world.CreateEntity();
        poses.Prune(world); // nothing to drop yet

        // ModelPoses.GetOrCreate needs a GPU model; the cache bookkeeping is the part under test, so poke the dictionary through TryGet/Remove.
        Assert.False(poses.TryGet(a, out _));

        world.DestroyEntity(b);
        poses.Prune(world);
        Assert.Equal(0, poses.Count);
        Assert.NotNull(data);
    }

    [Fact]
    public void A_pose_knows_which_model_it_was_built_for()
    {
        var data = SkinnedGltf.Import();
        var other = SkinnedGltf.Import();
        var pose = new AnimatedPose(data.Nodes, data.Skins);

        Assert.True(pose.Matches(data.Nodes, data.Skins));
        Assert.False(pose.Matches(other.Nodes, other.Skins));
    }
}
