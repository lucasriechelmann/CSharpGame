namespace Engine.Editor;

/// <summary>
/// File operations of the Assets window: rename, delete and new folder, on paths relative to the asset root. Every path is checked to stay inside
/// the root, and a rename never overwrites, so a typo cannot touch a file outside the project or lose another asset. Pure disk code, unit-tested.
/// </summary>
public static class AssetFileOps
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".tga"];

    /// <summary>True for a picture the browser can show as a thumbnail.</summary>
    /// <param name="file">A file name.</param>
    public static bool IsImage(string file) => ImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);

    /// <summary>Checks a single file or folder name typed by the user.</summary>
    /// <param name="name">The proposed name (no folders).</param>
    /// <param name="problem">Why it is unusable, when it is.</param>
    public static bool IsValidName(string name, out string problem)
    {
        if (string.IsNullOrWhiteSpace(name)) problem = "Type a name.";
        else if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) problem = "A name cannot contain folders or any of \\ / : * ? \" < > |.";
        else if (name is "." or "..") problem = "That is not a usable name.";
        else problem = string.Empty;
        return problem.Length == 0;
    }

    /// <summary>Renames a file in place (same folder).</summary>
    /// <param name="root">The asset root on disk.</param>
    /// <param name="relativePath">The file, relative to the root (<c>prefabs/coin.prefab.json</c>).</param>
    /// <param name="newName">The new file name, without folders.</param>
    /// <returns>The new relative path.</returns>
    /// <exception cref="ArgumentException">The name is not valid or the path leaves the root.</exception>
    /// <exception cref="IOException">The target exists already.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public static string RenameFile(string root, string relativePath, string newName)
    {
        if (!IsValidName(newName, out string problem)) throw new ArgumentException(problem, nameof(newName));

        string source = Resolve(root, relativePath);
        if (!File.Exists(source)) throw new FileNotFoundException("The file does not exist.", source);

        string target = Path.Combine(Path.GetDirectoryName(source)!, newName);
        if (File.Exists(target) || Directory.Exists(target)) throw new IOException($"'{newName}' already exists in that folder.");

        File.Move(source, target);
        string folder = (Path.GetDirectoryName(relativePath.Replace('\\', '/')) ?? string.Empty).Replace('\\', '/');
        return folder.Length == 0 ? newName : folder + "/" + newName;
    }

    /// <summary>Deletes a file.</summary>
    /// <param name="root">The asset root on disk.</param>
    /// <param name="relativePath">The file, relative to the root.</param>
    /// <exception cref="ArgumentException">The path leaves the root.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public static void DeleteFile(string root, string relativePath)
    {
        string path = Resolve(root, relativePath);
        if (!File.Exists(path)) throw new FileNotFoundException("The file does not exist.", path);

        File.Delete(path);
    }

    /// <summary>Creates a folder below <paramref name="parentRelative"/> (empty for the root).</summary>
    /// <param name="root">The asset root on disk.</param>
    /// <param name="parentRelative">The folder to create it in, relative to the root.</param>
    /// <param name="name">The new folder's name.</param>
    /// <exception cref="ArgumentException">The name is not valid or the parent leaves the root.</exception>
    /// <exception cref="IOException">Something of that name exists already.</exception>
    public static void CreateFolder(string root, string parentRelative, string name)
    {
        if (!IsValidName(name, out string problem)) throw new ArgumentException(problem, nameof(name));

        string path = Path.Combine(Resolve(root, parentRelative), name);
        if (File.Exists(path) || Directory.Exists(path)) throw new IOException($"'{name}' already exists.");

        Directory.CreateDirectory(path);
    }

    /// <summary>The full path of a relative one, refusing anything that leaves the root (<c>..</c> segments or an absolute path).</summary>
    private static string Resolve(string root, string relativePath)
    {
        string fullRoot = Path.GetFullPath(root);
        string full = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
        string inside = fullRoot.EndsWith(Path.DirectorySeparatorChar) ? fullRoot : fullRoot + Path.DirectorySeparatorChar;
        if (full != fullRoot && !full.StartsWith(inside, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"'{relativePath}' is outside the asset folder.", nameof(relativePath));

        return full;
    }
}
