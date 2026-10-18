using Engine.Animation;
using Engine.Core.Components;
using Engine.Physics2D;
using Engine.Rendering;
using Engine.Scenes;
using Engine.Tilemaps;

namespace Engine.Runtime;

/// <summary>
/// Registers every component the engine ships under its file name, so scenes and prefabs can use them. Runtime does this
/// because it is the one module that depends on all the others; games add their own components to the same registry.
/// </summary>
public static class EngineComponents
{
    /// <summary>Adds the engine's components to <paramref name="registry"/>.</summary>
    /// <returns>The same registry, so calls can be chained.</returns>
    public static ComponentRegistry RegisterAll(ComponentRegistry registry) => registry
        .Register<Name>("Name")
        .Register("Transform", Transform.Identity)
        .Register<Sprite>("Sprite")
        .Register<SpriteAnimator>("SpriteAnimator")
        .Register<TileLayerRenderer>("TileLayerRenderer")
        .Register<BoxCollider>("BoxCollider")
        .Register<StaticSolid>("StaticSolid")
        .Register<PhysicsBody>("PhysicsBody")
        .Register<PlatformerController>("PlatformerController")
        .Register<PlatformerIntent>("PlatformerIntent");
}
