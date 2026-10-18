using Engine.Core.Assets;

namespace Engine.Scenes.Tests.Fixtures;

/// <summary>Test component that refers to an asset by handle, to check handles are saved by name.</summary>
internal struct TextRef
{
    public AssetHandle<TextAsset> Asset;
}
