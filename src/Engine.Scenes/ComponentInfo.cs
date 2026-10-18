namespace Engine.Scenes;

/// <summary>What tools may know about a registered component without being able to name its type: the file name, the C# type, and a valid starting value.</summary>
/// <param name="Name">The name the component has in scene files.</param>
/// <param name="Type">The component's C# struct type.</param>
/// <param name="DefaultValue">A boxed value that is valid to start a new component from (tools add components with it).</param>
public sealed record ComponentInfo(string Name, Type Type, object DefaultValue);
