using Engine.Core.Ecs;
using Engine.Core.Input;
using Engine.Physics2D;

namespace Platformer;

/// <summary>
/// Reads the player's input once per frame and writes it into the hero's <see cref="PlatformerIntent"/>. The fixed-step
/// controller consumes that intent, so input edges are never read inside the fixed loop (see <see cref="PlatformerIntent"/>).
/// </summary>
public sealed class PlayerInputSystem : ISystem
{
    private readonly ActionMap _actions;
    private readonly EntityAction<PlatformerIntent> _apply;
    private float _move;
    private bool _jumpHeld;
    private bool _jumpPressed;

    /// <summary>Creates the system.</summary>
    /// <param name="actions">The action map to read movement and jump from.</param>
    public PlayerInputSystem(ActionMap actions)
    {
        _actions = actions;
        _apply = Apply; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _move = _actions.GetAxis(GameActions.MoveX);
        _jumpHeld = _actions.IsDown(GameActions.Jump);
        _jumpPressed = _actions.WasPressed(GameActions.Jump);

        world.Query(_apply);
    }

    private void Apply(Entity entity, ref PlatformerIntent intent)
    {
        intent.MoveX = _move;
        intent.JumpHeld = _jumpHeld;
        if (_jumpPressed) intent.JumpPressedPending = true; // sticky until the fixed-step controller consumes it
    }
}
