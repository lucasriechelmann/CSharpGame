using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// One node of a model's transform tree as the file described it. Parts are still stored flattened (each with its matrix already
/// multiplied down the tree, so drawing stays one matrix multiply), but the tree is kept so something can pose the nodes later:
/// animation changes a node's <see cref="Local"/> and recomputes the world matrices through <see cref="ModelPose"/>.
/// </summary>
/// <param name="Name">The node's name in the file, or an empty string when it has none.</param>
/// <param name="Parent">Index of the parent node in <see cref="ModelData.Nodes"/>, or -1 for a root. A parent always comes before its children.</param>
/// <param name="Local">Node-to-parent matrix (row-vector convention).</param>
public sealed record ModelNodeData(string Name, int Parent, Matrix4x4 Local);
