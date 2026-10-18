using Engine.Audio;

namespace Platformer;

/// <summary>The sound effects the gameplay rules play, gathered so systems take one argument and tests can pass silent stand-ins.</summary>
/// <param name="Coin">A coin was collected.</param>
/// <param name="Hurt">The hero lost a life.</param>
/// <param name="Stomp">The hero squashed a creature.</param>
/// <param name="Win">The run was won.</param>
/// <param name="Lose">The run was lost.</param>
/// <param name="Unlock">The exit opened (the last coin was collected).</param>
public sealed record GameSounds(AudioClip Coin, AudioClip Hurt, AudioClip Stomp, AudioClip Win, AudioClip Lose, AudioClip Unlock);
