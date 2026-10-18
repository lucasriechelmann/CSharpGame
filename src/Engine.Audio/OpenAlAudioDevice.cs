using Silk.NET.OpenAL;

namespace Engine.Audio;

/// <summary>
/// <see cref="IAudioDevice"/> backed by OpenAL (the bundled OpenAL Soft library). Opens the default output device, creates a fixed
/// pool of sources, and uploads each clip to an OpenAL buffer the first time it plays.
/// </summary>
/// <remarks>
/// Create it with <see cref="TryCreate"/>, which reports a missing library or device instead of throwing, so the game can fall back
/// to <see cref="NullAudioDevice"/>. Once created, any OpenAL error is thrown loudly. OpenAL does not need the window or the GL
/// context, but the device should still be disposed when the game shuts down.
/// </remarks>
public sealed unsafe partial class OpenAlAudioDevice : IAudioDevice
{
    /// <summary>Number of sounds that can play at once.</summary>
    public const int VoiceCount = 32;

    private readonly AL _al;
    private readonly ALContext _alc;
    private readonly Device* _device;
    private readonly Context* _context;
    private readonly uint[] _sources = new uint[VoiceCount];
    private readonly VoicePool _pool = new(VoiceCount);
    private readonly Dictionary<AudioClip, uint> _buffers = new();
    private float _masterVolume = 1f;
    private bool _disposed;

    private OpenAlAudioDevice(AL al, ALContext alc, Device* device, Context* context, string description)
    {
        _al = al;
        _alc = alc;
        _device = device;
        _context = context;
        Description = description;

        for (int i = 0; i < _sources.Length; i++)
        {
            _sources[i] = _al.GenSource();
            Check("creating a source");
        }
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public bool IsAudible => true;

    /// <inheritdoc />
    public int ActiveVoices => _pool.ActiveCount;

    /// <inheritdoc />
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Math.Clamp(value, 0f, 1f);
            _al.SetListenerProperty(ListenerFloat.Gain, _masterVolume);
            Check("setting the master volume");
        }
    }

    /// <summary>Opens the default output device.</summary>
    /// <param name="device">The device on success; null otherwise.</param>
    /// <param name="error">Why it failed (no OpenAL library, no output device, ...); empty on success.</param>
    /// <returns>True when an audible device is ready.</returns>
    public static bool TryCreate(out OpenAlAudioDevice? device, out string error)
    {
        device = null;
        error = string.Empty;
        try
        {
            var alc = ALContext.GetApi(soft: true);
            var al = AL.GetApi(soft: true);

            var handle = alc.OpenDevice(string.Empty);
            if (handle == null)
            {
                error = "OpenAL found no output device";
                return false;
            }

            var context = alc.CreateContext(handle, null);
            if (context == null || !alc.MakeContextCurrent(context))
            {
                alc.CloseDevice(handle);
                error = "OpenAL could not create an audio context";
                return false;
            }

            string name = alc.GetContextProperty(handle, GetContextString.DeviceSpecifier) ?? "default device";
            device = new OpenAlAudioDevice(al, alc, handle, context, $"OpenAL: {name}");
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or NotSupportedException or BadImageFormatException)
        {
            error = $"OpenAL library unavailable: {ex.Message}";
            return false;
        }
    }

    /// <summary>Throws if the last OpenAL call failed. OpenAL reports errors through a polled flag, not return values.</summary>
    private void Check(string operation)
    {
        var error = _al.GetError();
        if (error != AudioError.NoError)
            throw new InvalidOperationException($"OpenAL error while {operation}: {error}.");
    }
}
