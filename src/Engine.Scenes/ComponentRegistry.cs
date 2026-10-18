namespace Engine.Scenes;

/// <summary>
/// The list of component types that can appear in scene and prefab files, each under a stable name. Registering is explicit
/// on purpose: only components you list are saved, a misspelt name in a file fails loudly, and the name in the file does not
/// change when the C# type is renamed or moved.
/// </summary>
public sealed class ComponentRegistry
{
    private readonly Dictionary<string, ComponentSerializer> _byName = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, ComponentSerializer> _byType = new();

    /// <summary>All registrations in the order they were added (the order components are written in).</summary>
    internal IEnumerable<ComponentSerializer> Serializers => _byName.Values;

    /// <summary>Number of registered component types.</summary>
    public int Count => _byName.Count;

    /// <summary>The names of all registered components.</summary>
    public IEnumerable<string> Names => _byName.Keys;

    /// <summary>Registers a component type under a file name.</summary>
    /// <typeparam name="T">The component struct. Its public fields are what gets saved.</typeparam>
    /// <param name="name">Unique name used in JSON files.</param>
    /// <returns>This registry, so calls can be chained.</returns>
    /// <exception cref="ArgumentException">The name or the type is already registered.</exception>
    public ComponentRegistry Register<T>(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A component name cannot be empty.", nameof(name));
        if (_byName.ContainsKey(name)) throw new ArgumentException($"A component named '{name}' is already registered.", nameof(name));
        if (_byType.ContainsKey(typeof(T))) throw new ArgumentException($"{typeof(T).Name} is already registered as '{_byType[typeof(T)].Name}'.", nameof(name));

        var serializer = new TypedComponentSerializer<T>(name);
        _byName[name] = serializer;
        _byType[typeof(T)] = serializer;
        return this;
    }

    /// <summary>True when a component with that name is registered.</summary>
    public bool Contains(string name) => _byName.ContainsKey(name);

    /// <summary>Finds a registration or throws a message that names the unknown component.</summary>
    internal ComponentSerializer Get(string name) =>
        _byName.TryGetValue(name, out var serializer)
            ? serializer
            : throw new InvalidOperationException($"Unknown component '{name}'. Register it with ComponentRegistry.Register<T>(\"{name}\").");
}
