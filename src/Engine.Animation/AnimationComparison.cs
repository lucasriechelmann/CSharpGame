namespace Engine.Animation;

/// <summary>How an <see cref="AnimationCondition"/> compares a parameter with its value.</summary>
public enum AnimationComparison
{
    /// <summary>Parameter equals the value (exact float equality; use for flags stored as 0 or 1).</summary>
    Equal,

    /// <summary>Parameter differs from the value.</summary>
    NotEqual,

    /// <summary>Parameter is greater than the value.</summary>
    Greater,

    /// <summary>Parameter is greater than or equal to the value.</summary>
    GreaterOrEqual,

    /// <summary>Parameter is less than the value.</summary>
    Less,

    /// <summary>Parameter is less than or equal to the value.</summary>
    LessOrEqual,
}
