namespace Engine.Core.Ecs;

/// <summary>Callback for a two-component query. Components are passed by reference so they can be modified in place.</summary>
/// <typeparam name="T1">First required component type.</typeparam>
/// <typeparam name="T2">Second required component type.</typeparam>
/// <param name="entity">The entity being visited.</param>
/// <param name="c1">The entity's first component.</param>
/// <param name="c2">The entity's second component.</param>
public delegate void EntityAction<T1, T2>(Entity entity, ref T1 c1, ref T2 c2);
