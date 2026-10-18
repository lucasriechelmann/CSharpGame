using System.Reflection;
using ImGuiNET;

namespace Engine.Runtime.Inspector;

// Struct editing: public-field structs (components) and positional record structs (Rect and similar) edited member by member.
internal static partial class FieldEditor
{
    private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();
    private static readonly Dictionary<Type, RecordShape?> RecordCache = new();

    private static bool DrawStruct(string label, Type type, ref object? value)
    {
        var fields = FieldsOf(type);
        if (fields.Length > 0) return DrawFields(label, fields, ref value);

        var record = RecordShapeOf(type);
        if (record != null) return DrawRecord(label, record, ref value);

        return DrawReadOnly(label, value?.ToString() ?? "null");
    }

    /// <summary>
    /// Draws a whole component without an extra tree node around it (the inspector already shows a header per component).
    /// </summary>
    /// <returns>True when the user changed the component.</returns>
    public static bool DrawBody(Type type, ref object? value)
    {
        var fields = FieldsOf(type);
        if (fields.Length > 0) return DrawFieldRows(fields, ref value);

        var record = RecordShapeOf(type);
        if (record != null) return DrawRecordRows(record, ref value);

        return DrawReadOnly(type.Name, value?.ToString() ?? "null");
    }

    private static bool DrawFields(string label, FieldInfo[] fields, ref object? value)
    {
        if (!ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen)) return false;

        bool changed = DrawFieldRows(fields, ref value);
        ImGui.TreePop();
        return changed;
    }

    /// <summary>Edits each public instance field in place; a boxed struct is mutated through <see cref="FieldInfo.SetValue(object, object)"/>.</summary>
    private static bool DrawFieldRows(FieldInfo[] fields, ref object? value)
    {
        bool changed = false;
        foreach (var field in fields)
        {
            object? fieldValue = field.GetValue(value);
            if (field.IsInitOnly)
            {
                DrawReadOnly(field.Name, fieldValue?.ToString() ?? "null");
                continue;
            }

            if (!Draw(field.Name, field.FieldType, ref fieldValue)) continue;
            field.SetValue(value, fieldValue);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Record structs have get-only properties, so they cannot be edited in place: edit each constructor parameter's value, then
    /// rebuild the struct through the constructor.
    /// </summary>
    private static bool DrawRecord(string label, RecordShape shape, ref object? value)
    {
        if (!ImGui.TreeNodeEx(label, ImGuiTreeNodeFlags.DefaultOpen)) return false;

        bool changed = DrawRecordRows(shape, ref value);
        ImGui.TreePop();
        return changed;
    }

    private static bool DrawRecordRows(RecordShape shape, ref object? value)
    {
        var args = new object?[shape.Parameters.Length];
        bool changed = false;
        for (int i = 0; i < args.Length; i++)
        {
            args[i] = shape.Properties[i].GetValue(value);
            changed |= Draw(shape.Parameters[i].Name!, shape.Parameters[i].ParameterType, ref args[i]);
        }

        if (changed) value = shape.Constructor.Invoke(args);
        return changed;
    }

    private static FieldInfo[] FieldsOf(Type type)
    {
        if (FieldCache.TryGetValue(type, out var cached)) return cached;

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        FieldCache[type] = fields;
        return fields;
    }

    private static RecordShape? RecordShapeOf(Type type)
    {
        if (RecordCache.TryGetValue(type, out var cached)) return cached;

        RecordShape? shape = null;
        foreach (var ctor in type.GetConstructors().OrderByDescending(c => c.GetParameters().Length))
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0) continue;

            var properties = new PropertyInfo[parameters.Length];
            bool matches = true;
            for (int i = 0; i < parameters.Length && matches; i++)
            {
                properties[i] = type.GetProperty(parameters[i].Name!, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)!;
                matches = properties[i] != null && properties[i].PropertyType == parameters[i].ParameterType;
            }

            if (!matches) continue;
            shape = new RecordShape(ctor, parameters, properties);
            break;
        }

        RecordCache[type] = shape;
        return shape;
    }

    /// <summary>A positional record struct: its constructor, parameters and the properties that expose each parameter.</summary>
    private sealed record RecordShape(ConstructorInfo Constructor, ParameterInfo[] Parameters, PropertyInfo[] Properties);
}
