using Engine.Audio;
using Engine.Core.Assets;
using Engine.Core.Ecs;
using Engine.Physics2D;

namespace Platformer;

/// <summary>
/// Plays the hero's jump and landing sounds by watching its physics state change between frames: leaving the ground while moving
/// up is a jump, touching the ground after falling is a landing (louder the harder the fall). Gameplay code only reads
/// <see cref="PhysicsBody"/>; it never needs to know how the jump was triggered. Run it in the frame group.
/// </summary>
public sealed class HeroSoundSystem : ISystem
{
    /// <summary>Slower falls than this (world units per second) make no landing sound, so walking off a ledge stays quiet.</summary>
    private const float MinLandingSpeed = 120f;

    /// <summary>Fall speed at which the landing sound reaches full volume.</summary>
    private const float LoudLandingSpeed = 700f;

    private readonly IAudioDevice _audio;
    private readonly AudioClip _jump;
    private readonly AudioClip _land;
    private readonly EntityAction<PhysicsBody, PlatformerIntent> _apply;
    private bool _wasGrounded = true;
    private float _fastestFall;

    /// <summary>Creates the system.</summary>
    /// <param name="audio">Device the sounds play on.</param>
    /// <param name="jump">Sound for leaving the ground upwards.</param>
    /// <param name="land">Sound for hitting the ground.</param>
    public HeroSoundSystem(IAudioDevice audio, AudioClip jump, AudioClip land)
    {
        _audio = audio;
        _jump = jump;
        _land = land;
        audio.Preload(jump);
        audio.Preload(land);
        _apply = Apply; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt) => world.Query(_apply);

    // The PlatformerIntent component only exists on the hero, which is how this query singles it out.
    private void Apply(Entity entity, ref PhysicsBody body, ref PlatformerIntent intent)
    {
        bool grounded = body.IsGrounded;

        if (_wasGrounded && !grounded && body.Velocity.Y > 0f)
            _audio.Play(_jump, volume: 0.6f);

        if (!grounded)
            _fastestFall = MathF.Min(_fastestFall, body.Velocity.Y);

        if (!_wasGrounded && grounded)
        {
            float speed = -_fastestFall;
            if (speed >= MinLandingSpeed)
                _audio.Play(_land, volume: Math.Clamp(speed / LoudLandingSpeed, 0.25f, 1f));
        }

        if (grounded) _fastestFall = 0f;
        _wasGrounded = grounded;
    }
}
