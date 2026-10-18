using Engine.Core.Input;

namespace Engine.Core.Tests.Input;

public class ActionMapTests
{
    private readonly InputState _input = new();
    private readonly ActionMap _map;

    public ActionMapTests() => _map = new ActionMap(_input);

    [Fact]
    public void Action_follows_its_bound_key()
    {
        _map.Bind("Jump", KeyCode.Space);

        _input.SetKey(KeyCode.Space, true);

        Assert.True(_map.IsDown("Jump"));
        Assert.True(_map.WasPressed("Jump"));
        Assert.False(_map.WasReleased("Jump"));
    }

    [Fact]
    public void Action_with_several_bindings_fires_for_any_of_them()
    {
        _map.Bind("Fire", KeyCode.F).Bind("Fire", MouseButton.Left);

        _input.SetMouseButton(MouseButton.Left, true);
        Assert.True(_map.IsDown("Fire"));

        _input.SetMouseButton(MouseButton.Left, false);
        _input.SetKey(KeyCode.F, true);
        Assert.True(_map.IsDown("Fire"));
    }

    [Fact]
    public void Unbound_action_throws_to_surface_typos()
    {
        Assert.Throws<KeyNotFoundException>(() => _map.IsDown("Jmup"));
        Assert.Throws<KeyNotFoundException>(() => _map.GetAxis("MoveX"));
    }

    [Fact]
    public void Axis_goes_negative_and_positive()
    {
        _map.BindAxis("MoveX", KeyCode.A, KeyCode.D);

        Assert.Equal(0f, _map.GetAxis("MoveX"));

        _input.SetKey(KeyCode.D, true);
        Assert.Equal(1f, _map.GetAxis("MoveX"));

        _input.SetKey(KeyCode.D, false);
        _input.SetKey(KeyCode.A, true);
        Assert.Equal(-1f, _map.GetAxis("MoveX"));
    }

    [Fact]
    public void Opposing_keys_cancel_out()
    {
        _map.BindAxis("MoveX", KeyCode.A, KeyCode.D);
        _input.SetKey(KeyCode.A, true);
        _input.SetKey(KeyCode.D, true);

        Assert.Equal(0f, _map.GetAxis("MoveX"));
    }

    [Fact]
    public void Axis_with_alternative_bindings_is_clamped_to_one()
    {
        _map.BindAxis("MoveX", KeyCode.A, KeyCode.D).BindAxis("MoveX", KeyCode.Left, KeyCode.Right);
        _input.SetKey(KeyCode.D, true);
        _input.SetKey(KeyCode.Right, true);

        Assert.Equal(1f, _map.GetAxis("MoveX"));
    }

    [Fact]
    public void Edges_clear_after_end_of_frame()
    {
        _map.Bind("Jump", KeyCode.Space);
        _input.SetKey(KeyCode.Space, true);
        _input.EndFrame();

        Assert.False(_map.WasPressed("Jump"));
        Assert.True(_map.IsDown("Jump"));
    }
}
