using System.Numerics;
using Engine.Core.Components;

namespace Engine.Physics3D;

// Stepping a capsule (or sphere) body. A capsule does not sweep a box: it moves, then is pushed out of whatever it overlaps, along the shortest
// way out, so it slides along walls and round corners. Large moves are cut into substeps no longer than half a radius, so a fast capsule cannot
// skip over a thin wall.
public sealed partial class PhysicsSystem3D
{
    /// <summary>Most substeps one move is cut into, so a pathological speed cannot stall a frame.</summary>
    private const int MaxSubsteps = 16;

    /// <summary>Most push-out passes per substep; resolving one box can push the capsule into another, so a few passes settle a corner.</summary>
    private const int ResolvePasses = 4;

    private void StepCapsule(int index)
    {
        var entity = _bodies[index];
        ref var transform = ref _world.Get<Transform>(entity);
        ref var body = ref _world.Get<PhysicsBody3D>(entity);
        ref var capsule = ref _world.Get<CapsuleCollider3D>(entity);

        body.Velocity.Y += _settings.Gravity * body.GravityScale * _dt;
        body.Velocity.Y = MathF.Max(body.Velocity.Y, -_settings.MaxFallSpeed);

        // A character stops itself (its controller accelerates towards the wanted speed, zero when idle); friction on top would fight it.
        if (body.IsGrounded && _settings.GroundFriction > 0f && !_world.Has<CharacterController3D>(entity)) ApplyFriction(ref body.Velocity);

        var motion = body.Velocity * _dt;
        float longest = MathF.Max(capsule.Radius * 0.5f, 0.05f);
        int substeps = Math.Clamp((int)MathF.Ceiling(motion.Length() / longest), 1, MaxSubsteps);
        var step = motion / substeps;

        var position = transform.Position;
        for (int i = 0; i < substeps; i++)
        {
            position += step;
            PushOut(index, ref position, ref body.Velocity, capsule);
        }

        transform.Position = position;
        _solver.DynamicBoxes[index] = capsule.GetBounds(position);
        body.Contacts = ProbeCapsuleContacts(index, position, capsule);
    }

    /// <summary>
    /// Moves the capsule out of every solid and body it overlaps (a few passes, because leaving one can enter another) and removes the part of
    /// its velocity that points into what it hit, keeping the part along the surface, which is what makes it slide.
    /// </summary>
    private void PushOut(int self, ref Vector3 position, ref Vector3 velocity, in CapsuleCollider3D capsule)
    {
        for (int pass = 0; pass < ResolvePasses; pass++)
        {
            bool moved = false;
            for (int i = 0; i < _solver.StaticBoxes.Count; i++)
            {
                if (!Overlap(position, capsule, _solver.StaticBoxes[i], capsule.Radius, out var normal, out float depth)) continue;

                position += normal * depth;
                float into = Vector3.Dot(velocity, normal);
                if (into < 0f) velocity -= normal * into;
                moved = true;
            }

            for (int i = 0; i < _solver.StaticObbs.Count; i++)
            {
                if (!OverlapObb(position, capsule, _solver.StaticObbs[i], capsule.Radius, out var normal, out float depth)) continue;

                position += normal * depth;
                float into = Vector3.Dot(velocity, normal);
                if (into < 0f) velocity -= normal * into;
                moved = true;
            }

            for (int i = 0; i < _solver.DynamicBoxes.Count; i++)
            {
                if (i == self || !Overlap(position, capsule, _solver.DynamicBoxes[i], capsule.Radius, out var normal, out float depth)) continue;

                position += normal * depth; // the capsule gives way fully; the other body is set moving, and leaves on its own step
                ShareMomentumAlong(self, i, normal, ref velocity);
                moved = true;
            }

            if (!moved) return;
        }
    }

