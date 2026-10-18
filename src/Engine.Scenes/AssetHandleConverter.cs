using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.Assets;

namespace Engine.Scenes;

/// <summary>
/// Saves an <see cref="AssetHandle{T}"/> as the asset's name (for example <c>"hero.png"</c>) and loads it back through the
/// <see cref="AssetManager"/>, so files never contain run-specific ids. An invalid handle is written as <c>null</c>.
/// </summary>
/// <typeparam name="T">The asset type the handle refers to.</typeparam>
/// <param name="assets">Resolves names to handles and handles to names.</param>
internal sealed class AssetHandleConverter<T>(AssetManager assets) : JsonConverter<AssetHandle<T>>
{
    /// <inheritdoc />
    public override AssetHandle<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return AssetHandle<T>.Invalid;
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Expected an asset name (string) for {typeof(T).Name}, found {reader.TokenType}.");

        // Load returns the cached handle for assets that already exist (including in-memory ones) and loads files otherwise.
        return assets.Load<T>(reader.GetString()!);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AssetHandle<T> value, JsonSerializerOptions options)
    {
        if (!value.IsValid) writer.WriteNullValue();
        else writer.WriteStringValue(assets.NameOf(value));
    }
}
