using Engine.Core.Ecs;

namespace Engine.Rendering3D;

/// <summary>
/// The poses of the animated model instances, by entity. Shared between <see cref="ModelAnimationSystem"/>, which writes them every frame, and
/// <see cref="MeshRenderSystem"/>, which draws with them: components are plain data and cannot hold the arrays a pose needs, so the systems
/// keep them here. An entity that is not in the cache is drawn at the model's rest pose.
/// </summary>
public sealed class ModelPoses
{
    private readonly Dictionary<Entity, AnimatedPose> _poses = new();

    /// <summary>Number of poses held.</summary>
    public int Count => _poses.Count;

    /// <summary>The entity's pose, if it has been animated.</summary>
    /// <param name="entity">The animated entity.</param>
    /// <param name="pose">The pose.</param>
    public bool TryGet(Entity entity, out AnimatedPose pose) => _poses.TryGetValue(entity, out pose!);

    /// <summary>The entity's pose, creating it from the model's node tree on first use.</summary>
    /// <param name="entity">The animated entity.</param>
    /// <param name="model">The model the entity shows.</param>
    public AnimatedPose GetOrCreate(Entity entity, Model model)
    {
        if (!_poses.TryGetValue(entity, out var pose) || !pose.Matches(model.Nodes, model.Skins))
        {
            pose = new AnimatedPose(model.Nodes, model.Skins);
            _poses[entity] = pose;
        }

        return pose;
    }

    /// <summary>Forgets the poses of entities that no longer exist, so destroyed animated entities do not leak.</summary>
    /// <param name="world">The world the entities belong to.</param>
    public void Prune(World world)
    {
        List<Entity>? dead = null;
        foreach (var entity in _poses.Keys)
        {
            if (!world.IsAlive(entity)) (dead ??= new List<Entity>()).Add(entity);
        }

        if (dead == null) return;
        foreach (var entity in dead) _poses.Remove(entity);
    }

    /// <summary>Forgets one entity's pose (its model changed).</summary>
    /// <param name="entity">The entity.</param>
    public void Remove(Entity entity) => _poses.Remove(entity);
}
