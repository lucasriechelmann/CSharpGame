using System.Numerics;
using Engine.Core.Components;
using Engine.Core.Ecs;

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
    private readonly List<SpriteSubmission> _submissions = new();
    private readonly EntityAction<Transform, Sprite> _collect;

    /// <summary>Creates the system.</summary>
    /// <param name="batch">The batch sprites are drawn with.</param>
    /// <param name="camera">The camera that defines what part of the world is visible.</param>
    public SpriteRenderSystem(SpriteBatch batch, Camera2D camera)
    {
        _batch = batch;
        _camera = camera;
        _collect = Collect; // Created once so the per-frame query does not allocate a delegate.
    }

    /// <inheritdoc />
    public void Update(World world, double dt)
    {
        _submissions.Clear();
        world.Query(_collect);
        _submissions.Sort(BackToFront);

        _batch.Begin(_camera.GetViewProjection());
        foreach (var submission in _submissions)
            _batch.Draw(submission.Texture, submission.Draw);
        _batch.End();
    }

    private void Collect(Entity entity, ref Transform transform, ref Sprite sprite)
    {
        if (sprite.Texture is not { } texture) return;

        _submissions.Add(new SpriteSubmission
        {
            Texture = texture,
            Order = _submissions.Count,
            Draw = new SpriteDraw
            {
                Position = transform.Position,
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
