namespace Engine.Audio;

/// <summary>
/// The tree of <see cref="AudioGroup"/>s a device mixes sounds through. It starts with one <see cref="Master"/> group; games add
/// groups (<c>Mixer.CreateGroup("Music")</c>) and play sounds into them, then expose the volumes in a settings menu.
/// </summary>
public sealed class AudioMixer
{
    private readonly Dictionary<string, AudioGroup> _groups = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a mixer holding only the master group.</summary>
    public AudioMixer()
    {
        Master = new AudioGroup("Master", null);
        _groups[Master.Name] = Master;
    }

    /// <summary>The root group everything is mixed into.</summary>
    public AudioGroup Master { get; }

    /// <summary>Every group, the master first.</summary>
    public IEnumerable<AudioGroup> Groups => _groups.Values;

    /// <summary>Adds a group.</summary>
    /// <param name="name">A unique name.</param>
    /// <param name="parent">The group to mix into, or null for the master.</param>
    /// <exception cref="ArgumentException">The name is empty or already used.</exception>
    public AudioGroup CreateGroup(string name, AudioGroup? parent = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A group needs a name.", nameof(name));
        if (_groups.ContainsKey(name)) throw new ArgumentException($"A group named '{name}' already exists.", nameof(name));
        if (parent != null && !(_groups.TryGetValue(parent.Name, out var known) && ReferenceEquals(known, parent)))
            throw new ArgumentException("The parent group belongs to another mixer.", nameof(parent));

        var group = new AudioGroup(name, parent ?? Master);
        _groups[name] = group;
        return group;
    }

    /// <summary>Finds a group by name.</summary>
    /// <exception cref="KeyNotFoundException">No group has that name.</exception>
    public AudioGroup Find(string name) =>
        _groups.TryGetValue(name, out var group) ? group : throw new KeyNotFoundException($"No audio group named '{name}'. Known groups: {string.Join(", ", _groups.Keys)}.");

    /// <summary>Finds a group by name, or returns false.</summary>
    public bool TryFind(string name, out AudioGroup? group) => _groups.TryGetValue(name, out group);
}
