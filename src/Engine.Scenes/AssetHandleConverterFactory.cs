using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.Assets;

namespace Engine.Scenes;

/// <summary>Creates an <see cref="AssetHandleConverter{T}"/> for every closed <c>AssetHandle&lt;T&gt;</c> that System.Text.Json meets.</summary>
/// <param name="assets">The manager the converters resolve names with.</param>
internal sealed class AssetHandleConverterFactory(AssetManager assets) : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(AssetHandle<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var assetType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(AssetHandleConverter<>).MakeGenericType(assetType);
        return (JsonConverter)Activator.CreateInstance(converterType, assets)!;
    }
}
