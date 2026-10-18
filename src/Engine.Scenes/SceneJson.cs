using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.Assets;

namespace Engine.Scenes;

/// <summary>
/// The one place that decides how components look in JSON: camelCase names, public fields included (components are
/// plain structs with public fields), computed read-only properties skipped, enums as text, and asset handles as names.
/// </summary>
public static class SceneJson
{
    /// <summary>Builds the serializer options used for scene and prefab files.</summary>
    /// <param name="assets">Lets asset handles be saved and loaded by name.</param>
    public static JsonSerializerOptions CreateOptions(AssetManager assets) => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        IgnoreReadOnlyProperties = true, // computed values such as Transform.RotationZ are not data
        Converters =
        {
            new JsonStringEnumConverter(),
            new AssetHandleConverterFactory(assets),
        },
    };
}
