using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// The pose of one model instance at one moment: every node's matrix in model space, and for each skin the joint matrices a skinned mesh is
/// deformed by. Built once per animated entity and re-evaluated as its clip plays, reusing the same arrays, so animating allocates nothing
/// after construction. Pure CPU maths (no GL), so it is unit-tested.
/// </summary>
public sealed class AnimatedPose
{
    private readonly IReadOnlyList<ModelNodeData> _nodes;
    private readonly IReadOnlyList<ModelSkinData> _skins;
    private readonly Vector3[] _restTranslation;
    private readonly Quaternion[] _restRotation;
    private readonly Vector3[] _restScale;
    private readonly Vector3[] _translation;
    private readonly Quaternion[] _rotation;
    private readonly Vector3[] _scale;
    private readonly Matrix4x4[] _local;

    /// <summary>Creates a pose for a model's node tree, starting at rest.</summary>
    /// <param name="nodes">The model's nodes (parents before children).</param>
    /// <param name="skins">The model's skins.</param>
    /// <exception cref="InvalidOperationException">A node's rest matrix has no translation-rotation-scale form (a zero scale).</exception>
    public AnimatedPose(IReadOnlyList<ModelNodeData> nodes, IReadOnlyList<ModelSkinData> skins)
    {
        _nodes = nodes;
        _skins = skins;
        int count = nodes.Count;
        _restTranslation = new Vector3[count];
        _restRotation = new Quaternion[count];
        _restScale = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            if (!Matrix4x4.Decompose(nodes[i].Local, out _restScale[i], out _restRotation[i], out _restTranslation[i]))
                throw new InvalidOperationException($"Model node {i} (\"{nodes[i].Name}\") cannot be animated: its matrix has no translation, rotation and scale (a zero scale?).");
        }

        _translation = new Vector3[count];
        _rotation = new Quaternion[count];
        _scale = new Vector3[count];
        _local = new Matrix4x4[count];
        NodeWorld = new Matrix4x4[count];
        JointMatrices = new Matrix4x4[skins.Count][];
        for (int s = 0; s < skins.Count; s++) JointMatrices[s] = new Matrix4x4[skins[s].Joints.Count];
        Evaluate(null, 0f);
    }

    /// <summary>True when this pose was built for exactly these nodes and skins, i.e. the entity still shows the same model.</summary>
    /// <param name="nodes">The model's nodes.</param>
    /// <param name="skins">The model's skins.</param>
    public bool Matches(IReadOnlyList<ModelNodeData> nodes, IReadOnlyList<ModelSkinData> skins) => ReferenceEquals(_nodes, nodes) && ReferenceEquals(_skins, skins);

    /// <summary>Each node's matrix in model space (node space to model space), one per node.</summary>
    public Matrix4x4[] NodeWorld { get; }

    /// <summary>For each skin, one matrix per joint that carries a bind-pose vertex to where that joint has moved it (model space to model space).</summary>
    public Matrix4x4[][] JointMatrices { get; }

    /// <summary>
    /// Poses the model at <paramref name="time"/> of <paramref name="clip"/>: every node starts at its rest transform and each channel replaces
    /// its translation, rotation or scale; then the world matrices and joint matrices are rebuilt.
    /// </summary>
    /// <param name="clip">The clip to sample, or null for the rest pose.</param>
    /// <param name="time">Seconds into the clip (the caller wraps looping).</param>
    public void Evaluate(ModelAnimationData? clip, float time)
    {
        Array.Copy(_restTranslation, _translation, _translation.Length);
        Array.Copy(_restRotation, _rotation, _rotation.Length);
        Array.Copy(_restScale, _scale, _scale.Length);

        if (clip != null)
        {
            foreach (var channel in clip.Channels)
            {
                if (channel.Node < 0 || channel.Node >= _nodes.Count)
                    throw new InvalidOperationException($"Animation \"{clip.Name}\" drives node {channel.Node}, but the model has {_nodes.Count} nodes.");

                switch (channel.Path)
                {
                    case AnimationPath.Translation: _translation[channel.Node] = AnimationSampling.SampleVector3(channel, time); break;
                    case AnimationPath.Rotation: _rotation[channel.Node] = AnimationSampling.SampleQuaternion(channel, time); break;
                    default: _scale[channel.Node] = AnimationSampling.SampleVector3(channel, time); break;
                }
            }
        }

        for (int i = 0; i < _local.Length; i++)
        {
            _local[i] = Matrix4x4.CreateScale(_scale[i]) * Matrix4x4.CreateFromQuaternion(_rotation[i]) * Matrix4x4.CreateTranslation(_translation[i]);
        }

        ModelPose.ComputeWorld(_nodes, _local, NodeWorld);

        for (int s = 0; s < _skins.Count; s++)
        {
            var skin = _skins[s];
            for (int j = 0; j < skin.Joints.Count; j++)
                JointMatrices[s][j] = skin.InverseBind[j] * NodeWorld[skin.Joints[j]]; // into the joint's space as bound, then out to where the joint is now
        }
    }
}
