using Engine.Core.Assets;

namespace Engine.Audio;

/// <summary>Asset loader for <c>.wav</c> sound files; register it so <c>Assets.Load&lt;AudioClip&gt;("sounds/jump.wav")</c> works.</summary>
public sealed class AudioClipLoader : IAssetLoader<AudioClip>
{
    /// <inheritdoc />
    public AudioClip Load(string fullPath)
    {
        try
        {
            return WavDecoder.Decode(File.ReadAllBytes(fullPath));
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"Cannot load sound '{fullPath}': {ex.Message}", ex);
        }
    }
}
