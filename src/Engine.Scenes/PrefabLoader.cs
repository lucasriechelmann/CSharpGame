using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Core.Assets;

namespace Engine.Scenes;

/// <summary>Asset loader for <c>.prefab.json</c> files.</summary>
public sealed class PrefabLoader : IAssetLoader<Prefab>
{
    /// <inheritdoc />
    public Prefab Load(string fullPath)
    {
        try
        {
            if (JsonNode.Parse(File.ReadAllText(fullPath)) is not JsonObject template)
                throw new InvalidOperationException("A prefab file must contain a JSON object.");

            return new Prefab(template);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Prefab '{fullPath}' is not valid JSON: {ex.Message}", ex);
        }
    }
}
