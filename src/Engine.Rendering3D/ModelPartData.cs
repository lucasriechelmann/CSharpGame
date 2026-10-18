using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>One drawable piece of a model: geometry, which material it uses, and where it sits relative to the model's origin.</summary>
/// <param name="Mesh">The geometry in the piece's own space.</param>
/// <param name="MaterialIndex">Index into <see cref="ModelData.Materials"/>.</param>
/// <param name="Local">Piece-to-model matrix (row-vector convention): the node transforms of the file, already multiplied down the hierarchy. The model's own entity transform applies after it.</param>
/// <param name="Node">Index into <see cref="ModelData.Nodes"/> of the node that carries the piece (its model-space matrix is <paramref name="Local"/> at rest), or -1 for a part built by hand.</param>
/// <param name="Skin">Index into <see cref="ModelData.Skins"/> when the piece is skinned (its vertices are deformed by the skin's joints, and are already in model space), or -1 for a rigid piece.</param>
public sealed record ModelPartData(MeshData Mesh, int MaterialIndex, Matrix4x4 Local, int Node = -1, int Skin = -1);
