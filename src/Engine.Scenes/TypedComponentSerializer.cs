using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Core.Ecs;

namespace Engine.Scenes;

/// <summary>The serializer for one concrete component type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The component struct.</typeparam>
/// <param name="name">The name the component has in JSON files.</param>
/// <param name="defaultValue">The value a newly added component starts from.</param>
internal sealed class TypedComponentSerializer<T>(string name, T defaultValue) : ComponentSerializer(name, typeof(T), defaultValue!)
{
    /// <inheritdoc />
    public override bool TryWrite(World world, Entity entity, JsonSerializerOptions options, out JsonNode? node)
    {
        if (!world.TryGet<T>(entity, out var value))
        {
            node = null;
            return false;
        }

        node = JsonSerializer.SerializeToNode(value, options);
        return true;
    }

    /// <inheritdoc />
    public override Action<World, Entity> Prepare(JsonNode node, JsonSerializerOptions options)
    {
        T value;
        try
        {
            value = node.Deserialize<T>(options)!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Component '{Name}' has invalid data: {ex.Message}", ex);
        }

        return (world, entity) => world.Set(entity, value);
    }
}
