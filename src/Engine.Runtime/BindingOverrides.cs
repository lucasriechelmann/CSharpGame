using Engine.Core.Input;

namespace Engine.Runtime;

/// <summary>
/// Applies the player's key bindings from <see cref="GameSettings"/> to the game's <see cref="ActionMap"/>: for every action or axis the settings
/// list, the game's own bindings are replaced by the listed inputs. Actions the settings do not mention keep their defaults.
/// </summary>
/// <remarks>Fails loudly: an unknown key name or an action the game never bound (almost always a typo) throws <see cref="InvalidDataException"/> instead of being ignored, so a broken settings file is noticed at start-up and not as a key that silently does nothing.</remarks>
public static class BindingOverrides
{
    private const string MousePrefix = "Mouse.";

    /// <summary>Rebinds <paramref name="map"/> as <paramref name="settings"/> says.</summary>
    /// <exception cref="InvalidDataException">A name is not a key, a mouse button or a bound action, or an axis pair is malformed.</exception>
    public static void Apply(ActionMap map, GameSettings settings)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(settings);

        // Parse everything before changing anything, so a bad entry cannot leave the map half rebound.
        var actions = new List<(string Action, List<Action<ActionMap>> Binds)>();
        if (settings.Bindings != null)
        {
            foreach (var (action, names) in settings.Bindings)
            {
                if (!map.HasAction(action)) throw new InvalidDataException($"game.settings.json: bindings names an action '{action}' the game does not have.");

                var binds = new List<Action<ActionMap>>();
                foreach (string name in names) binds.Add(ParseBinding(action, name));
                actions.Add((action, binds));
            }
        }

        var axes = new List<(string Axis, List<(KeyCode Negative, KeyCode Positive)> Pairs)>();
        if (settings.AxisBindings != null)
        {
            foreach (var (axis, pairs) in settings.AxisBindings)
            {
                if (!map.HasAxis(axis)) throw new InvalidDataException($"game.settings.json: axisBindings names an axis '{axis}' the game does not have.");

                var parsed = new List<(KeyCode, KeyCode)>();
                foreach (string pair in pairs) parsed.Add(ParsePair(axis, pair));
                axes.Add((axis, parsed));
            }
        }

        foreach (var (action, binds) in actions)
        {
            map.ClearAction(action);
            foreach (var bind in binds) bind(map);
        }

        foreach (var (axis, pairs) in axes)
        {
            map.ClearAxis(axis);
            foreach (var (negative, positive) in pairs) map.BindAxis(axis, negative, positive);
        }
    }

    private static Action<ActionMap> ParseBinding(string action, string name)
    {
        if (name.StartsWith(MousePrefix, StringComparison.OrdinalIgnoreCase))
        {
            string button = name[MousePrefix.Length..];
            if (!Enum.TryParse<MouseButton>(button, ignoreCase: true, out var parsedButton) || !Enum.IsDefined(parsedButton))
                throw new InvalidDataException($"game.settings.json: '{name}' (action '{action}') is not a mouse button; use Mouse.Left, Mouse.Right or Mouse.Middle.");

            return map => map.Bind(action, parsedButton);
        }

        var key = ParseKey(action, name);
        return map => map.Bind(action, key);
    }

    private static (KeyCode, KeyCode) ParsePair(string axis, string pair)
    {
        string[] parts = pair.Split(':');
        if (parts.Length != 2) throw new InvalidDataException($"game.settings.json: '{pair}' (axis '{axis}') must be written negative:positive, for example A:D.");

        return (ParseKey(axis, parts[0]), ParseKey(axis, parts[1]));
    }

    private static KeyCode ParseKey(string owner, string name)
    {
        if (!Enum.TryParse<KeyCode>(name.Trim(), ignoreCase: true, out var key) || !Enum.IsDefined(key) || key == KeyCode.None)
            throw new InvalidDataException($"game.settings.json: '{name}' (for '{owner}') is not a key name.");

        return key;
    }
}
