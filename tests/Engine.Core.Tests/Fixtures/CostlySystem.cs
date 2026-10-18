using Engine.Core.Ecs;

namespace Engine.Core.Tests.Fixtures;

/// <summary>Test system that "takes" a fixed number of clock ticks each time it runs.</summary>
internal sealed class CostlySystem(FakeClock clock, long cost) : ISystem
{
    public void Update(World world, double dt) => clock.Advance(cost);
}
