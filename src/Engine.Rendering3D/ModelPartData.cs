using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>One drawable piece of a model: geometry, which material it uses, and where it sits relative to the model's origin.</summary>
/// <param name="Mesh">The geometry in the piece's own space.</param>
/// <param name="MaterialIndex">Index into <see cref="ModelData.Materials"/>.</param>
/// <param name="Local">Piece-to-model matrix (row-vector convention): the node transforms of the file, already multiplied down the hierarchy. The model's own entity transform applies after it.</param>
public sealed record ModelPartData(MeshData Mesh, int MaterialIndex, Matrix4x4 Local);
