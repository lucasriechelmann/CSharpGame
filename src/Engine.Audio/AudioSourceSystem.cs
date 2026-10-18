using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Audio;

/// <summary>
/// Plays the sounds that <see cref="AudioSource"/> components describe: starts them, keeps positioned ones at their entity, follows
/// volume changes, stops them when their entity dies, and places the device's listener at the <see cref="AudioListener"/>.
/// Run it per frame (not in the fixed step), after anything that moves entities.
/// </summary>
public sealed class AudioSourceSystem : ISystem
{
    private readonly IAudioDevice _device;
    private readonly AssetManager _assets;
    private readonly Dictionary<Entity, VoiceId> _voices = new();
    private readonly List<Entity> _stale = new();
    private readonly EntityAction<AudioListener, Transform> _placeListener;
    private readonly EntityAction<AudioSource, Transform> _updateSource;
    private World _world = null!;
    private Vector3 _listenerPosition;
    private Vector3 _listenerForward = -Vector3.UnitZ;
    private Vector3 _listenerUp = Vector3.UnitY;
    private bool _hasListener;

    /// <summary>Creates the system.</summary>
    /// <param name="device">Where sounds are played.</param>
    /// <param name="assets">Resolves a source's clip handle.</param>
    public AudioSourceSystem(IAudioDevice device, AssetManager assets)
    {
        _device = device;
        _assets = assets;
        _placeListener = PlaceListener;
        _updateSource = UpdateSource;
    }

    /// <summary>Number of entities whose source is tracked (playing, or already finished).</summary>
    public int TrackedCount => _voices.Count;

    /// <summary>
    /// Starts an entity's source now, restarting it if it is playing. Use it for sources with <see cref="AudioSource.PlayOnStart"/> off.
    /// </summary>
    /// <exception cref="InvalidOperationException">The entity has no <see cref="AudioSource"/> and <see cref="Transform"/>, or names an unknown group.</exception>
    public void Play(World world, Entity entity)
    {
        if (!world.TryGet<AudioSource>(entity, out var source) || !world.TryGet<Transform>(entity, out var transform))
            throw new InvalidOperationException($"{entity} needs an AudioSource and a Transform to play.");

        Stop(entity);
        _world = world;
        Start(entity, ref source, in transform);
    }

    /// <summary>Stops an entity's sound. The source stays; <see cref="Play"/> starts it again.</summary>
    public void Stop(Entity entity)
    {
        if (_voices.TryGetValue(entity, out var voice)) _device.Stop(voice);
        _voices[entity] = VoiceId.None;
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _world = world;

        _hasListener = false;
        world.Query(_placeListener);
        if (_hasListener) _device.SetListener(_listenerPosition, _listenerForward, _listenerUp);

        world.Query(_updateSource);
        StopOrphans(world);
    }


    private void PlaceListener(Entity entity, ref AudioListener listener, ref Transform transform)
    {
        if (_hasListener) return;

        var world = TransformHierarchy.GetWorldTransform(_world, entity);
        _listenerPosition = world.Position;
        _listenerForward = Vector3.Transform(-Vector3.UnitZ, world.Rotation);
        _listenerUp = Vector3.Transform(Vector3.UnitY, world.Rotation);
        _hasListener = true;
    }

    private void UpdateSource(Entity entity, ref AudioSource source, ref Transform transform)
    {
        if (!_voices.TryGetValue(entity, out var voice))
        {
            if (source.PlayOnStart) Start(entity, ref source, in transform);
            return;
        }

        if (voice.IsNone) return; // finished or stopped; the entity keeps its entry so a one-shot does not restart every frame

        if (!_device.IsPlaying(voice))
        {
            _voices[entity] = VoiceId.None;
            return;
        }

        _device.SetVolume(voice, source.Volume);
        if (source.Space != AudioSpace.Flat) _device.SetPosition(voice, EmitterPosition(entity, in source, in transform));
    }

    private void Start(Entity entity, ref AudioSource source, in Transform transform)
    {
        if (!source.Clip.IsValid) throw new InvalidOperationException($"{entity} has an AudioSource with no clip.");

        var clip = _assets.Get(source.Clip);
        var group = FindGroup(entity, source.Group);
        VoiceId voice;
        if (source.Space == AudioSpace.Flat)
        {
            voice = _device.PlayIn(clip, group, source.Volume, source.Pitch, source.Loop);
        }
        else
        {
            var spatial = new Spatialization(source.MinDistance, source.MaxDistance, source.Rolloff);
            voice = _device.PlayAt(clip, EmitterPosition(entity, in source, in transform), spatial, group, source.Volume, source.Pitch, source.Loop);
        }

        _voices[entity] = voice;
    }

    /// <summary>The entity's world position; for 2D sounds the depth is replaced by the listener's, so draw order never counts as distance.</summary>
    private Vector3 EmitterPosition(Entity entity, in AudioSource source, in Transform transform)
    {
        var position = TransformHierarchy.GetWorldPosition(_world, entity, in transform);
        return source.Space == AudioSpace.Positional2D ? new Vector3(position.X, position.Y, _hasListener ? _listenerPosition.Z : 0f) : position;
    }

    private AudioGroup? FindGroup(Entity entity, string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_device.Mixer.TryFind(name, out var group)) return group;

        throw new InvalidOperationException($"{entity} plays through audio group '{name}', which the mixer does not have.");
    }

    /// <summary>Stops the sound of every tracked entity that is gone or lost its source, and forgets it.</summary>
    private void StopOrphans(World world)
    {
        _stale.Clear();
        foreach (var entity in _voices.Keys)
        {
            if (!world.IsAlive(entity) || !world.Has<AudioSource>(entity)) _stale.Add(entity);
        }

        foreach (var entity in _stale)
        {
            if (_voices.Remove(entity, out var voice)) _device.Stop(voice);
        }
    }
}
