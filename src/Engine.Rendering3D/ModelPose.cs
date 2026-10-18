using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>Pure maths over a model's node tree: turns local matrices into model-space ones.</summary>
public static class ModelPose
{
    /// <summary>
    /// Computes every node's node-to-model matrix: its local matrix times its parent's. Relies on parents coming before their children
    /// (the importer writes them in that order), so one pass is enough and nothing allocates.
    /// </summary>
    /// <param name="nodes">The node tree.</param>
    /// <param name="locals">Each node's local matrix, one per node: the rest pose is <c>nodes[i].Local</c>, an animation supplies its own.</param>
    /// <param name="world">Receives the model-space matrices, one per node.</param>
    /// <exception cref="ArgumentException">The spans do not have one entry per node, or a node's parent does not come before it.</exception>
    public static void ComputeWorld(IReadOnlyList<ModelNodeData> nodes, ReadOnlySpan<Matrix4x4> locals, Span<Matrix4x4> world)
    {
        if (locals.Length != nodes.Count) throw new ArgumentException($"Expected {nodes.Count} local matrices, got {locals.Length}.", nameof(locals));
        if (world.Length != nodes.Count) throw new ArgumentException($"Expected room for {nodes.Count} world matrices, got {world.Length}.", nameof(world));

        for (int i = 0; i < nodes.Count; i++)
        {
            int parent = nodes[i].Parent;
            if (parent >= i) throw new ArgumentException($"Node {i} names parent {parent}, which does not come before it.", nameof(nodes));
            world[i] = parent < 0 ? locals[i] : locals[i] * world[parent];
        }
    }

    /// <summary>The rest pose: <see cref="ComputeWorld"/> with every node's own matrix.</summary>
    /// <param name="nodes">The node tree.</param>
    /// <param name="world">Receives the model-space matrices, one per node.</param>
    public static void ComputeRestWorld(IReadOnlyList<ModelNodeData> nodes, Span<Matrix4x4> world)
    {
        var locals = new Matrix4x4[nodes.Count];
        for (int i = 0; i < locals.Length; i++) locals[i] = nodes[i].Local;
        ComputeWorld(nodes, locals, world);
    }
}
