using Engine.Animation;
using Engine.Core.Components;
using Engine.Physics2D;
using Engine.Physics3D;
using Engine.Rendering;
using Engine.Rendering3D;
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
        .Register<PreviousTransform>("PreviousTransform")
        .Register<Sprite>("Sprite")
        .Register("Light2D", Light2D.Create(160f, Color.White))
        .Register<MeshRenderer>("MeshRenderer")
        .Register<ModelRenderer>("ModelRenderer")
        .Register("DirectionalLight", DirectionalLight.Create())
        .Register("PointLight3D", PointLight3D.Create(8f, Color.White))
        .Register<SpriteAnimator>("SpriteAnimator")
        .Register<TileLayerRenderer>("TileLayerRenderer")
        .Register<BoxCollider>("BoxCollider")
        .Register<StaticSolid>("StaticSolid")
        .Register<MovingPlatform>("MovingPlatform")
        .Register<PhysicsBody>("PhysicsBody")
        .Register<BoxCollider3D>("BoxCollider3D")
        .Register<StaticSolid3D>("StaticSolid3D")
        .Register<PhysicsBody3D>("PhysicsBody3D")
        .Register<CapsuleCollider3D>("CapsuleCollider3D")
        .Register("CharacterController3D", CharacterController3D.Create())
        .Register<PlatformerController>("PlatformerController")
        .Register<PlatformerIntent>("PlatformerIntent");
}
