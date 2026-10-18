using System.Globalization;

namespace Engine.Tilemaps.Tiled;

/// <summary>
/// The custom properties of a Tiled map, layer, tile or object: name to value, kept as text (Tiled stores booleans, numbers and strings, and
/// the typed getters parse what the caller expects, failing loudly when a property is there but is not that type).
/// </summary>
public sealed class TiledProperties
{
    /// <summary>No properties.</summary>
    public static TiledProperties Empty { get; } = new(new Dictionary<string, string>());

    private readonly IReadOnlyDictionary<string, string> _values;

    /// <summary>Wraps a name-to-text dictionary.</summary>
    /// <param name="values">The properties.</param>
    public TiledProperties(IReadOnlyDictionary<string, string> values) => _values = values;

    /// <summary>Number of properties.</summary>
    public int Count => _values.Count;

    /// <summary>The names, in no particular order.</summary>
    public IEnumerable<string> Names => _values.Keys;

    /// <summary>True when a property of that name exists.</summary>
    /// <param name="name">The property name.</param>
    public bool Contains(string name) => _values.ContainsKey(name);

    /// <summary>The property as text, or <paramref name="fallback"/> when absent.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">Returned when there is no such property.</param>
    public string? GetString(string name, string? fallback = null) => _values.TryGetValue(name, out var value) ? value : fallback;

    /// <summary>The property as a boolean, or <paramref name="fallback"/> when absent.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">Returned when there is no such property.</param>
    /// <exception cref="InvalidOperationException">The property exists but is not true or false.</exception>
    public bool GetBool(string name, bool fallback = false)
    {
        if (!_values.TryGetValue(name, out var value)) return fallback;
        if (bool.TryParse(value, out bool result)) return result;
        throw new InvalidOperationException($"Tiled property \"{name}\" is \"{value}\", which is not true or false.");
    }

    /// <summary>The property as a number, or <paramref name="fallback"/> when absent.</summary>
    /// <param name="name">The property name.</param>
    /// <param name="fallback">Returned when there is no such property.</param>
    /// <exception cref="InvalidOperationException">The property exists but is not a number.</exception>
    public float GetFloat(string name, float fallback = 0f)
    {
        if (!_values.TryGetValue(name, out var value)) return fallback;
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) return result;
        throw new InvalidOperationException($"Tiled property \"{name}\" is \"{value}\", which is not a number.");
    }
}
