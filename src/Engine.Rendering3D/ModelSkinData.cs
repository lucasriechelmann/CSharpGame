using System.Numerics;

namespace Engine.Rendering3D;

/// <summary>
/// A skin: the joints (nodes) a deformable mesh follows, and for each the matrix that undoes where the joint sat when the mesh was modelled
/// (the inverse bind matrix). A vertex is moved by a blend of <c>joint pose * inverse bind</c> over the joints that influence it.
/// </summary>
/// <param name="Joints">Indices into <see cref="ModelData.Nodes"/>, in the order the vertex joint numbers refer to.</param>
/// <param name="InverseBind">One matrix per joint (row-vector convention).</param>
public sealed record ModelSkinData(IReadOnlyList<int> Joints, IReadOnlyList<Matrix4x4> InverseBind);
