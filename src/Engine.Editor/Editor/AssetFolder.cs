namespace Engine.Editor;

/// <summary>
/// One folder of the asset tree as the browser shows it: its sub-folders and files, with names relative to the asset root (the same
/// names games load assets by, such as <c>prefabs/coin.prefab.json</c>). Built from the disk by <see cref="Scan"/>; pure, so it is unit-tested.
/// </summary>
internal sealed class AssetFolder
{
    private AssetFolder(string name, string relativePath, List<AssetFolder> folders, List<string> files)
    {
        Name = name;
        RelativePath = relativePath;
        Folders = folders;
        Files = files;
    }

    /// <summary>The folder's own name (the root's is empty).</summary>
    public string Name { get; }

    /// <summary>The folder's path relative to the asset root, with forward slashes (empty for the root).</summary>
    public string RelativePath { get; }

    /// <summary>Sub-folders, sorted by name.</summary>
    public IReadOnlyList<AssetFolder> Folders { get; }

    /// <summary>File names (not paths) in this folder, sorted.</summary>
    public IReadOnlyList<string> Files { get; }

    /// <summary>The asset name of a file in this folder, as games load it.</summary>
    /// <param name="file">A name from <see cref="Files"/>.</param>
    public string AssetName(string file) => RelativePath.Length == 0 ? file : RelativePath + "/" + file;

    /// <summary>True for a prefab file, the kind that can be dragged into the scene.</summary>
    /// <param name="file">A file name.</param>
    public static bool IsPrefab(string file) => file.EndsWith(".prefab.json", StringComparison.OrdinalIgnoreCase);

    /// <summary>Reads a folder and everything below it. A missing root gives an empty folder.</summary>
    /// <param name="root">The asset root on disk.</param>
    public static AssetFolder Scan(string root) => Read(root, string.Empty, string.Empty);

    private static AssetFolder Read(string path, string name, string relative)
    {
        var folders = new List<AssetFolder>();
        var files = new List<string>();
        if (Directory.Exists(path))
        {
            foreach (string directory in Directory.GetDirectories(path).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                string child = Path.GetFileName(directory);
                folders.Add(Read(directory, child, relative.Length == 0 ? child : relative + "/" + child));
            }

            files.AddRange(Directory.GetFiles(path).Select(f => Path.GetFileName(f)).OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
        }

        return new AssetFolder(name, relative, folders, files);
    }
}
