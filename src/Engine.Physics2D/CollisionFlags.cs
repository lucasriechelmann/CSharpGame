namespace Engine.Physics2D;

/// <summary>Which sides of a body touched something solid during the last physics step.</summary>
[Flags]
public enum CollisionFlags
{
    /// <summary>No contact.</summary>
    None = 0,

    /// <summary>Standing on something (the body was moving down and was stopped).</summary>
    Below = 1,

    /// <summary>Head against something (the body was moving up and was stopped).</summary>
    Above = 2,

    /// <summary>Stopped while moving left.</summary>
    Left = 4,

    /// <summary>Stopped while moving right.</summary>
    Right = 8,
}
