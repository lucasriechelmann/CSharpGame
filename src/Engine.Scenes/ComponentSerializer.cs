using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Core.Ecs;

namespace Engine.Scenes;

/// <summary>
/// Reads and writes one registered component type as JSON without the rest of the code needing to know the type.
/// The world stays generic; the typed work is done in <see cref="TypedComponentSerializer{T}"/>.
/// </summary>
/// <param name="name">The name the component has in JSON files.</param>
/// <param name="type">The component's C# type.</param>
/// <param name="defaultValue">The boxed value tools use when adding the component (<c>default</c> is invalid for some, e.g. <c>Transform</c>).</param>
internal abstract class ComponentSerializer(string name, Type type, object defaultValue)
{
    /// <summary>The boxed value tools such as the inspector start a newly added component from.</summary>
    public object DefaultValue { get; } = defaultValue;

    /// <summary>The name the component has in JSON files.</summary>
    public string Name { get; } = name;

    /// <summary>The component's C# type.</summary>
    public Type Type { get; } = type;

    /// <summary>Writes the entity's component as JSON, or returns false when the entity does not have it.</summary>
    public abstract bool TryWrite(World world, Entity entity, JsonSerializerOptions options, out JsonNode? node);

    /// <summary>
    /// Parses the JSON into a component value <b>now</b> (so bad data fails before any entity is created) and returns the
    /// action that applies it to an entity later.
    /// </summary>
    public abstract Action<World, Entity> Prepare(JsonNode node, JsonSerializerOptions options);
}
