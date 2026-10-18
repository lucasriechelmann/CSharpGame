using System.Numerics;
using Engine.Core.Components;

namespace Engine.Physics3D;

// Stepping a box body: gravity, friction, a swept move per axis, momentum exchange with the bodies it runs into, and contacts.
public sealed partial class PhysicsSystem3D
{
    private void StepBox(int index)
    {
        var entity = _bodies[index];
        ref var transform = ref _world.Get<Transform>(entity);
        ref var body = ref _world.Get<PhysicsBody3D>(entity);
        ref var collider = ref _world.Get<BoxCollider3D>(entity);

        // Gravity first (semi-implicit Euler: update the velocity, then move with it), clamped to the terminal fall speed.
        body.Velocity.Y += _settings.Gravity * body.GravityScale * _dt;
        body.Velocity.Y = MathF.Max(body.Velocity.Y, -_settings.MaxFallSpeed);

        // Friction acts while standing on something (contacts from the last step), slowing the horizontal motion without reversing it.
        if (body.IsGrounded && _settings.GroundFriction > 0f) ApplyFriction(ref body.Velocity);

        var box = collider.GetBounds(transform.Position);
        var moved = Vector3.Zero;
        MoveAlong(index, ref box, ref moved, ref body.Velocity.X, Axis3D.X);
        MoveAlong(index, ref box, ref moved, ref body.Velocity.Z, Axis3D.Z);
        MoveAlong(index, ref box, ref moved, ref body.Velocity.Y, Axis3D.Y);
        if (_solver.StaticObbs.Count > 0) PushBoxOutOfObbs(ref box, ref moved, ref body.Velocity);

        transform.Position += moved;
        _solver.DynamicBoxes[index] = box; // bodies stepped after this one collide with where it ended up
        body.Contacts = DetectContacts(index, box);
    }

    /// <summary>
    /// Pushes a box body out of the oriented static solids it overlaps (a few passes, because leaving one can enter another), along the
    /// separating axis with the least overlap, and removes the part of its velocity that points into what it hit so it slides along a ramp.
    /// Oriented solids are tested after the move, not swept, so a very fast body can pass through a thin one; the axis-aligned solids are
    /// swept and cannot be skipped.
    /// </summary>
    private void PushBoxOutOfObbs(ref Aabb3 box, ref Vector3 moved, ref Vector3 velocity)
    {
        for (int pass = 0; pass < ResolvePasses; pass++)
        {
            bool pushed = false;
            for (int i = 0; i < _solver.StaticObbs.Count; i++)
            {
                if (!Sat.TryPenetration(Obb3.FromAabb(box), _solver.StaticObbs[i], out var normal, out float depth)) continue;

                var push = normal * depth;
                box = box.Translated(push);
                moved += push;
                float into = Vector3.Dot(velocity, normal);
                if (into < 0f) velocity -= normal * into;
                pushed = true;
            }

            if (!pushed) return;
        }
    }

    /// <summary>Contacts with the oriented solids, found by testing the box a hair's breadth larger; the push direction says which side of the body they are on.</summary>
    private CollisionFlags3D ProbeObbContacts(in Aabb3 box)
    {
        var contacts = CollisionFlags3D.None;
        var grown = Obb3.FromAabb(box).Inflated(ContactSkin);
        for (int i = 0; i < _solver.StaticObbs.Count; i++)
        {
            if (Sat.TryPenetration(grown, _solver.StaticObbs[i], out var normal, out _)) contacts |= SideOf(normal);
        }

        return contacts;
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

    /// <summary>
    /// Sweeps the box along one axis by its velocity component and moves it. A static solid zeroes the velocity; another body is a
    /// collision, where the two share momentum (see <see cref="ShareMomentum"/>).
    /// </summary>
    private void MoveAlong(int self, ref Aabb3 box, ref Vector3 moved, ref float velocity, Axis3D axis)
    {
        float delta = velocity * _dt;
        if (delta == 0f) return;

        float allowed = _solver.Sweep(box, axis, delta, self, out var hit);
        var step = AxisMath.Along(axis, allowed);
        box = box.Translated(step);
        moved += step;
        if (!hit.Blocked) return;

        if (hit.IsBody) ShareMomentum(self, hit.DynamicIndex, axis, ref velocity);
        else velocity = 0f;
    }

    /// <summary>
    /// A perfectly inelastic collision along one axis: both bodies leave with the same velocity, the mass-weighted average of what they had,
    /// so a heavy body shoves a light one along and a light one barely slows a heavy one. Does nothing when the other body is already
    /// moving away at least as fast (it has simply not left yet), and the mover stays blocked for the step either way.
    /// </summary>
    private void ShareMomentum(int self, int other, Axis3D axis, ref float velocity)
    {
        ref var target = ref _world.Get<PhysicsBody3D>(_bodies[other]);
        float otherVelocity = AxisMath.Get(target.Velocity, axis);
        if ((velocity - otherVelocity) * MathF.Sign(velocity) <= 0f) return;

        float m1 = _world.Get<PhysicsBody3D>(_bodies[self]).EffectiveMass;
        float m2 = target.EffectiveMass;
        float shared = (m1 * velocity + m2 * otherVelocity) / (m1 + m2);
        velocity = shared;
        AxisMath.Set(ref target.Velocity, axis, shared);
    }

    /// <summary>
    /// Probes a hair's breadth in each direction so contacts stay reported while a body rests against a surface, not only on the step
    /// where it arrived (a body pushed into a wall has zero velocity and would otherwise report nothing). Other bodies count as surfaces.
    /// </summary>
    private CollisionFlags3D DetectContacts(int self, in Aabb3 box)
    {
        var contacts = CollisionFlags3D.None;
        _solver.Sweep(box, Axis3D.Y, -ContactSkin, self, out var below);
        _solver.Sweep(box, Axis3D.Y, ContactSkin, self, out var above);
        _solver.Sweep(box, Axis3D.X, -ContactSkin, self, out var minX);
        _solver.Sweep(box, Axis3D.X, ContactSkin, self, out var maxX);
        _solver.Sweep(box, Axis3D.Z, -ContactSkin, self, out var minZ);
        _solver.Sweep(box, Axis3D.Z, ContactSkin, self, out var maxZ);

        if (below.Blocked) contacts |= CollisionFlags3D.Below;
        if (above.Blocked) contacts |= CollisionFlags3D.Above;
        if (minX.Blocked) contacts |= CollisionFlags3D.MinX;
        if (maxX.Blocked) contacts |= CollisionFlags3D.MaxX;
        if (minZ.Blocked) contacts |= CollisionFlags3D.MinZ;
        if (maxZ.Blocked) contacts |= CollisionFlags3D.MaxZ;
        if (_solver.StaticObbs.Count > 0) contacts |= ProbeObbContacts(box);
        return contacts;
    }
}
