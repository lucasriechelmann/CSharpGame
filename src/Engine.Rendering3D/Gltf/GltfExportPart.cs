using System.Numerics;
using Engine.Rendering;

namespace Engine.Rendering3D.Gltf;

/// <summary>One piece handed to <see cref="GltfWriter"/>: geometry, a flat colour and where it sits in the model.</summary>
/// <param name="Name">Node name in the file.</param>
/// <param name="Mesh">The geometry.</param>
/// <param name="Color">Base colour of the piece's material (pieces with equal colours share one material).</param>
/// <param name="Local">Piece-to-model matrix (row-vector convention).</param>
public sealed record GltfExportPart(string Name, MeshData Mesh, Color Color, Matrix4x4 Local);
