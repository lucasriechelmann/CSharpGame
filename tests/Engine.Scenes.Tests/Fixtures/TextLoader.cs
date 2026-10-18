using Engine.Core.Assets;

namespace Engine.Scenes.Tests.Fixtures;

/// <summary>Test loader that reads a whole text file into a <see cref="TextAsset"/>.</summary>
internal sealed class TextLoader : IAssetLoader<TextAsset>
{
    public TextAsset Load(string fullPath) => new(File.ReadAllText(fullPath));
}
