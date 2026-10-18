using Engine.Core;

namespace Engine.Core.Tests;

public class MathHelperTests
{
    [Theory]
    [InlineData(0f, 10f, 3f, 3f)]
    [InlineData(0f, -10f, 3f, -3f)]
    [InlineData(9f, 10f, 3f, 10f)]    // would overshoot, so it lands exactly on the target
    [InlineData(5f, 5f, 3f, 5f)]
    [InlineData(0f, 10f, 0f, 0f)]
    public void MoveToward_steps_without_overshooting(float current, float target, float maxDelta, float expected)
    {
        Assert.Equal(expected, MathHelper.MoveToward(current, target, maxDelta));
    }

    [Fact]
    public void MoveToward_rejects_negative_step()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MathHelper.MoveToward(0f, 1f, -1f));
    }
}
