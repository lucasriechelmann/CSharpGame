using System.Numerics;
using Engine.Core.Input;

namespace Engine.Core.Tests;

public class InputStateMouseOffsetTests
{
    [Fact]
    public void The_mouse_is_relative_to_the_offset_a_host_sets()
    {
        var input = new InputState();
        input.SetMousePosition(new Vector2(300, 200));

        input.SetMouseOffset(new Vector2(240, 80));

        Assert.Equal(new Vector2(60, 120), input.MousePosition);
    }

    [Fact]
    public void Moving_the_mouse_after_setting_the_offset_keeps_the_offset()
    {
        var input = new InputState();
        input.SetMouseOffset(new Vector2(10, 10));

        input.SetMousePosition(new Vector2(15, 25));

        Assert.Equal(new Vector2(5, 15), input.MousePosition);
    }

    [Fact]
    public void With_no_offset_the_position_is_the_window_position()
    {
        var input = new InputState();
        input.SetMousePosition(new Vector2(7, 9));

        Assert.Equal(new Vector2(7, 9), input.MousePosition);
    }
}
