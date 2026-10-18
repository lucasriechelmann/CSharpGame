using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// Where an entity's <see cref="Transform"/> was at the start of the last fixed step. Entities that carry one are drawn
/// between that and their current transform (see <see cref="Interpolate"/>), so motion stays smooth when the display rate
/// is not a multiple of the fixed rate. <see cref="PreviousTransformSystem"/> keeps it up to date.
/// </summary>
/// <remarks>
/// <c>default</c> means "no history yet": a freshly created or loaded entity draws exactly at its <see cref="Transform"/>
/// until the first fixed step has recorded one. That is why teleporting an entity (spawn, respawn, loading a scene) never
/// smears it across the screen. Only add this to things that move; static entities gain nothing.
/// </remarks>
public struct PreviousTransform
{
    /// <summary>World-space position at the start of the last fixed step.</summary>
    public Vector3 Position;

    /// <summary>World-space orientation at the start of the last fixed step.</summary>
    public Quaternion Rotation;

    /// <summary>True once a step has recorded a position; a default (all-zero) rotation is not a valid orientation, so it marks "empty".</summary>
    public readonly bool HasHistory => Rotation != default;

    /// <summary>Captures <paramref name="current"/> as the history.</summary>
    /// <param name="current">The transform to remember.</param>
    public static PreviousTransform From(in Transform current) => new() { Position = current.Position, Rotation = current.Rotation };

    /// <summary>
    /// The transform to draw: <paramref name="alpha"/> of the way from <paramref name="previous"/> to <paramref name="current"/>.
    /// Scale is not interpolated. Without history the current transform is returned unchanged.
    /// </summary>
    /// <param name="current">The simulated transform (end of the last fixed step).</param>
    /// <param name="previous">The history recorded at the start of that step.</param>
    /// <param name="alpha">0..1 position between the last two fixed steps (<see cref="FixedTimestep.Alpha"/>).</param>
    public static Transform Interpolate(in Transform current, in PreviousTransform previous, float alpha)
    {
        if (!previous.HasHistory) return current;

        var result = current;
        result.Position = Vector3.Lerp(previous.Position, current.Position, alpha);
        result.Rotation = Quaternion.Slerp(previous.Rotation, current.Rotation, alpha);
        return result;
    }
}
