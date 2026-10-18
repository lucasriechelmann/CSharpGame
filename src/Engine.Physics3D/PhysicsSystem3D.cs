using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;

namespace Engine.Physics3D;

/// <summary>
/// Moves every <see cref="PhysicsBody3D"/> that has a <see cref="Transform"/> and a <see cref="BoxCollider3D"/>: applies gravity, then
/// moves along X, then Z, then Y, stopping at <see cref="StaticSolid3D"/> boxes. Horizontal first and vertical last, so a body that
/// lands finds its floor after its sideways motion is settled. Run it in the fixed-step group, after any system that changes velocities.
/// Bodies do not collide with each other yet (see the architecture notes).
/// </summary>
public sealed class PhysicsSystem3D : ISystem
{
    /// <summary>How far to look for a surface when reporting contacts; far above float noise (<see cref="CollisionSolver3D.Epsilon"/>), far below a visible gap.</summary>
    private const float ContactSkin = 0.01f;

    private readonly PhysicsSettings3D _settings;
    private readonly CollisionSolver3D _solver = new();
    private readonly List<Entity> _solidEntities = new();
    private readonly EntityAction<Transform, BoxCollider3D, StaticSolid3D> _collectSolid;
    private readonly EntityAction<Transform, PhysicsBody3D, BoxCollider3D> _moveBody;
    private float _dt;

    /// <summary>Creates the system.</summary>
    /// <param name="settings">Gravity and fall-speed limits; defaults are used when null.</param>
    public PhysicsSystem3D(PhysicsSettings3D? settings = null)
    {
        _settings = settings ?? new PhysicsSettings3D();
        _collectSolid = CollectSolid; // created once so the per-step queries do not allocate delegates
        _moveBody = MoveBody;
    }

    /// <summary>
    /// Finds the nearest static solid a ray hits, among the solids gathered in the last step (so before the first step there are none).
    /// </summary>
    /// <param name="ray">The ray (unit direction), for example from <c>Camera3D.ScreenPointToRay</c>.</param>
    /// <param name="maxDistance">How far along the ray to look.</param>
    /// <param name="hit">The nearest hit.</param>
    /// <param name="entity">The entity that owns the box that was hit.</param>
    /// <returns>True when something was hit.</returns>
    public bool Raycast(in Ray ray, float maxDistance, out RaycastHit3D hit, out Entity entity)
    {
        entity = Entity.Null;
        if (!_solver.Raycast(ray, maxDistance, out hit)) return false;

        entity = _solidEntities[hit.Index];
        return true;
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;

        _solver.StaticBoxes.Clear();
        _solidEntities.Clear();
        world.Query(_collectSolid);
        world.Query(_moveBody);
    }

    private void CollectSolid(Entity entity, ref Transform transform, ref BoxCollider3D collider, ref StaticSolid3D solid)
    {
        _solver.StaticBoxes.Add(collider.GetBounds(transform.Position));
        _solidEntities.Add(entity);
    }

    private void MoveBody(Entity entity, ref Transform transform, ref PhysicsBody3D body, ref BoxCollider3D collider)
    {
        // Gravity first (semi-implicit Euler: update the velocity, then move with it), clamped to the terminal fall speed.
        body.Velocity.Y += _settings.Gravity * body.GravityScale * _dt;
        body.Velocity.Y = MathF.Max(body.Velocity.Y, -_settings.MaxFallSpeed);

        // Friction acts while standing on something (contacts from the last step), slowing the horizontal motion without reversing it.
        if (body.IsGrounded && _settings.GroundFriction > 0f) ApplyFriction(ref body.Velocity);

        var box = collider.GetBounds(transform.Position);
        var moved = Vector3.Zero;
        MoveAlong(ref box, ref moved, ref body.Velocity.X, Axis3D.X);
        MoveAlong(ref box, ref moved, ref body.Velocity.Z, Axis3D.Z);
        MoveAlong(ref box, ref moved, ref body.Velocity.Y, Axis3D.Y);

        transform.Position += moved;
        body.Contacts = DetectContacts(box);
    }

    private void ApplyFriction(ref Vector3 velocity)
    {
        var horizontal = new Vector2(velocity.X, velocity.Z);
        float speed = horizontal.Length();
        if (speed == 0f) return;

        float slowed = MathF.Max(speed - _settings.GroundFriction * _dt, 0f);
        horizontal *= slowed / speed;
        velocity.X = horizontal.X;
        velocity.Z = horizontal.Y;
    }

    /// <summary>Sweeps the box along one axis by its velocity component, moves it, and zeroes the velocity when something blocked it.</summary>
    private void MoveAlong(ref Aabb3 box, ref Vector3 moved, ref float velocity, Axis3D axis)
    {
        float delta = velocity * _dt;
        if (delta == 0f) return;

        float allowed = _solver.Sweep(box, axis, delta, out bool hit);
        var step = AxisMath.Along(axis, allowed);
        box = box.Translated(step);
        moved += step;
        if (hit) velocity = 0f;
    }

    /// <summary>
    /// Probes a hair's breadth in each direction so contacts stay reported while a body rests against a surface, not only on the step
    /// where it arrived (a body pushed into a wall has zero velocity and would otherwise report nothing).
    /// </summary>
    private CollisionFlags3D DetectContacts(in Aabb3 box)
    {
        var contacts = CollisionFlags3D.None;
        _solver.Sweep(box, Axis3D.Y, -ContactSkin, out bool below);
        _solver.Sweep(box, Axis3D.Y, ContactSkin, out bool above);
        _solver.Sweep(box, Axis3D.X, -ContactSkin, out bool minX);
        _solver.Sweep(box, Axis3D.X, ContactSkin, out bool maxX);
        _solver.Sweep(box, Axis3D.Z, -ContactSkin, out bool minZ);
        _solver.Sweep(box, Axis3D.Z, ContactSkin, out bool maxZ);

        if (below) contacts |= CollisionFlags3D.Below;
        if (above) contacts |= CollisionFlags3D.Above;
        if (minX) contacts |= CollisionFlags3D.MinX;
        if (maxX) contacts |= CollisionFlags3D.MaxX;
        if (minZ) contacts |= CollisionFlags3D.MinZ;
        if (maxZ) contacts |= CollisionFlags3D.MaxZ;
        return contacts;
    }
}
