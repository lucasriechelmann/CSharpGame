using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Rendering;

/// <summary>The shadow source for <see cref="ShadowCaster2D"/> entities: each becomes the four walls of its (transformed) box, listed counter-clockwise.</summary>
public sealed class BoxShadowCasterSource : IShadowCasterSource
{
    private readonly World _world;
    private readonly RenderClock? _clock;
    private readonly EntityAction<Transform, ShadowCaster2D> _collect;
    private List<ShadowEdge> _edges = null!;
    private Vector2 _min;
    private Vector2 _max;

    /// <summary>Creates the source.</summary>
    /// <param name="world">The world whose casters are read.</param>
    /// <param name="clock">Interpolation factor, so shadows follow moving entities exactly like their sprites do; null reads the simulated transforms.</param>
    public BoxShadowCasterSource(World world, RenderClock? clock = null)
    {
        _world = world;
        _clock = clock;
        _collect = Collect; // Created once so the per-light query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void CollectEdges(Vector2 min, Vector2 max, List<ShadowEdge> edges)
    {
        _edges = edges;
        _min = min;
        _max = max;
        _world.Query(_collect);
    }

    /// <summary>Appends the four walls of a box, counter-clockwise starting at the bottom-left corner.</summary>
    /// <param name="centre">Box centre.</param>
    /// <param name="halfSize">Half width and half height.</param>
    /// <param name="rotation">Rotation about Z in radians.</param>
    /// <param name="edges">The list to append to.</param>
    public static void AddBox(Vector2 centre, Vector2 halfSize, float rotation, List<ShadowEdge> edges)
    {
        var (sin, cos) = MathF.SinCos(rotation);
        Vector2 Corner(float x, float y) => centre + new Vector2(x * cos - y * sin, x * sin + y * cos);

        var bottomLeft = Corner(-halfSize.X, -halfSize.Y);
        var bottomRight = Corner(halfSize.X, -halfSize.Y);
        var topRight = Corner(halfSize.X, halfSize.Y);
        var topLeft = Corner(-halfSize.X, halfSize.Y);
        edges.Add(new ShadowEdge(bottomLeft, bottomRight));
        edges.Add(new ShadowEdge(bottomRight, topRight));
        edges.Add(new ShadowEdge(topRight, topLeft));
        edges.Add(new ShadowEdge(topLeft, bottomLeft));
    }

    private void Collect(Entity entity, ref Transform simulated, ref ShadowCaster2D caster)
    {
        var transform = TransformHierarchy.GetRenderTransform(_world, entity, simulated, _clock);
        var half = new Vector2(Math.Abs(caster.Size.X * transform.Scale.X), Math.Abs(caster.Size.Y * transform.Scale.Y)) * 0.5f;
        var centre = new Vector2(transform.Position.X, transform.Position.Y);

        // A cheap reject using the box's circumscribed circle, so far-away casters add no edges.
        float reach = half.Length();
        if (centre.X + reach < _min.X || centre.X - reach > _max.X || centre.Y + reach < _min.Y || centre.Y - reach > _max.Y) return;

        AddBox(centre, half, transform.RotationZ, _edges);
    }
}
