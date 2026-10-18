using Engine.Core.Input;

namespace Engine.Core.Tests.Input;

public class ActionMapRebindTests
{
    private readonly InputState _input = new();
    private readonly ActionMap _map;

    public ActionMapRebindTests() => _map = new ActionMap(_input);

    [Fact]
    public void HasAction_and_HasAxis_tell_bound_names_from_typos()
    {
        _map.Bind("Jump", KeyCode.Space).BindAxis("MoveX", KeyCode.A, KeyCode.D);

        Assert.True(_map.HasAction("Jump"));
        Assert.False(_map.HasAction("Jmp"));
        Assert.True(_map.HasAxis("MoveX"));
        Assert.False(_map.HasAxis("Jump"));
    }

    [Fact]
    public void ClearAction_drops_the_old_keys_but_keeps_the_action_asking_safe()
    {
        _map.Bind("Jump", KeyCode.Space);

        _map.ClearAction("Jump");
        _input.SetKey(KeyCode.Space, true);

        Assert.False(_map.IsDown("Jump")); // no longer bound, but asking does not throw
        _map.Bind("Jump", KeyCode.W);
        _input.SetKey(KeyCode.W, true);
        Assert.True(_map.IsDown("Jump"));
    }

    [Fact]
    public void ClearAxis_leaves_a_zero_axis_until_pairs_are_bound_again()
    {
        _map.BindAxis("MoveX", KeyCode.A, KeyCode.D);
        _input.SetKey(KeyCode.D, true);
        Assert.Equal(1f, _map.GetAxis("MoveX"));

        _map.ClearAxis("MoveX");
        Assert.Equal(0f, _map.GetAxis("MoveX"));

        _map.BindAxis("MoveX", KeyCode.Left, KeyCode.Right);
        _input.SetKey(KeyCode.Right, true);
        Assert.Equal(1f, _map.GetAxis("MoveX"));
    }

    [Fact]
    public void Clearing_an_unknown_name_throws()
    {
        Assert.Throws<KeyNotFoundException>(() => _map.ClearAction("Nope"));
        Assert.Throws<KeyNotFoundException>(() => _map.ClearAxis("Nope"));
    }
}
