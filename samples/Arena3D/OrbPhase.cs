namespace Arena3D;

/// <summary>Where a run of Orb Run stands.</summary>
public enum OrbPhase
{
    /// <summary>The clock is running and orbs are left.</summary>
    Playing,

    /// <summary>Every orb was collected in time.</summary>
    Won,

    /// <summary>The time ran out first.</summary>
    Lost,
}
