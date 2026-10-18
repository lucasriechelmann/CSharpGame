using System.Numerics;
using System.Reflection;
using Engine.Rendering;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

/// <summary>
/// The "Post effects" debug window: one section per effect in the chain with an enable checkbox and every public setting
/// (found by reflection, so a game's own effects show up with no extra code), edited live.
/// </summary>
internal sealed class PostEffectsPanel(PostProcessChain chain)
{
    private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        ImGui.SetNextWindowPos(new Vector2(1010f, 10f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(260f, 420f), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Post effects"))
        {
            ImGui.End();
            return;
        }

        if (chain.Effects.Count == 0) ImGui.TextDisabled("No effects in the chain.");

        for (int i = 0; i < chain.Effects.Count; i++)
        {
            var effect = chain.Effects[i];
            ImGui.PushID(i);
            if (ImGui.CollapsingHeader(effect.Name, ImGuiTreeNodeFlags.DefaultOpen))
            {
                bool enabled = effect.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled)) effect.Enabled = enabled;
                DrawSettings(effect);
            }

            ImGui.PopID();
        }

        ImGui.End();
    }

    private static void DrawSettings(IPostEffect effect)
    {
        foreach (var property in PropertiesOf(effect.GetType()))
        {
            object? value = property.GetValue(effect);
            if (!property.CanWrite)
            {
                ImGui.LabelText(property.Name, value?.ToString() ?? "null");
                continue;
            }

            if (FieldEditor.Draw(property.Name, property.PropertyType, ref value)) property.SetValue(effect, value);
        }
    }

    /// <summary>The effect's own public settings: everything except the members every effect has (name, enabled, and disposal).</summary>
    private static PropertyInfo[] PropertiesOf(Type type)
    {
        if (PropertyCache.TryGetValue(type, out var cached)) return cached;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name is not (nameof(IPostEffect.Name) or nameof(IPostEffect.Enabled)) && p.GetIndexParameters().Length == 0)
            .ToArray();
        PropertyCache[type] = properties;
        return properties;
    }
}
