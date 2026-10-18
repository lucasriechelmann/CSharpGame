using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>One drawable piece of a loaded <see cref="Model"/>: GPU geometry, its material and the piece-to-model matrix.</summary>
/// <param name="Mesh">The uploaded geometry (owned by the model).</param>
/// <param name="Material">The surface look (owned by the model).</param>
/// <param name="Local">Piece-to-model matrix (row-vector convention); the entity's transform applies after it.</param>
/// <param name="Node">Index into <see cref="Model.Nodes"/> of the node that carries the piece, or -1 for a part built by hand.</param>
/// <param name="Skin">Index into <see cref="Model.Skins"/> when the piece is skinned (deformed by a skeleton; its vertices are already in model space), or -1.</param>
public sealed record ModelPart(Mesh Mesh, Material Material, Matrix4x4 Local, int Node = -1, int Skin = -1);
