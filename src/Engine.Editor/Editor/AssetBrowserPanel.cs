using System.Text;
using ImGuiNET;

namespace Engine.Editor;

/// <summary>
/// The editor's "Assets" window: the game's asset folder as a tree. A prefab can be dragged from here onto the Scene panel to place an
/// instance there (the Scene panel is the drop target, see <c>SceneEditor.ScenePanel.cs</c>). The tree is read from disk when the window
/// first opens and again when Refresh is pressed, not every frame.
/// </summary>
internal sealed class AssetBrowserPanel(string assetRoot)
{
    /// <summary>The drag-and-drop payload type for a prefab; the payload is the prefab's asset name as UTF-8 text.</summary>
    public const string PrefabPayload = "ASSET_PREFAB";

    private AssetFolder? _root;

    /// <summary>Draws the window. Call between ImGui frame begin and render.</summary>
    public void Draw()
    {
        if (!ImGui.Begin("Assets"))
        {
            ImGui.End();
            return;
        }

        _root ??= AssetFolder.Scan(assetRoot);
        if (ImGui.Button("Refresh")) _root = AssetFolder.Scan(assetRoot);
        ImGui.SameLine();
        ImGui.TextDisabled("Drag a prefab onto the Scene to place it.");
        ImGui.Separator();

        ImGui.BeginChild("##assets-tree");
        DrawFolder(_root);
        ImGui.EndChild();
        ImGui.End();
    }

    private static void DrawFolder(AssetFolder folder)
    {
        foreach (var child in folder.Folders)
        {
            if (!ImGui.TreeNode(child.Name)) continue;

            DrawFolder(child);
            ImGui.TreePop();
        }

        foreach (string file in folder.Files)
        {
            ImGui.Selectable(file);
            if (AssetFolder.IsPrefab(file)) PrefabDragSource(folder.AssetName(file));
        }
    }

    private static unsafe void PrefabDragSource(string assetName)
    {
        if (!ImGui.BeginDragDropSource()) return;

        byte[] payload = Encoding.UTF8.GetBytes(assetName);
        fixed (byte* data = payload) ImGui.SetDragDropPayload(PrefabPayload, (IntPtr)data, (uint)payload.Length);
        ImGui.TextUnformatted(assetName);
        ImGui.EndDragDropSource();
    }
}
