using Engine.Core.Ecs;

namespace Engine.Core.Tests.Fixtures;

/// <summary>Test system that appends its name to a shared log so execution order can be asserted.</summary>
internal sealed class LogSystem(string name, List<string> log) : ISystem
{
    public void Update(World world, double dt) => log.Add(name);
}
