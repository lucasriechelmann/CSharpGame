using System.Numerics;
using System.Text;
using Engine.Rendering;
using ImGuiNET;

namespace Engine.Editor;

/// <summary>
/// The editor's "Assets" window: the game's asset folder as a tree. A prefab can be dragged from here onto the Scene panel (or double-clicked to
/// place it in the middle of the view); hovering a picture shows a thumbnail; right-clicking a file offers Rename and Delete and right-clicking a
/// folder offers New folder. The tree is read from disk when the window first opens and after each change or Refresh, not every frame.
/// </summary>
/// <param name="assetRoot">The asset folder on disk.</param>
/// <param name="thumbnail">Loads a picture by asset name for the hover preview, or null when it cannot be loaded.</param>
/// <param name="placePrefab">Places a prefab (by asset name) in the scene, for a double-click.</param>
internal sealed class AssetBrowserPanel(string assetRoot, Func<string, Texture2D?> thumbnail, Action<string> placePrefab)
{
    /// <summary>The drag-and-drop payload type for a prefab; the payload is the prefab's asset name as UTF-8 text.</summary>
    public const string PrefabPayload = "ASSET_PREFAB";

    private const float ThumbnailSize = 160f;
    private const string RenamePopup = "Rename asset";
    private const string DeletePopup = "Delete asset";
    private const string FolderPopup = "New folder";

    private AssetFolder? _root;
    private string _target = string.Empty;   // the asset or folder a popup acts on, relative to the asset root
    private string _input = string.Empty;
    private string _error = string.Empty;
    private string _openPopup = string.Empty; // popups are opened after the tree, outside its ID stack

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
        ImGui.TextDisabled("Drag a prefab onto the Scene (or double-click it). Right-click for more.");
        ImGui.Separator();

        ImGui.BeginChild("##assets-tree");
        DrawFolder(_root);
        ImGui.EndChild();

        OpenRequestedPopup();
        DrawRenamePopup();
        DrawDeletePopup();
        DrawFolderPopup();
        ImGui.End();
    }

    private void DrawFolder(AssetFolder folder)
    {
        foreach (var child in folder.Folders)
        {
            bool open = ImGui.TreeNode(child.Name);
            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.MenuItem("New folder...")) Request(FolderPopup, child.RelativePath, string.Empty);
                ImGui.EndPopup();
            }

            if (!open) continue;

            DrawFolder(child);
            ImGui.TreePop();
        }

        foreach (string file in folder.Files)
        {
            string assetName = folder.AssetName(file);
            ImGui.Selectable(file, false, ImGuiSelectableFlags.AllowDoubleClick);
            bool hovered = ImGui.IsItemHovered();
            if (hovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) && AssetFolder.IsPrefab(file)) placePrefab(assetName);
            if (hovered && AssetFileOps.IsImage(file)) DrawThumbnail(assetName);
            if (AssetFolder.IsPrefab(file)) PrefabDragSource(assetName);

            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.MenuItem("Rename...")) Request(RenamePopup, assetName, file);
                if (ImGui.MenuItem("Delete...")) Request(DeletePopup, assetName, string.Empty);
                ImGui.EndPopup();
            }
        }
    }

    private void DrawThumbnail(string assetName)
    {
        var texture = thumbnail(assetName);
        if (texture == null) return;

        float scale = Math.Min(ThumbnailSize / texture.Width, ThumbnailSize / texture.Height);
        var size = new Vector2(texture.Width * scale, texture.Height * scale);
        ImGui.BeginTooltip();
        ImGui.Image((IntPtr)texture.Handle, size);
        ImGui.TextDisabled($"{texture.Width} x {texture.Height}");
        ImGui.EndTooltip();
    }

    private static unsafe void PrefabDragSource(string assetName)
    {
        if (!ImGui.BeginDragDropSource()) return;

        byte[] payload = Encoding.UTF8.GetBytes(assetName);
        fixed (byte* data = payload) ImGui.SetDragDropPayload(PrefabPayload, (IntPtr)data, (uint)payload.Length);
        ImGui.TextUnformatted(assetName);
        ImGui.EndDragDropSource();
    }

    private void Request(string popup, string target, string input)
    {
        _openPopup = popup;
        _target = target;
        _input = input;
        _error = string.Empty;
    }

    private void OpenRequestedPopup()
    {
        if (_openPopup.Length == 0) return;

        ImGui.OpenPopup(_openPopup);
        _openPopup = string.Empty;
    }

    private void DrawRenamePopup()
    {
        if (!ImGui.BeginPopupModal(RenamePopup, ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.TextUnformatted($"Rename {_target}");
        ImGui.TextDisabled("A game that loads it by the old name will not find it any more.");
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere(); // type straight away
        ImGui.SetNextItemWidth(360f);
        bool enter = ImGui.InputText("##name", ref _input, 128, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue);
        if (_error.Length > 0) ImGui.TextColored(new Vector4(1f, 0.55f, 0.5f, 1f), _error);

        if (ImGui.Button("Rename") || enter) Apply(() => AssetFileOps.RenameFile(assetRoot, _target, _input.Trim()));
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawDeletePopup()
    {
        if (!ImGui.BeginPopupModal(DeletePopup, ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.TextUnformatted($"Delete {_target}?");
        ImGui.TextDisabled("The file is removed from the disk. This cannot be undone.");
        if (_error.Length > 0) ImGui.TextColored(new Vector4(1f, 0.55f, 0.5f, 1f), _error);

        if (ImGui.Button("Delete")) Apply(() => AssetFileOps.DeleteFile(assetRoot, _target));
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void DrawFolderPopup()
    {
        if (!ImGui.BeginPopupModal(FolderPopup, ImGuiWindowFlags.AlwaysAutoResize)) return;

        ImGui.TextUnformatted(_target.Length == 0 ? "New folder in the asset root" : $"New folder in {_target}");
        if (ImGui.IsWindowAppearing()) ImGui.SetKeyboardFocusHere(); // type straight away
        ImGui.SetNextItemWidth(360f);
        bool enter = ImGui.InputText("##folder", ref _input, 128, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue);
        if (_error.Length > 0) ImGui.TextColored(new Vector4(1f, 0.55f, 0.5f, 1f), _error);

        if (ImGui.Button("Create") || enter) Apply(() => AssetFileOps.CreateFolder(assetRoot, _target, _input.Trim()));
        ImGui.SameLine();
        if (ImGui.Button("Cancel")) ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    /// <summary>Runs a file operation; on success closes the popup and rescans, on a refused one keeps the popup open and says why.</summary>
    private void Apply(Action operation)
    {
        try
        {
            operation();
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            _error = ex.Message;
            return;
        }

        _root = AssetFolder.Scan(assetRoot);
        ImGui.CloseCurrentPopup();
    }
}
