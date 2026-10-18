using System.Numerics;
using Engine.Rendering3D;
using Engine.Rendering3D.Tests.Fixtures;

namespace Engine.Rendering3D.Tests;

public class GltfAnimationTests
{
    private static readonly ModelData Arm = SkinnedGltf.Import();

    private static void AssertNear(Vector3 expected, Vector3 actual, int digits = 4)
    {
        Assert.Equal(expected.X, actual.X, digits);
        Assert.Equal(expected.Y, actual.Y, digits);
        Assert.Equal(expected.Z, actual.Z, digits);
    }

    // ---- importing ----

    [Fact]
    public void Animations_skins_and_the_skinned_part_are_imported()
    {
        Assert.Equal(3, Arm.Animations.Count);
        var skin = Assert.Single(Arm.Skins);
        Assert.Equal(2, skin.Joints.Count);
        Assert.Equal(2, skin.InverseBind.Count);
        var part = Assert.Single(Arm.Parts);
        Assert.Equal(0, part.Skin);
        Assert.NotNull(part.Mesh.Skin);
    }

    [Fact]
    public void Clip_names_and_durations_come_from_the_file_with_a_fallback_name()
    {
        Assert.Equal("lift", Arm.Animations[0].Name);
        Assert.Equal("hop", Arm.Animations[1].Name);
        Assert.Equal("Animation 2", Arm.Animations[2].Name);
        Assert.Equal(1f, Arm.Animations[0].Duration);
    }

    [Fact]
    public void Channels_target_the_models_own_node_indices_and_paths()
    {
        var lift = Arm.Animations[0];

        Assert.Equal(2, lift.Channels.Count);
        Assert.Equal(AnimationPath.Translation, lift.Channels[0].Path);
        Assert.Equal("tip", Arm.Nodes[lift.Channels[0].Node].Name);
        Assert.Equal(AnimationPath.Rotation, lift.Channels[1].Path);
        Assert.Equal("root", Arm.Nodes[lift.Channels[1].Node].Name);
    }

    [Fact]
    public void Interpolation_modes_are_read()
    {
        Assert.Equal(AnimationInterpolation.Linear, Arm.Animations[0].Channels[0].Interpolation);
        Assert.Equal(AnimationInterpolation.Step, Arm.Animations[1].Channels[0].Interpolation);
        Assert.Equal(AnimationInterpolation.CubicSpline, Arm.Animations[2].Channels[0].Interpolation);
    }

    [Fact]
    public void Skin_joints_are_model_node_indices_and_inverse_binds_are_row_vector_matrices()
    {
        var skin = Arm.Skins[0];

        Assert.Equal(["root", "tip"], skin.Joints.Select(j => Arm.Nodes[j].Name));
        Assert.Equal(Matrix4x4.Identity, skin.InverseBind[0]);
        AssertNear(new Vector3(0, -1, 0), skin.InverseBind[1].Translation);
    }

    [Fact]
    public void Vertex_joints_and_weights_are_read_and_normalised()
    {
        var skin = Arm.Parts[0].Mesh.Skin!;

        Assert.Equal(4, skin.Length);
        Assert.Equal(new Vector4(0, 0, 0, 0), skin[0].Joints);
        Assert.Equal(new Vector4(1, 0, 0, 0), skin[2].Joints);
        Assert.Equal(new Vector4(0.5f, 0.5f, 0, 0), skin[2].Weights);
        Assert.All(skin, v => Assert.Equal(1f, v.Weights.X + v.Weights.Y + v.Weights.Z + v.Weights.W, 4));
    }

    [Fact]
    public void A_model_without_animations_or_skins_has_empty_lists()
    {
        var model = GltfBytes.Import(GltfBytes.TriangleJson());

        Assert.Empty(model.Animations);
        Assert.Empty(model.Skins);
        Assert.Null(model.Parts[0].Mesh.Skin);
        Assert.Equal(-1, model.Parts[0].Skin);
    }

