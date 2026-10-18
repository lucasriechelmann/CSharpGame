using System.Numerics;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Rendering;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

/// <summary>
/// Draws an ImGui widget for one value of any type and reports whether the user changed it. Values travel as boxed objects, so
/// the inspector can edit components without knowing their types at compile time: the type decides the widget (a drag box for
/// floats, a checkbox for bools, a colour picker for <see cref="Color"/>, a combo for enums) and structs recurse field by field.
/// </summary>
/// <remarks>
/// Why reflection: the engine's components are plain public-field structs (the scene registry already relies on that), so one
/// generic editor serves every current and future component, including a game's own, with no per-component inspector code.
/// It allocates (boxing), which is fine for a debug panel and never used in game code paths.
/// </remarks>
internal static partial class FieldEditor
{
    /// <summary>Drag step that suits the value's size: fine for 0..1 settings such as intensities, coarse for world positions.</summary>
    private static float SpeedFor(float magnitude) => MathF.Abs(magnitude) < 5f ? 0.01f : 0.1f;

    /// <summary>Draws the editor for <paramref name="value"/>.</summary>
    /// <param name="label">Field name shown beside the widget.</param>
    /// <param name="type">Static type of the value (the box may be null for reference types such as strings).</param>
    /// <param name="value">The current value; replaced with the edited value when this returns true.</param>
    /// <returns>True when the user changed the value this frame.</returns>
    public static bool Draw(string label, Type type, ref object? value)
    {
        ImGui.PushID(label);
        bool changed = DrawValue(label, type, ref value);
        ImGui.PopID();
        return changed;
    }

    private static bool DrawValue(string label, Type type, ref object? value)
    {
        if (type == typeof(float))
        {
            float v = (float)value!;
            if (!ImGui.DragFloat(label, ref v, SpeedFor(v))) return false;
            value = v;
            return true;
        }

        if (type == typeof(int))
        {
            int v = (int)value!;
            if (!ImGui.DragInt(label, ref v)) return false;
            value = v;
            return true;
        }

        if (type == typeof(bool))
        {
            bool v = (bool)value!;
            if (!ImGui.Checkbox(label, ref v)) return false;
            value = v;
            return true;
        }

        if (type == typeof(string))
        {
            string v = (string?)value ?? string.Empty;
            if (!ImGui.InputText(label, ref v, 256)) return false;
            value = v;
            return true;
        }

        if (type == typeof(Vector2)) return DrawVector2(label, ref value);
        if (type == typeof(Vector3)) return DrawVector3(label, ref value);
        if (type == typeof(Vector4)) return DrawVector4(label, ref value);
        if (type == typeof(Quaternion)) return DrawQuaternion(label, ref value);
        if (type == typeof(Color)) return DrawColor(label, ref value);
        if (type.IsEnum) return DrawEnum(label, type, ref value);
        if (IsAssetHandle(type)) return DrawReadOnly(label, $"handle #{type.GetProperty(nameof(AssetHandle<int>.Id))!.GetValue(value)}");
        if (type.IsValueType) return DrawStruct(label, type, ref value);

        return DrawReadOnly(label, value?.ToString() ?? "null");
    }

    private static bool DrawVector2(string label, ref object? value)
    {
        var v = (Vector2)value!;
        if (!ImGui.DragFloat2(label, ref v, SpeedFor(MathF.Max(MathF.Abs(v.X), MathF.Abs(v.Y))))) return false;
        value = v;
        return true;
    }

    private static bool DrawVector3(string label, ref object? value)
    {
        var v = (Vector3)value!;
        if (!ImGui.DragFloat3(label, ref v, SpeedFor(MathF.Max(MathF.Abs(v.X), MathF.Max(MathF.Abs(v.Y), MathF.Abs(v.Z)))))) return false;
        value = v;
        return true;
    }

    private static bool DrawVector4(string label, ref object? value)
    {
        var v = (Vector4)value!;
        if (!ImGui.DragFloat4(label, ref v, SpeedFor(MathF.Max(MathF.Max(MathF.Abs(v.X), MathF.Abs(v.Y)), MathF.Max(MathF.Abs(v.Z), MathF.Abs(v.W)))))) return false;
        value = v;
        return true;
    }

    /// <summary>Quaternions are edited as Euler angles in degrees (yaw, pitch, roll) because raw x/y/z/w are not meaningful to a human.</summary>
    private static bool DrawQuaternion(string label, ref object? value)
    {
        var q = (Quaternion)value!;
        var euler = QuaternionMath.ToEulerDegrees(q);
        if (!ImGui.DragFloat3(label, ref euler, 0.5f)) return false;
        value = QuaternionMath.FromEulerDegrees(euler);
        return true;
    }

    private static bool DrawColor(string label, ref object? value)
    {
        var c = (Color)value!;
        var v = new Vector4(c.R, c.G, c.B, c.A);
        if (!ImGui.ColorEdit4(label, ref v)) return false;
        value = new Color(v.X, v.Y, v.Z, v.W);
        return true;
    }

    private static bool DrawEnum(string label, Type type, ref object? value)
    {
        string[] names = Enum.GetNames(type);
        int current = Array.IndexOf(names, value!.ToString());
        if (!ImGui.Combo(label, ref current, names, names.Length)) return false;
        value = Enum.Parse(type, names[current]);
        return true;
    }

    private static bool DrawReadOnly(string label, string text)
    {
        ImGui.LabelText(label, text);
        return false;
    }

    private static bool IsAssetHandle(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AssetHandle<>);
}
