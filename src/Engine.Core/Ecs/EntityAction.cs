namespace Engine.Core.Ecs;

/// <summary>Callback for a one-component query. Components are passed by reference so they can be modified in place.</summary>
/// <typeparam name="T1">First required component type.</typeparam>
/// <param name="entity">The entity being visited.</param>
/// <param name="c1">The entity's first component.</param>
public delegate void EntityAction<T1>(Entity entity, ref T1 c1);
