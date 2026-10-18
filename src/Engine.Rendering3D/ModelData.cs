namespace Engine.Rendering3D;

/// <summary>
/// A model as plain CPU data: a flat list of parts and the materials they share. glTF is a node tree, but the engine has no
/// transform parents yet, so the importer flattens it: each mesh primitive becomes a part with the matrices down the tree
/// already multiplied in. Keeping this separate from the GPU <see cref="Model"/> makes importing testable without a window.
/// </summary>
/// <param name="Parts">The drawable pieces.</param>
/// <param name="Materials">The materials the parts index into.</param>
public sealed record ModelData(IReadOnlyList<ModelPartData> Parts, IReadOnlyList<ModelMaterialData> Materials);
