using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Zmd;

internal sealed class TerminalSession : IDisposable
{
    private readonly object writeLock = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly SafeFileHandle inputWrite;
    private readonly SafeFileHandle outputRead;
    private readonly SafeFileHandle processInputRead;
    private readonly SafeFileHandle processOutputWrite;
    private readonly FileStream inputWriter;
    private readonly FileStream outputReader;
    private readonly Task readTask;
    private nint pseudoConsole;
    private ProcessInformation processInformation;
    private string title;
    private bool disposed;

    public TerminalSession(TerminalSettings settings, int columns, int rows)
    {
        title = Path.GetFileNameWithoutExtension(settings.ShellPath);
        Icon = SessionIconCatalog.DefaultKey;

        if (!NativeMethods.CreatePipe(out outputRead, out processOutputWrite, nint.Zero, 0))
        {
            throw NativeMethods.LastError();
        }

        if (!NativeMethods.CreatePipe(out processInputRead, out inputWrite, nint.Zero, 0))
        {
            throw NativeMethods.LastError();
        }

        var hr = NativeMethods.CreatePseudoConsole(
            new Coord((short)Math.Max(20, columns), (short)Math.Max(5, rows)),
            processInputRead,
            processOutputWrite,
            0,
            out pseudoConsole);

        if (hr != 0)
        {
            throw new Win32Exception(hr);
        }

        StartProcess(settings);

        processInputRead.Dispose();
        processOutputWrite.Dispose();

        inputWriter = new FileStream(inputWrite, FileAccess.Write, 4096, false);
        outputReader = new FileStream(outputRead, FileAccess.Read, 4096, false);
        readTask = Task.Run(ReadOutputAsync);
    }

    public event EventHandler<string>? OutputReceived;

    public event EventHandler? Exited;

    public event EventHandler? MetadataChanged;

    public string Title
    {
        get => title;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? title : value.Trim();
            if (title == normalized)
            {
                return;
            }

            title = normalized;
            MetadataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Color TagColor { get; private set; } = Color.Transparent;

    public string Icon { get; private set; }

    public int ProcessId => processInformation.dwProcessId;

    public void SetTagColor(Color color)
    {
        if (TagColor == color)
        {
            return;
        }

        TagColor = color;
        MetadataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetIcon(string icon)
    {
        var normalized = SessionIconCatalog.Normalize(icon);

        if (Icon == normalized)
        {
            return;
        }

        Icon = normalized;
        MetadataChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Write(string text)
    {
        if (disposed || string.IsNullOrEmpty(text))
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        lock (writeLock)
        {
            inputWriter.Write(bytes, 0, bytes.Length);
            inputWriter.Flush();
        }
    }

    public void Resize(int columns, int rows)
    {
        if (disposed || pseudoConsole == nint.Zero)
        {
            return;
        }

        NativeMethods.ResizePseudoConsole(
            pseudoConsole,
            new Coord((short)Math.Max(20, columns), (short)Math.Max(5, rows)));
    }

    private void StartProcess(TerminalSettings settings)
    {
        var startupInfo = new StartupInfoEx();
        startupInfo.StartupInfo.cb = Marshal.SizeOf<StartupInfoEx>();

        nuint attributeListSize = 0;
        NativeMethods.InitializeProcThreadAttributeList(nint.Zero, 1, 0, ref attributeListSize);
        startupInfo.lpAttributeList = Marshal.AllocHGlobal((nint)attributeListSize);

        if (!NativeMethods.InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref attributeListSize))
        {
            throw NativeMethods.LastError();
        }

        if (!NativeMethods.UpdateProcThreadAttribute(
                startupInfo.lpAttributeList,
                0,
                NativeMethods.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                pseudoConsole,
                (nuint)nint.Size,
                nint.Zero,
                nint.Zero))
        {
            throw NativeMethods.LastError();
        }

        try
        {
            var commandLine = BuildCommandLine(settings.ShellPath);
            var currentDirectory = Directory.Exists(settings.WorkingDirectory)
                ? settings.WorkingDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (!NativeMethods.CreateProcess(
                    null,
                    commandLine,
                    nint.Zero,
                    nint.Zero,
                    false,
                    NativeMethods.EXTENDED_STARTUPINFO_PRESENT,
                    nint.Zero,
                    currentDirectory,
                    ref startupInfo,
                    out processInformation))
            {
                throw NativeMethods.LastError();
            }
        }
        finally
        {
            NativeMethods.DeleteProcThreadAttributeList(startupInfo.lpAttributeList);
            Marshal.FreeHGlobal(startupInfo.lpAttributeList);
        }
    }

    private async Task ReadOutputAsync()
    {
        var buffer = new byte[8192];
        var decoder = Encoding.UTF8.GetDecoder();
        var chars = new char[8192];

        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var count = await outputReader.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellation.Token);
                if (count == 0)
                {
                    break;
                }

                var charCount = decoder.GetChars(buffer, 0, count, chars, 0);
                if (charCount > 0)
                {
                    OutputReceived?.Invoke(this, new string(chars, 0, charCount));
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            Exited?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }

    private static string BuildCommandLine(string shellPath)
    {
        var command = Quote(shellPath);
        var fileName = Path.GetFileName(shellPath);
        if (fileName.Equals("cmd.exe", StringComparison.OrdinalIgnoreCase))
        {
            return $"{command} /K \"prompt $E[92m$p$E[90m$g$E[0m \"";
        }

        return command;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cancellation.Cancel();

        inputWriter.Dispose();
        outputReader.Dispose();
        inputWrite.Dispose();
        outputRead.Dispose();

        if (processInformation.hProcess != nint.Zero)
        {
            NativeMethods.TerminateProcess(processInformation.hProcess, 0);
            NativeMethods.CloseHandle(processInformation.hProcess);
            processInformation.hProcess = nint.Zero;
        }

        if (processInformation.hThread != nint.Zero)
        {
            NativeMethods.CloseHandle(processInformation.hThread);
            processInformation.hThread = nint.Zero;
        }

        if (pseudoConsole != nint.Zero)
        {
            NativeMethods.ClosePseudoConsole(pseudoConsole);
            pseudoConsole = nint.Zero;
        }

        cancellation.Dispose();
    }
}
