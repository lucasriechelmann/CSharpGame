using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Physics3D;

/// <summary>
/// Moves every <see cref="PhysicsBody3D"/> that has a <see cref="Transform"/> and a <see cref="BoxCollider3D"/> or <see cref="CapsuleCollider3D"/>: applies gravity, then
/// moves along X, then Z, then Y, stopping at <see cref="StaticSolid3D"/> boxes and at other bodies. Horizontal first and vertical last,
/// so a body that lands finds its floor after its sideways motion is settled. Run it in the fixed-step group, after any system that
/// changes velocities.
/// </summary>
/// <remarks>
/// <para>Bodies are stepped lowest first (by the bottom of their box, ties by entity id), so a stack settles from the bottom up: the
/// body underneath has already been stopped by the floor when the one above is swept against it.</para>
/// <para>Split by topic: this file (gathering, ray queries), <c>PhysicsSystem3D.Boxes.cs</c> (stepping a box body), <c>PhysicsSystem3D.Capsules.cs</c> (stepping a capsule or sphere body).</para>
/// </remarks>
public sealed partial class PhysicsSystem3D : ISystem
{
    /// <summary>How far to look for a surface when reporting contacts; far above float noise (<see cref="CollisionSolver3D.Epsilon"/>), far below a visible gap.</summary>
    private const float ContactSkin = 0.01f;

    private readonly PhysicsSettings3D _settings;
    private readonly CollisionSolver3D _solver = new();
    private readonly List<Entity> _solidEntities = new();
    private readonly List<Entity> _obbEntities = new();
    private readonly List<Entity> _bodies = new();
    private readonly List<bool> _isCapsule = new();
    private readonly List<int> _order = new();
    private readonly BodyOrder _bodyOrder;
    private readonly EntityAction<Transform, BoxCollider3D, StaticSolid3D> _collectSolid;
    private readonly EntityAction<Transform, PhysicsBody3D, BoxCollider3D> _collectBox;
    private readonly EntityAction<Transform, PhysicsBody3D, CapsuleCollider3D> _collectCapsule;
    private float _dt;
    private World _world = null!;

    /// <summary>Creates the system.</summary>
    /// <param name="settings">Gravity and fall-speed limits; defaults are used when null.</param>
    public PhysicsSystem3D(PhysicsSettings3D? settings = null)
    {
        _settings = settings ?? new PhysicsSettings3D();
        _bodyOrder = new BodyOrder(_solver.DynamicBoxes, _bodies);
        _collectSolid = CollectSolid; // created once so the per-step queries do not allocate delegates
        _collectBox = CollectBox;
        _collectCapsule = CollectCapsule;
    }

    /// <summary>
    /// Finds the nearest solid or body a ray hits, among those gathered in the last step (so before the first step there are none).
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

        int obbBase = _solidEntities.Count;
        int bodyBase = obbBase + _obbEntities.Count;
        entity = hit.Index < obbBase ? _solidEntities[hit.Index] : hit.Index < bodyBase ? _obbEntities[hit.Index - obbBase] : _bodies[hit.Index - bodyBase];
        return true;
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _dt = (float)dt;
        _world = world;

        _solver.StaticBoxes.Clear();
        _solidEntities.Clear();
        _solver.StaticObbs.Clear();
        _obbEntities.Clear();
        _solver.DynamicBoxes.Clear();
        _bodies.Clear();
        _isCapsule.Clear();
        world.Query(_collectSolid);
        world.Query(_collectBox);
        world.Query(_collectCapsule);

        // Lowest first, so what is underneath has settled before what rests on it is swept against it.
        _order.Clear();
        for (int i = 0; i < _bodies.Count; i++) _order.Add(i);
        _order.Sort(_bodyOrder);

        foreach (int index in _order)
        {
            if (_isCapsule[index]) StepCapsule(index);
            else StepBox(index);
        }
    }

    private void CollectSolid(Entity entity, ref Transform transform, ref BoxCollider3D collider, ref StaticSolid3D solid)
    {
        if (!collider.Oriented)
        {
            _solver.StaticBoxes.Add(collider.GetBounds(TransformHierarchy.GetWorldPosition(_world, entity, transform)));
            _solidEntities.Add(entity);
            return;
        }

        // An oriented solid takes the entity's world rotation; a matrix with no rotation in it (a zero scale) counts as unrotated.
        var world = TransformHierarchy.GetWorldMatrix(_world, entity);
        var rotation = Matrix4x4.Decompose(world, out _, out var turned, out _) ? turned : Quaternion.Identity;
        _solver.StaticObbs.Add(collider.GetObb(world.Translation, rotation));
        _obbEntities.Add(entity);
    }

    private void CollectBox(Entity entity, ref Transform transform, ref PhysicsBody3D body, ref BoxCollider3D collider)
    {
        // A body's transform is moved as world space; under a parent it would be local and the box would be in the wrong place.
        if (TransformHierarchy.HasParent(_world, entity))
            throw new InvalidOperationException($"{entity} has a PhysicsBody3D and a Parent; moving bodies must be roots (static solids may be children).");

        if (collider.Oriented)
            throw new InvalidOperationException($"{entity} is a moving body with an oriented BoxCollider3D; bodies do not rotate, so only static solids can be oriented.");
        if (_world.Has<CapsuleCollider3D>(entity))
            throw new InvalidOperationException($"{entity} has both a BoxCollider3D and a CapsuleCollider3D; a body has one collision shape.");

        _solver.DynamicBoxes.Add(collider.GetBounds(transform.Position));
        _bodies.Add(entity);
        _isCapsule.Add(false);
    }

    private void CollectCapsule(Entity entity, ref Transform transform, ref PhysicsBody3D body, ref CapsuleCollider3D collider)
    {
        if (TransformHierarchy.HasParent(_world, entity))
            throw new InvalidOperationException($"{entity} has a PhysicsBody3D and a Parent; moving bodies must be roots (static solids may be children).");
        if (collider.Radius <= 0f)
            throw new InvalidOperationException($"{entity} has a CapsuleCollider3D with radius {collider.Radius}; the radius must be positive.");

        _solver.DynamicBoxes.Add(collider.GetBounds(transform.Position));
        _bodies.Add(entity);
        _isCapsule.Add(true);
    }

    /// <summary>Orders bodies by the bottom of their box, then by entity id, so the step order is the same on every run.</summary>
    private sealed class BodyOrder(List<Aabb3> boxes, List<Entity> bodies) : IComparer<int>
    {
        public int Compare(int a, int b)
        {
            int byHeight = boxes[a].Min.Y.CompareTo(boxes[b].Min.Y);
            return byHeight != 0 ? byHeight : bodies[a].Id.CompareTo(bodies[b].Id);
        }
    }
}
