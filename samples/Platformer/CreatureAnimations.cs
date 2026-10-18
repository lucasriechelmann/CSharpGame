using Engine.Animation;

namespace Platformer;

/// <summary>Builds the walk animation for each creature type from <c>Assets/creatures.png</c> (8 types, 2 frames each, in one row).</summary>
public static class CreatureAnimations
{
    /// <summary>Number of creature types in the sheet.</summary>
    public const int TypeCount = 8;

    private const int Cell = 16;
    private const int SheetColumns = TypeCount * 2;

    /// <summary>A single-state machine that loops the two walk frames of creature <paramref name="type"/>.</summary>
    public static AnimationStateMachine Create(int type)
    {
        var machine = new AnimationStateMachine();
        machine.AddState("Walk", AnimationClip.FromGrid($"Walk{type}", Cell, Cell, SheetColumns, [type * 2, type * 2 + 1], 6f));
        return machine;
    }
}
