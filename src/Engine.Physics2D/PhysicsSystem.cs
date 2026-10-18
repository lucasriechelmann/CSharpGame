using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Physics2D;

/// <summary>
/// Moves every <see cref="PhysicsBody"/> that has a <see cref="Transform"/> and a <see cref="BoxCollider"/>: applies
/// gravity, then moves along X and then Y, stopping at tiles and <see cref="StaticSolid"/> boxes. Run it in the
/// fixed-step group, after any system that changes velocities (such as the platformer controller).
/// </summary>
public sealed class PhysicsSystem : ISystem
{
    /// <summary>How far to look for a surface when reporting contacts; far above float noise (<see cref="CollisionSolver.Epsilon"/>), far below a pixel.</summary>
    private const float ContactSkin = 0.01f;

    private readonly PhysicsSettings _settings;
    private readonly CollisionSolver _solver;
    private readonly EntityAction<Transform, BoxCollider, StaticSolid> _collectSolid;
    private readonly EntityAction<Transform, PhysicsBody, BoxCollider> _moveBody;
    private float _dt;

    /// <summary>Creates the system.</summary>
    /// <param name="grid">Tile collision for the level, or null for none.</param>
    /// <param name="settings">Gravity and fall-speed limits; defaults are used when null.</param>
    public PhysicsSystem(TileGrid? grid, PhysicsSettings? settings = null)
    {
        _settings = settings ?? new PhysicsSettings();
        _solver = new CollisionSolver(grid);
        _collectSolid = CollectSolid; // Created once so the per-step queries do not allocate delegates.
        _moveBody = MoveBody;
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;

        _solver.StaticBoxes.Clear();
        world.Query(_collectSolid);
        world.Query(_moveBody);
    }

    private void CollectSolid(Entity entity, ref Transform transform, ref BoxCollider collider, ref StaticSolid solid) =>
        _solver.StaticBoxes.Add(collider.GetBounds(transform.Position));

    private void MoveBody(Entity entity, ref Transform transform, ref PhysicsBody body, ref BoxCollider collider)
    {
        // Gravity first (semi-implicit Euler), clamped to the terminal fall speed.
        body.Velocity.Y += _settings.Gravity * body.GravityScale * _dt;
        body.Velocity.Y = MathF.Max(body.Velocity.Y, -_settings.MaxFallSpeed);

        var box = collider.GetBounds(transform.Position);
        var moved = Vector2.Zero;

        // Horizontal, then vertical. Moving one axis at a time lets a body slide along walls and floors.
        float dx = body.Velocity.X * _dt;
        if (dx != 0f)
        {
            float allowed = _solver.Sweep(box, Axis.X, dx, out bool hit);
            box = box.Translated(new Vector2(allowed, 0f));
            moved.X = allowed;
            if (hit) body.Velocity.X = 0f;
        }

        float dy = body.Velocity.Y * _dt;
        if (dy != 0f)
        {
            float allowed = _solver.Sweep(box, Axis.Y, dy, out bool hit);
            box = box.Translated(new Vector2(0f, allowed));
            moved.Y = allowed;
            if (hit) body.Velocity.Y = 0f;
        }

        transform.Position += new Vector3(moved, 0f);
        body.Contacts = DetectContacts(box);
    }

    /// <summary>
    /// Probes a hair's breadth in each direction so contacts stay reported while a body rests against a surface,
    /// not only on the step where it arrived (a body pushed into a wall has zero velocity and would otherwise report nothing).
    /// </summary>
    private CollisionFlags DetectContacts(in Aabb box)
    {
        var contacts = CollisionFlags.None;
        _solver.Sweep(box, Axis.Y, -ContactSkin, out bool below);
        _solver.Sweep(box, Axis.Y, ContactSkin, out bool above);
        _solver.Sweep(box, Axis.X, -ContactSkin, out bool left);
        _solver.Sweep(box, Axis.X, ContactSkin, out bool right);

        if (below) contacts |= CollisionFlags.Below;
        if (above) contacts |= CollisionFlags.Above;
        if (left) contacts |= CollisionFlags.Left;
        if (right) contacts |= CollisionFlags.Right;
        return contacts;
    }
}
