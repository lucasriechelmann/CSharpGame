namespace Engine.Core.Ecs;

/// <summary>A unit of behaviour that runs over the world's components. Systems hold logic; components hold data.</summary>
public interface ISystem
{
    /// <summary>Runs the system once.</summary>
    /// <param name="world">The world whose components the system reads and writes.</param>
    /// <param name="dt">Seconds elapsed: the fixed step for fixed systems, the frame time for frame systems.</param>
    void Update(World world, double dt);
}
