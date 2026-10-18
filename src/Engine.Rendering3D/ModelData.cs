namespace Engine.Rendering3D;

/// <summary>
/// A model as plain CPU data: the drawable parts, the materials they share, the node tree they came from, and the skins and animations that
/// pose it. glTF is a node tree; the importer keeps it in <see cref="Nodes"/> (for posing and animation) and also flattens it into
/// <see cref="Parts"/>, each with the matrices down the tree already multiplied in, so a static model draws with one matrix multiply per part.
/// Keeping this separate from the GPU <see cref="Model"/> makes importing testable without a window.
/// </summary>
/// <param name="Parts">The drawable pieces.</param>
/// <param name="Materials">The materials the parts index into.</param>
/// <param name="Nodes">The transform tree, parents before children; empty for data built by hand. Parts refer to it through <see cref="ModelPartData.Node"/>.</param>
/// <param name="Skins">The skins skinned parts refer to through <see cref="ModelPartData.Skin"/>; empty for a rigid model.</param>
/// <param name="Animations">The animation clips; empty for a static model.</param>
public sealed record ModelData(
    IReadOnlyList<ModelPartData> Parts,
    IReadOnlyList<ModelMaterialData> Materials,
    IReadOnlyList<ModelNodeData>? Nodes = null,
    IReadOnlyList<ModelSkinData>? Skins = null,
    IReadOnlyList<ModelAnimationData>? Animations = null)
{
    /// <summary>The transform tree (see the constructor parameter); never null.</summary>
    public IReadOnlyList<ModelNodeData> Nodes { get; init; } = Nodes ?? [];

    /// <summary>The skins (see the constructor parameter); never null.</summary>
    public IReadOnlyList<ModelSkinData> Skins { get; init; } = Skins ?? [];

    /// <summary>The animation clips (see the constructor parameter); never null.</summary>
    public IReadOnlyList<ModelAnimationData> Animations { get; init; } = Animations ?? [];
}
