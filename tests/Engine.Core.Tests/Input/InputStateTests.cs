using System.Numerics;
using Engine.Core.Input;

namespace Engine.Core.Tests.Input;

public class InputStateTests
{
    [Fact]
    public void Key_down_raises_pressed_once_and_stays_down()
    {
        var input = new InputState();
        input.SetKey(KeyCode.Space, true);

        Assert.True(input.IsDown(KeyCode.Space));
        Assert.True(input.WasPressed(KeyCode.Space));

        input.EndFrame();

        Assert.True(input.IsDown(KeyCode.Space));
        Assert.False(input.WasPressed(KeyCode.Space));
    }

    [Fact]
    public void Key_up_raises_released_once()
    {
        var input = new InputState();
        input.SetKey(KeyCode.A, true);
        input.EndFrame();

        input.SetKey(KeyCode.A, false);
        Assert.False(input.IsDown(KeyCode.A));
        Assert.True(input.WasReleased(KeyCode.A));

        input.EndFrame();
        Assert.False(input.WasReleased(KeyCode.A));
    }

    [Fact]
    public void Key_repeat_does_not_raise_pressed_again()
    {
        var input = new InputState();
        input.SetKey(KeyCode.D, true);
        input.EndFrame();

        input.SetKey(KeyCode.D, true); // OS key repeat
        Assert.False(input.WasPressed(KeyCode.D));
    }

    [Fact]
    public void Release_of_a_key_that_is_up_is_ignored()
    {
        var input = new InputState();
        input.SetKey(KeyCode.W, false);

        Assert.False(input.WasReleased(KeyCode.W));
    }

    [Fact]
    public void Tap_within_a_single_frame_is_seen_as_pressed_and_released()
    {
        var input = new InputState();
        input.SetKey(KeyCode.Enter, true);
        input.SetKey(KeyCode.Enter, false);

        Assert.True(input.WasPressed(KeyCode.Enter));
        Assert.True(input.WasReleased(KeyCode.Enter));
        Assert.False(input.IsDown(KeyCode.Enter));
    }

    [Fact]
    public void None_key_is_ignored()
    {
        var input = new InputState();
        input.SetKey(KeyCode.None, true);

        Assert.False(input.IsDown(KeyCode.None));
    }

    [Fact]
    public void Mouse_buttons_track_edges_like_keys()
    {
        var input = new InputState();
        input.SetMouseButton(MouseButton.Left, true);

        Assert.True(input.IsDown(MouseButton.Left));
        Assert.True(input.WasPressed(MouseButton.Left));
        Assert.False(input.IsDown(MouseButton.Right));

        input.EndFrame();
        input.SetMouseButton(MouseButton.Left, false);
        Assert.True(input.WasReleased(MouseButton.Left));
    }

    [Fact]
    public void Scroll_accumulates_within_a_frame_and_resets_at_end_of_frame()
    {
        var input = new InputState();
        input.AddScroll(1f);
        input.AddScroll(2f);
        Assert.Equal(3f, input.ScrollDelta);

        input.EndFrame();
        Assert.Equal(0f, input.ScrollDelta);
    }

    [Fact]
    public void Mouse_position_persists_across_frames()
    {
        var input = new InputState();
        input.SetMousePosition(new Vector2(12, 34));
        input.EndFrame();

        Assert.Equal(new Vector2(12, 34), input.MousePosition);
    }

    [Fact]
    public void ReleaseAll_lets_go_of_everything_and_raises_release_edges()
    {
        var input = new InputState();
        input.SetKey(KeyCode.W, true);
        input.SetMouseButton(MouseButton.Right, true);
        input.EndFrame();

        input.ReleaseAll();

        Assert.False(input.IsDown(KeyCode.W));
        Assert.True(input.WasReleased(KeyCode.W));
        Assert.False(input.IsDown(MouseButton.Right));
        Assert.True(input.WasReleased(MouseButton.Right));
    }
}
