namespace Engine.Rendering3D;

/// <summary>
/// A model as plain CPU data: the drawable parts, the materials they share and the node tree they came from. glTF is a node tree;
/// the importer keeps it in <see cref="Nodes"/> (for posing and animation) and also flattens it into <see cref="Parts"/>, each with the
/// matrices down the tree already multiplied in, so a static model draws with one matrix multiply per part. Keeping this separate from
/// the GPU <see cref="Model"/> makes importing testable without a window.
/// </summary>
/// <param name="Parts">The drawable pieces.</param>
/// <param name="Materials">The materials the parts index into.</param>
/// <param name="Nodes">The transform tree, parents before children; empty for data built by hand. Parts refer to it through <see cref="ModelPartData.Node"/>.</param>
public sealed record ModelData(IReadOnlyList<ModelPartData> Parts, IReadOnlyList<ModelMaterialData> Materials, IReadOnlyList<ModelNodeData>? Nodes = null)
{
    /// <summary>The transform tree (see the constructor parameter); never null.</summary>
    public IReadOnlyList<ModelNodeData> Nodes { get; init; } = Nodes ?? [];
}
