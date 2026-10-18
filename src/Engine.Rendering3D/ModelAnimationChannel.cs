namespace Engine.Rendering3D;

/// <summary>Keyframes for one property of one node: when, and what value.</summary>
/// <param name="Node">Index into <see cref="ModelData.Nodes"/> of the node that is moved.</param>
/// <param name="Path">Which part of the node's transform the values replace.</param>
/// <param name="Interpolation">How values between keys are made.</param>
/// <param name="Times">Key times in seconds, ascending.</param>
/// <param name="Values">The values: 3 floats per key for translation and scale, 4 for rotation (x, y, z, w); times 3 for cubic splines (in-tangent, value, out-tangent).</param>
public sealed record ModelAnimationChannel(int Node, AnimationPath Path, AnimationInterpolation Interpolation, float[] Times, float[] Values);