    private static bool Overlap(Vector3 position, in CapsuleCollider3D capsule, in Aabb3 box, float radius, out Vector3 normal, out float depth)
    {
        var (bottom, top) = capsule.GetSegment(position);
        return CapsuleCollision.TryPenetration(bottom, top, radius, box, out normal, out depth);
    }

    /// <summary>
    /// A capsule against an oriented box: both are moved into the box's own space, where it is an ordinary axis-aligned box and the capsule is
    /// still a capsule (a rotation keeps distances), and the push-out direction is turned back into world space.
    /// </summary>
    private static bool OverlapObb(Vector3 position, in CapsuleCollider3D capsule, in Obb3 obb, float radius, out Vector3 normal, out float depth)
    {
        var (bottom, top) = capsule.GetSegment(position);
        if (!CapsuleCollision.TryPenetration(obb.ToLocal(bottom), obb.ToLocal(top), radius, obb.LocalBox, out var local, out depth))
        {
            normal = default;
            return false;
        }

        normal = obb.ToWorldDirection(local);
        return true;
    }
    /// <summary>
    /// The same perfectly inelastic exchange as between two boxes, but along a contact normal instead of an axis: both bodies leave with the
    /// mass-weighted average of their speeds along it. <paramref name="normal"/> points from the other body toward this one, so a speed below
    /// zero is moving into it.
    /// </summary>
    private void ShareMomentumAlong(int self, int other, Vector3 normal, ref Vector3 velocity)
    {
        ref var target = ref _world.Get<PhysicsBody3D>(_bodies[other]);
        float own = Vector3.Dot(velocity, normal);
        float theirs = Vector3.Dot(target.Velocity, normal);
        if (own - theirs >= 0f) return; // not closing

        float m1 = _world.Get<PhysicsBody3D>(_bodies[self]).EffectiveMass;
        float m2 = target.EffectiveMass;
        float shared = (m1 * own + m2 * theirs) / (m1 + m2);
        velocity += normal * (shared - own);
        target.Velocity += normal * (shared - theirs);
    }

    /// <summary>
    /// Reports what the capsule is touching by testing it a hair's breadth larger, so contacts stay reported while it rests on a surface, not
    /// only on the step where it arrived. The contact normal says which side: an obstacle below pushes the capsule up.
    /// </summary>
    private CollisionFlags3D ProbeCapsuleContacts(int self, Vector3 position, in CapsuleCollider3D capsule)
    {
        var contacts = CollisionFlags3D.None;
        float radius = capsule.Radius + ContactSkin;
        for (int i = 0; i < _solver.StaticBoxes.Count; i++)
        {
            if (Overlap(position, capsule, _solver.StaticBoxes[i], radius, out var normal, out _)) contacts |= SideOf(normal);
        }

        for (int i = 0; i < _solver.StaticObbs.Count; i++)
        {
            if (OverlapObb(position, capsule, _solver.StaticObbs[i], radius, out var normal, out _)) contacts |= SideOf(normal);
        }

        for (int i = 0; i < _solver.DynamicBoxes.Count; i++)
        {
            if (i != self && Overlap(position, capsule, _solver.DynamicBoxes[i], radius, out var normal, out _)) contacts |= SideOf(normal);
        }

        return contacts;
    }

    /// <summary>Which side of the body a contact is on, from the normal that points from the obstacle toward the body (only fairly direct contacts count).</summary>
    private static CollisionFlags3D SideOf(Vector3 normal)
    {
        var side = CollisionFlags3D.None;
        if (normal.Y > 0.5f) side |= CollisionFlags3D.Below;
        else if (normal.Y < -0.5f) side |= CollisionFlags3D.Above;
        if (normal.X > 0.5f) side |= CollisionFlags3D.MinX;
        else if (normal.X < -0.5f) side |= CollisionFlags3D.MaxX;
        if (normal.Z > 0.5f) side |= CollisionFlags3D.MinZ;
        else if (normal.Z < -0.5f) side |= CollisionFlags3D.MaxZ;
        return side;
    }
}
