using Engine.Core.Input;

namespace Engine.Runtime.Tests;

public sealed class BindingOverridesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bindings-" + Guid.NewGuid().ToString("N"));
    private readonly InputState _input = new();
    private readonly ActionMap _map;

    public BindingOverridesTests()
    {
        Directory.CreateDirectory(_folder);
        _map = new ActionMap(_input);
        _map.Bind("Jump", KeyCode.Space).Bind("Fire", KeyCode.F).BindAxis("MoveX", KeyCode.A, KeyCode.D);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static GameSettings With(Dictionary<string, string[]>? bindings = null, Dictionary<string, string[]>? axes = null) =>
        new() { Bindings = bindings, AxisBindings = axes };

    [Fact]
    public void Listed_actions_are_rebound_and_the_rest_keep_their_defaults()
    {
        BindingOverrides.Apply(_map, With(new() { ["Jump"] = ["Up", "Mouse.Right"] }));

        _input.SetKey(KeyCode.Space, true);
        Assert.False(_map.IsDown("Jump"));
        _input.SetKey(KeyCode.Space, false);
        _input.SetMouseButton(MouseButton.Right, true);
        Assert.True(_map.IsDown("Jump"));
        _input.SetKey(KeyCode.F, true);
        Assert.True(_map.IsDown("Fire"));
    }

    [Fact]
    public void Key_names_are_case_insensitive()
    {
        BindingOverrides.Apply(_map, With(new() { ["Fire"] = ["space"] }));

        _input.SetKey(KeyCode.Space, true);

        Assert.True(_map.IsDown("Fire"));
    }

    [Fact]
    public void An_axis_is_rebound_to_negative_positive_pairs()
    {
        BindingOverrides.Apply(_map, With(axes: new() { ["MoveX"] = ["Left:Right"] }));

        _input.SetKey(KeyCode.D, true);
        Assert.Equal(0f, _map.GetAxis("MoveX"));
        _input.SetKey(KeyCode.Left, true);
        Assert.Equal(-1f, _map.GetAxis("MoveX"));
    }

    [Theory]
    [InlineData("Nope")]
    [InlineData("None")]
    [InlineData("Mouse.Side")]
    [InlineData("99999")]
    public void A_bad_input_name_throws_and_leaves_every_binding_untouched(string name)
    {
        var settings = With(new() { ["Fire"] = ["G"], ["Jump"] = [name] });

        Assert.Throws<InvalidDataException>(() => BindingOverrides.Apply(_map, settings));

        _input.SetKey(KeyCode.F, true);
        Assert.True(_map.IsDown("Fire")); // Fire was listed first, but nothing changed
    }

    [Fact]
    public void An_unknown_action_or_axis_or_a_malformed_pair_throws()
    {
        Assert.Throws<InvalidDataException>(() => BindingOverrides.Apply(_map, With(new() { ["Jmp"] = ["Space"] })));
        Assert.Throws<InvalidDataException>(() => BindingOverrides.Apply(_map, With(axes: new() { ["MoveY"] = ["W:S"] })));
        Assert.Throws<InvalidDataException>(() => BindingOverrides.Apply(_map, With(axes: new() { ["MoveX"] = ["Left"] })));
    }

    [Fact]
    public void Bindings_survive_a_settings_file_round_trip()
    {
        string path = Path.Combine(_folder, GameSettings.FileName);
        File.WriteAllText(path, """{ "bindings": { "Jump": ["Z"] }, "axisBindings": { "MoveX": ["J:L"] } }""");

        var loaded = GameSettings.Load(path);
        BindingOverrides.Apply(_map, loaded);

        _input.SetKey(KeyCode.Z, true);
        Assert.True(_map.IsDown("Jump"));
        _input.SetKey(KeyCode.L, true);
        Assert.Equal(1f, _map.GetAxis("MoveX"));
    }
}
