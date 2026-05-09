using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Zmd;

internal static class NativeMethods
{
    internal static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    internal const int DWMWA_BORDER_COLOR = 34;
    internal const int DWMWA_CAPTION_COLOR = 35;
    internal const int DWMWA_TEXT_COLOR = 36;
    internal const int WM_SETTINGCHANGE = 0x001A;
    internal const int WM_DROPFILES = 0x0233;
    internal const int HWND_BROADCAST = 0xFFFF;
    internal const int SMTO_ABORTIFHUNG = 0x0002;

    internal const int GENERIC_READ = unchecked((int)0x80000000);
    internal const int GENERIC_WRITE = 0x40000000;
    internal const int FILE_SHARE_READ = 0x00000001;
    internal const int FILE_SHARE_WRITE = 0x00000002;
    internal const int OPEN_EXISTING = 3;
    internal const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
    internal const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    internal const int EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    internal const int DRAGDROP_E_ALREADYREGISTERED = unchecked((int)0x80040101);

    [DllImport("user32.dll")]
    internal static extern bool SetProcessDpiAwarenessContext(nint dpiContext);

    [DllImport("shell32.dll")]
    internal static extern void DragAcceptFiles(nint hwnd, bool accept);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint DragQueryFile(nint hDrop, uint fileIndex, char[]? fileName, uint size);

    [DllImport("shell32.dll")]
    internal static extern void DragFinish(nint hDrop);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint SendMessageTimeout(
        nint hwnd,
        int msg,
        nint wParam,
        string lParam,
        int flags,
        int timeout,
        out nint result);

    [DllImport("ole32.dll")]
    internal static extern int RegisterDragDrop(nint hwnd, IDropTarget dropTarget);

    [DllImport("ole32.dll")]
    internal static extern int RevokeDragDrop(nint hwnd);

    [DllImport("ole32.dll")]
    internal static extern void ReleaseStgMedium(ref STGMEDIUM medium);

    [DllImport("user32.dll")]
    internal static extern bool EnumChildWindows(nint parent, EnumWindowsProc callback, nint lParam);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    internal static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CreatePipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, nint pipeAttributes, int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern SafeFileHandle CreateFile(
        string fileName,
        int desiredAccess,
        int shareMode,
        nint securityAttributes,
        int creationDisposition,
        int flagsAndAttributes,
        nint templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int CreatePseudoConsole(Coord size, SafeFileHandle input, SafeFileHandle output, int flags, out nint phpc);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern int ResizePseudoConsole(nint hpc, Coord size);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern void ClosePseudoConsole(nint hpc);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool InitializeProcThreadAttributeList(nint attributeList, int attributeCount, int flags, ref nuint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool UpdateProcThreadAttribute(
        nint attributeList,
        int flags,
        nint attribute,
        nint value,
        nuint size,
        nint previousValue,
        nint returnSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern void DeleteProcThreadAttributeList(nint attributeList);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool CreateProcess(
        string? applicationName,
        string commandLine,
        nint processAttributes,
        nint threadAttributes,
        bool inheritHandles,
        int creationFlags,
        nint environment,
        string? currentDirectory,
        ref StartupInfoEx startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint WaitForSingleObject(nint handle, uint milliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool TerminateProcess(nint processHandle, uint exitCode);

    internal static Win32Exception LastError()
    {
        return new Win32Exception(Marshal.GetLastWin32Error());
    }
}

internal delegate bool EnumWindowsProc(nint hwnd, nint lParam);

[ComVisible(true)]
[Guid("00000122-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDropTarget
{
    [PreserveSig]
    int DragEnter(System.Runtime.InteropServices.ComTypes.IDataObject dataObject, int keyState, PointL point, ref int effect);

    [PreserveSig]
    int DragOver(int keyState, PointL point, ref int effect);

    [PreserveSig]
    int DragLeave();

    [PreserveSig]
    int Drop(System.Runtime.InteropServices.ComTypes.IDataObject dataObject, int keyState, PointL point, ref int effect);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PointL
{
    public readonly int X;
    public readonly int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct Coord
{
    public Coord(short x, short y)
    {
        X = x;
        Y = y;
    }

    public short X { get; }
    public short Y { get; }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct StartupInfo
{
    public int cb;
    public string? lpReserved;
    public string? lpDesktop;
    public string? lpTitle;
    public int dwX;
    public int dwY;
    public int dwXSize;
    public int dwYSize;
    public int dwXCountChars;
    public int dwYCountChars;
    public int dwFillAttribute;
    public int dwFlags;
    public short wShowWindow;
    public short cbReserved2;
    public nint lpReserved2;
    public nint hStdInput;
    public nint hStdOutput;
    public nint hStdError;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct StartupInfoEx
{
    public StartupInfo StartupInfo;
    public nint lpAttributeList;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ProcessInformation
{
    public nint hProcess;
    public nint hThread;
    public int dwProcessId;
    public int dwThreadId;
}
