using System.Runtime.InteropServices;
using System.Text;

namespace Engine.Editor;

/// <summary>The Windows folder and file pickers, shown from the hub. Each call blocks until the user answers, and returns null when they cancel (or on a system without these dialogs).</summary>
internal static class NativeDialogs
{
    private const uint ReturnOnlyFileSystemDirectories = 0x1;
    private const uint EditBox = 0x10;
    private const uint NewDialogStyle = 0x40;
    private const int FileMustExist = 0x1000;
    private const int PathMustExist = 0x800;
    private const int MaxPath = 1024;

    /// <summary>Asks for a folder.</summary>
    /// <param name="title">The text shown above the folder tree.</param>
    public static string? PickFolder(string title) => OnDialogThread(() => ShowFolderDialog(title));

    /// <summary>Asks for an existing file.</summary>
    /// <param name="title">The dialog's title.</param>
    /// <param name="filterText">What the filter shows, such as <c>Game projects</c>.</param>
    /// <param name="pattern">The file pattern, such as <c>*.csgame.json</c>.</param>
    public static string? PickFile(string title, string filterText, string pattern) => OnDialogThread(() => ShowFileDialog(title, filterText, pattern));

    /// <summary>The folder dialog needs a single-threaded COM apartment, which the window's thread is not, so it runs on its own short-lived thread.</summary>
    private static string? OnDialogThread(Func<string?> show)
    {
        if (!OperatingSystem.IsWindows()) return null;

        string? result = null;
        var thread = new Thread(() => result = show());
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }

    private static unsafe string? ShowFolderDialog(string title)
    {
        char* display = stackalloc char[MaxPath];
        var info = new BrowseInfo { DisplayName = (nint)display, Title = title, Flags = ReturnOnlyFileSystemDirectories | EditBox | NewDialogStyle };
        nint item = SHBrowseForFolderW(ref info);
        if (item == 0) return null;

        try
        {
            var path = new StringBuilder(MaxPath);
            return SHGetPathFromIDListW(item, path) ? path.ToString() : null;
        }
        finally
        {
            CoTaskMemFree(item);
        }
    }

    private static string? ShowFileDialog(string title, string filterText, string pattern)
    {
        nint buffer = Marshal.AllocHGlobal(MaxPath * sizeof(char));
        try
        {
            Marshal.WriteInt16(buffer, 0);
            var dialog = new OpenFileName
            {
                StructSize = Marshal.SizeOf<OpenFileName>(),
                Filter = $"{filterText} ({pattern})\0{pattern}\0All files\0*.*\0\0",
                File = buffer,
                MaxFile = MaxPath,
                Title = title,
                Flags = FileMustExist | PathMustExist,
            };
            return GetOpenFileNameW(ref dialog) ? Marshal.PtrToStringUni(buffer) : null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BrowseInfo
    {
        public nint Owner;
        public nint Root;
        public nint DisplayName;
        public string Title;
        public uint Flags;
        public nint Callback;
        public nint Param;
        public int Image;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int StructSize;
        public nint Owner;
        public nint Instance;
        public string Filter;
        public string? CustomFilter;
        public int MaxCustomFilter;
        public int FilterIndex;
        public nint File;
        public int MaxFile;
        public string? FileTitle;
        public int MaxFileTitle;
        public string? InitialDir;
        public string Title;
        public int Flags;
        public short FileOffset;
        public short FileExtension;
        public string? DefaultExtension;
        public nint CustomData;
        public nint Hook;
        public string? TemplateName;
        public nint Reserved;
        public int ReservedInt;
        public int FlagsEx;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SHBrowseForFolderW(ref BrowseInfo info);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SHGetPathFromIDListW(nint item, StringBuilder path);

    [DllImport("ole32.dll")]
    private static extern void CoTaskMemFree(nint pointer);

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetOpenFileNameW(ref OpenFileName dialog);
}
