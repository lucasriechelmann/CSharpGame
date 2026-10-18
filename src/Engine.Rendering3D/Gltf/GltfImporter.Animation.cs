using System.Numerics;

namespace Engine.Rendering3D.Gltf;

// Skins and animations: per-vertex joint data, the skin joint lists with their inverse bind matrices, and the keyframe channels.
public static partial class GltfImporter
{
    /// <summary>Reads JOINTS_0 and WEIGHTS_0 when both are present; null for a rigid primitive. Weights are normalised so they add up to one.</summary>
    private static SkinVertex[]? ReadSkinVertices(GltfFile file, byte[][] buffers, GltfFile.PrimitiveInfo primitive, int meshIndex, int vertexCount)
    {
        if (!primitive.Attributes.TryGetValue("JOINTS_0", out int jointsAccessor) || !primitive.Attributes.TryGetValue("WEIGHTS_0", out int weightsAccessor))
            return null;

        var joints = GltfAccessors.ReadFloats(file, buffers, jointsAccessor, 4);
        var weights = GltfAccessors.ReadFloats(file, buffers, weightsAccessor, 4);
        if (joints.Length / 4 != vertexCount) throw new InvalidOperationException($"glTF mesh {meshIndex}: JOINTS_0 has {joints.Length / 4} entries but POSITION has {vertexCount}.");
        if (weights.Length / 4 != vertexCount) throw new InvalidOperationException($"glTF mesh {meshIndex}: WEIGHTS_0 has {weights.Length / 4} entries but POSITION has {vertexCount}.");

        var skin = new SkinVertex[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            var weight = new Vector4(weights[i * 4], weights[i * 4 + 1], weights[i * 4 + 2], weights[i * 4 + 3]);
            float sum = weight.X + weight.Y + weight.Z + weight.W;
            weight = sum > 1e-6f ? weight / sum : new Vector4(1f, 0f, 0f, 0f); // a vertex no joint claims follows the first one
            skin[i] = new SkinVertex(new Vector4(joints[i * 4], joints[i * 4 + 1], joints[i * 4 + 2], joints[i * 4 + 3]), weight);
        }

        return skin;
    }

    private static List<ModelSkinData> ImportSkins(GltfFile file, byte[][] buffers, Dictionary<int, int> nodeMap)
    {
        var skins = new List<ModelSkinData>(file.Skins.Count);
        for (int s = 0; s < file.Skins.Count; s++)
        {
            var info = file.Skins[s];
            var joints = new int[info.Joints.Count];
            for (int j = 0; j < joints.Length; j++)
            {
                if (!nodeMap.TryGetValue(info.Joints[j], out joints[j]))
                    throw new InvalidOperationException($"glTF skin {s}: joint {j} is node {info.Joints[j]}, which is not part of the scene.");
            }

            if (joints.Length > MeshShaderSource.MaxJoints)
                throw new InvalidOperationException($"glTF skin {s} has {joints.Length} joints; this renderer supports at most {MeshShaderSource.MaxJoints}.");

            var inverseBind = new Matrix4x4[joints.Length];
            if (info.InverseBindMatrices is int accessor)
            {
                var values = GltfAccessors.ReadFloats(file, buffers, accessor, 16);
                if (values.Length / 16 < joints.Length)
                    throw new InvalidOperationException($"glTF skin {s}: {joints.Length} joints but only {values.Length / 16} inverse bind matrices.");

                for (int j = 0; j < joints.Length; j++)
                {
                    // Column-major for column vectors, read straight into the row-major fields: the row-vector matrix we use.
                    int o = j * 16;
                    inverseBind[j] = new Matrix4x4(
                        values[o], values[o + 1], values[o + 2], values[o + 3],
                        values[o + 4], values[o + 5], values[o + 6], values[o + 7],
                        values[o + 8], values[o + 9], values[o + 10], values[o + 11],
                        values[o + 12], values[o + 13], values[o + 14], values[o + 15]);
                }
            }
            else
            {
                Array.Fill(inverseBind, Matrix4x4.Identity); // the spec: absent means identity
            }

            skins.Add(new ModelSkinData(joints, inverseBind));
        }

        return skins;
    }

    private static List<ModelAnimationData> ImportAnimations(GltfFile file, byte[][] buffers, Dictionary<int, int> nodeMap)
    {
        var clips = new List<ModelAnimationData>(file.Animations.Count);
        for (int a = 0; a < file.Animations.Count; a++)
        {
            var info = file.Animations[a];
            var channels = new List<ModelAnimationChannel>(info.Channels.Count);
            float duration = 0f;
            foreach (var channel in info.Channels)
            {
                // Morph-target weights and extension targets are not supported; a channel on a node outside the scene moves nothing visible.
                if (channel.Target?.Node is not int node || !nodeMap.TryGetValue(node, out int modelNode)) continue;
                var path = channel.Target.Path switch
                {
                    "translation" => AnimationPath.Translation,
                    "rotation" => AnimationPath.Rotation,
                    "scale" => AnimationPath.Scale,
                    _ => (AnimationPath?)null,
                };
                if (path is not { } animated) continue;

                if (channel.Sampler < 0 || channel.Sampler >= info.Samplers.Count)
                    throw new InvalidOperationException($"glTF animation {a}: a channel uses sampler {channel.Sampler}, which does not exist (the animation has {info.Samplers.Count}).");
                var sampler = info.Samplers[channel.Sampler];

                var interpolation = sampler.Interpolation switch
                {
                    null or "LINEAR" => AnimationInterpolation.Linear,
                    "STEP" => AnimationInterpolation.Step,
                    "CUBICSPLINE" => AnimationInterpolation.CubicSpline,
                    _ => throw new InvalidOperationException($"glTF animation {a}: interpolation '{sampler.Interpolation}' is not LINEAR, STEP or CUBICSPLINE."),
                };

                int components = animated == AnimationPath.Rotation ? 4 : 3;
                var times = GltfAccessors.ReadFloats(file, buffers, sampler.Input, 1);
                var values = GltfAccessors.ReadFloats(file, buffers, sampler.Output, components);
                int perKey = interpolation == AnimationInterpolation.CubicSpline ? 3 : 1;
                if (times.Length == 0) throw new InvalidOperationException($"glTF animation {a}: a channel has no keyframes.");
                if (values.Length != times.Length * components * perKey)
                    throw new InvalidOperationException($"glTF animation {a}: {times.Length} keyframes need {times.Length * components * perKey} values, the output accessor has {values.Length}.");
                for (int k = 1; k < times.Length; k++)
                {
                    if (times[k] < times[k - 1]) throw new InvalidOperationException($"glTF animation {a}: keyframe times must not go backwards (key {k}).");
                }

                channels.Add(new ModelAnimationChannel(modelNode, animated, interpolation, times, values));
                duration = MathF.Max(duration, times[^1]);
            }

            clips.Add(new ModelAnimationData(string.IsNullOrEmpty(info.Name) ? $"Animation {a}" : info.Name, duration, channels));
        }

        return clips;
    }
}