    [Fact]
    public void A_skin_that_names_a_missing_node_is_rejected()
    {
        string json = SkinnedGltf.Json().Replace("\"joints\": [0, 1]", "\"joints\": [0, 7]");

        var ex = Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));

        Assert.Contains("joint 1", ex.Message);
    }

    [Fact]
    public void A_channel_that_names_a_missing_sampler_is_rejected()
    {
        string json = SkinnedGltf.Json().Replace("{ \"sampler\": 1, \"target\": { \"node\": 0, \"path\": \"rotation\" } }", "{ \"sampler\": 9, \"target\": { \"node\": 0, \"path\": \"rotation\" } }");

        Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));
    }

    [Fact]
    public void An_unknown_interpolation_is_rejected()
    {
        string json = SkinnedGltf.Json().Replace("\"STEP\"", "\"WOBBLY\"");

        var ex = Assert.Throws<InvalidOperationException>(() => GltfBytes.Import(json));

        Assert.Contains("WOBBLY", ex.Message);
    }

    [Fact]
    public void Channels_on_weights_are_ignored_rather_than_failing()
    {
        string json = SkinnedGltf.Json().Replace("\"path\": \"scale\"", "\"path\": \"weights\"")
            .Replace("{ \"sampler\": 0, \"target\": { \"node\": 1, \"path\": \"translation\" } },", "{ \"sampler\": 0, \"target\": { \"node\": 1, \"path\": \"weights\" } },");

        var model = GltfBytes.Import(json);

        Assert.Single(model.Animations[0].Channels); // only the rotation channel is left
    }

    // ---- sampling ----

    [Fact]
    public void Key_lookup_finds_the_surrounding_keys_and_holds_at_the_ends()
    {
        float[] times = [0f, 1f, 3f];

        AnimationSampling.FindKeys(times, 2f, out int a, out int b, out float t);
        Assert.Equal((1, 2), (a, b));
        Assert.Equal(0.5f, t, 4);

        AnimationSampling.FindKeys(times, -5f, out a, out b, out t);
        Assert.Equal((0, 0), (a, b));

        AnimationSampling.FindKeys(times, 99f, out a, out b, out t);
        Assert.Equal((2, 2), (a, b));
        Assert.Equal(0f, t);
    }

    [Fact]
    public void A_single_key_channel_holds_its_value()
    {
        var channel = new ModelAnimationChannel(0, AnimationPath.Translation, AnimationInterpolation.Linear, [0f], [1f, 2f, 3f]);

        AssertNear(new Vector3(1, 2, 3), AnimationSampling.SampleVector3(channel, 5f));
    }

    [Fact]
    public void Linear_translation_blends_between_the_keys()
    {
        var channel = Arm.Animations[0].Channels[0]; // y from 1 to 2

        AssertNear(new Vector3(0, 1.5f, 0), AnimationSampling.SampleVector3(channel, 0.5f));
    }

    [Fact]
    public void Step_translation_holds_the_earlier_key_until_the_next_one()
    {
        var channel = Arm.Animations[1].Channels[0];

        AssertNear(new Vector3(0, 1, 0), AnimationSampling.SampleVector3(channel, 0.99f));
        AssertNear(new Vector3(5, 5, 5), AnimationSampling.SampleVector3(channel, 1f));
    }

    [Fact]
    public void A_spline_with_zero_tangents_eases_between_the_values()
    {
        var channel = Arm.Animations[2].Channels[0];

        Assert.Equal(1.5f, AnimationSampling.SampleVector3(channel, 0.5f).Y, 4); // the midpoint of an ease is the midpoint of the values
        Assert.True(AnimationSampling.SampleVector3(channel, 0.1f).Y < 1.1f);    // but it starts slower than a straight line
        Assert.Equal(1f, AnimationSampling.SampleVector3(channel, 0f).Y, 4);
        Assert.Equal(2f, AnimationSampling.SampleVector3(channel, 1f).Y, 4);
    }

    [Fact]
    public void Rotation_halfway_between_identity_and_a_quarter_turn_is_an_eighth_turn()
    {
        var channel = Arm.Animations[0].Channels[1];

        var half = AnimationSampling.SampleQuaternion(channel, 0.5f);

        var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4f);
        Assert.Equal(1f, MathF.Abs(Quaternion.Dot(half, expected)), 4);
        Assert.Equal(1f, half.Length(), 4);
    }

    // ---- the pose ----

    [Fact]
    public void The_rest_pose_matches_the_flattened_node_matrices()
    {
        var pose = new AnimatedPose(Arm.Nodes, Arm.Skins);
        var rest = new Matrix4x4[Arm.Nodes.Count];
        ModelPose.ComputeRestWorld(Arm.Nodes, rest);

        for (int i = 0; i < rest.Length; i++) AssertNear(rest[i].Translation, pose.NodeWorld[i].Translation);
    }

    [Fact]
    public void In_the_bind_pose_every_joint_matrix_is_the_identity()
    {
        var pose = new AnimatedPose(Arm.Nodes, Arm.Skins);

        foreach (var joint in pose.JointMatrices[0])
        {
            AssertNear(Vector3.Zero, joint.Translation);
            Assert.Equal(1f, joint.M11, 4);
            Assert.Equal(1f, joint.M22, 4);
        }
    }

    [Fact]
    public void Lifting_the_tip_joint_moves_the_vertices_it_owns_but_not_the_roots()
    {
        var pose = new AnimatedPose(Arm.Nodes, Arm.Skins);
        var lift = Arm.Animations[0];
        pose.Evaluate(lift, 1f); // tip at y = 2, root turned a quarter about Z

        // Skin joint 1 is the tip: it moved a unit up (its own bind was one unit up) and was swung round by the root.
        var tipMatrix = pose.JointMatrices[0][1];
        var tipVertex = Vector3.Transform(new Vector3(0, 1, 0), tipMatrix);   // a vertex bound at the tip
        var rootVertex = Vector3.Transform(new Vector3(1, 0, 0), pose.JointMatrices[0][0]);

        // The root's quarter turn about Z takes +Y to -X and +X to +Y; the tip then sits 2 up its (turned) axis.
        AssertNear(new Vector3(-2, 0, 0), tipVertex);
        AssertNear(new Vector3(0, 1, 0), rootVertex);
    }

    [Fact]
    public void Evaluating_again_does_not_accumulate_the_previous_pose()
    {
        var pose = new AnimatedPose(Arm.Nodes, Arm.Skins);
        var lift = Arm.Animations[0];

        pose.Evaluate(lift, 1f);
        pose.Evaluate(lift, 0f);

        AssertNear(Vector3.Zero, pose.JointMatrices[0][1].Translation);
    }

    [Fact]
    public void Evaluating_with_no_clip_returns_to_rest()
    {
        var pose = new AnimatedPose(Arm.Nodes, Arm.Skins);
        pose.Evaluate(Arm.Animations[0], 1f);

        pose.Evaluate(null, 0f);

        AssertNear(Vector3.Zero, pose.JointMatrices[0][1].Translation);
    }

    [Fact]
    public void A_clip_that_drives_a_node_the_model_lacks_is_reported()
    {
        var pose = new AnimatedPose(Arm.Nodes, Arm.Skins);
        var bad = new ModelAnimationData("bad", 1f, [new ModelAnimationChannel(99, AnimationPath.Translation, AnimationInterpolation.Linear, [0f], [0f, 0f, 0f])]);

        Assert.Throws<InvalidOperationException>(() => pose.Evaluate(bad, 0f));
    }

    [Fact]
    public void A_mesh_with_the_wrong_amount_of_skin_data_is_rejected()
    {
        var vertices = new[] { new Vertex3D(Vector3.Zero, Vector3.UnitY, Vector2.Zero), new Vertex3D(Vector3.UnitX, Vector3.UnitY, Vector2.Zero), new Vertex3D(Vector3.UnitZ, Vector3.UnitY, Vector2.Zero) };

        Assert.Throws<ArgumentException>(() => new MeshData(vertices, [0, 1, 2], [new SkinVertex(Vector4.Zero, Vector4.UnitX)]));
    }
}
