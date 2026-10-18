using System.Numerics;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Components;
using Engine.Core.Ecs;
using Engine.Core.Hierarchy;

namespace Engine.Rendering;

/// <summary>
/// Draws every entity that has a <see cref="Transform"/> and a <see cref="Sprite"/>. Sprites are sorted back to
/// front by Z, then by texture (so equal-depth sprites batch together), then by discovery order.
/// </summary>
public sealed class SpriteRenderSystem : ISystem
{
    private static readonly Comparison<SpriteSubmission> BackToFront = (a, b) =>
    {
        int byDepth = a.Draw.Position.Z.CompareTo(b.Draw.Position.Z);
        if (byDepth != 0) return byDepth;

        int byTexture = a.Texture.Handle.CompareTo(b.Texture.Handle);
        return byTexture != 0 ? byTexture : a.Order.CompareTo(b.Order);
    };

    private readonly SpriteBatch _batch;
    private readonly Camera2D _camera;
    private readonly AssetManager _assets;
    private readonly RenderClock? _clock;
    private readonly List<SpriteSubmission> _submissions = new();
    private readonly EntityAction<Transform, Sprite> _collect;

    // Consecutive sprites usually share a texture, so remember the last lookup.
    private AssetHandle<Texture2D> _lastHandle;
    private Texture2D? _lastTexture;
    private World _world = null!;

    /// <summary>Creates the system.</summary>
    /// <param name="batch">The batch sprites are drawn with.</param>
    /// <param name="camera">The camera that defines what part of the world is visible.</param>
    /// <param name="assets">Resolves each sprite's texture handle.</param>
    /// <param name="clock">Interpolation factor; when given, entities with a <see cref="PreviousTransform"/> are drawn between their last two fixed steps.</param>
    public SpriteRenderSystem(SpriteBatch batch, Camera2D camera, AssetManager assets, RenderClock? clock = null)
    {
        _batch = batch;
        _camera = camera;
        _assets = assets;
        _clock = clock;
        _collect = Collect; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _world = world;
        _submissions.Clear();
        _lastHandle = AssetHandle<Texture2D>.Invalid;
        _lastTexture = null;

        world.Query(_collect);
        _submissions.Sort(BackToFront);

        _batch.Begin(_camera.GetViewProjection());
        foreach (var submission in _submissions)
            _batch.Draw(submission.Texture, submission.Draw);
        _batch.End();
    }

    /// <summary>The position on the camera's pixel grid when pixel snapping is on; Z, the draw order, is kept.</summary>
    private Vector3 SnapKeepingDepth(Vector3 position)
    {
        var xy = _camera.Snap(new Vector2(position.X, position.Y));
        return new Vector3(xy.X, xy.Y, position.Z);
    }

    private void Collect(Entity entity, ref Transform simulated, ref Sprite sprite)
    {
        if (!sprite.Texture.IsValid) return;

        var transform = TransformHierarchy.GetRenderTransform(_world, entity, simulated, _clock);

        if (sprite.Texture != _lastHandle)
        {
            _lastTexture = _assets.Get(sprite.Texture);
            _lastHandle = sprite.Texture;
        }

        var texture = _lastTexture!;
        _submissions.Add(new SpriteSubmission
        {
            Texture = texture,
            Order = _submissions.Count,
            Draw = new SpriteDraw
            {
                Position = SnapKeepingDepth(transform.Position),
                Size = new Vector2(sprite.Size.X * transform.Scale.X, sprite.Size.Y * transform.Scale.Y),
                Pivot = sprite.Pivot,
                Rotation = transform.RotationZ,
                UvRect = sprite.Source.ToUv(texture.Width, texture.Height),
                Color = sprite.Color,
                Flip = sprite.Flip,
            },
        });
    }
}
