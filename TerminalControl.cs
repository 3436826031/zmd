using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Zmd;

internal sealed class TerminalControl : UserControl
{
    private readonly WebView2 webView = new();
    private readonly Queue<string> pendingOutput = new();
    private readonly FileDropTarget fileDropTarget;
    private readonly HashSet<nint> registeredDropHandles = new();
    private TerminalSettings settings;
    private int columns;
    private int rows;
    private int outputBrightness = 100;
    private bool webReady;
    private bool resizeQueued;
    private int fitVersion;

    public TerminalControl(TerminalSettings settings)
    {
        this.settings = settings;
        fileDropTarget = new FileDropTarget(InsertDroppedFiles);
        BackColor = Color.FromArgb(12, 12, 12);
        TabStop = true;
        AllowDrop = true;

        columns = settings.InitialColumns;
        rows = settings.InitialRows;

        webView.Dock = DockStyle.Fill;
        webView.AllowExternalDrop = false;
        webView.DefaultBackgroundColor = Color.FromArgb(12, 12, 12);
        Controls.Add(webView);

        webView.CoreWebView2InitializationCompleted += HandleWebViewReady;
        webView.WebMessageReceived += HandleWebMessage;
        webView.DragEnter += HandleDragEnter;
        webView.DragDrop += HandleDragDrop;
        Resize += (_, _) => QueueFit();
        VisibleChanged += (_, _) => QueueFit();
        _ = InitializeAsync();
    }

    public event EventHandler<string>? InputReceived;

    public event DragEventHandler? SessionDragDrop;

    public event EventHandler? TerminalSizeChanged;

    public int Columns => Math.Max(20, columns);

    public int Rows => Math.Max(5, rows);

    public int OutputBrightness
    {
        get => outputBrightness;
        set
        {
            outputBrightness = Math.Clamp(value, 15, 100);
            _ = ExecuteScriptAsync($"window.zmd?.setBrightness({outputBrightness});");
        }
    }

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (!webReady)
        {
            pendingOutput.Enqueue(text);
            return;
        }

        _ = WriteAsync(text);
    }

    public void ApplySettings(TerminalSettings settings)
    {
        this.settings = settings;
        if (!webReady)
        {
            return;
        }

        _ = ApplySettingsAsync();
    }

    public void Clear()
    {
        _ = ExecuteScriptAsync("window.zmd?.clear();");
    }

    public void ActivateTerminal()
    {
        if (IsDisposed)
        {
            return;
        }

        Focus();
        webView.Focus();
        QueueFit();
        _ = ExecuteScriptAsync("window.zmd?.focus();");
    }

    public void RequestFit()
    {
        QueueFit();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        webView.Focus();
        _ = ExecuteScriptAsync("window.zmd?.focus();");
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.V))
        {
            PasteClipboardText();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        NativeMethods.DragAcceptFiles(Handle, true);
        if (webView.IsHandleCreated)
        {
            NativeMethods.DragAcceptFiles(webView.Handle, true);
        }

        RegisterDropTarget(Handle);
        RegisterWebViewDropTargets();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        RevokeDropTargets();

        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WM_DROPFILES)
        {
            InsertDroppedFiles(message.WParam);
            return;
        }

        base.WndProc(ref message);
    }

    protected override void OnDragEnter(DragEventArgs drgevent)
    {
        HandleDragEnter(this, drgevent);
        base.OnDragEnter(drgevent);
    }

    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        HandleDragDrop(this, drgevent);
        base.OnDragDrop(drgevent);
    }

    private void HandleDragEnter(object? sender, DragEventArgs drgevent)
    {
        if (drgevent.Data is not null && drgevent.Data.GetDataPresent(SessionTab.DragDataFormat))
        {
            drgevent.Effect = DragDropEffects.Move;
        }
        else if (drgevent.Data is not null && drgevent.Data.GetDataPresent(DataFormats.FileDrop))
        {
            drgevent.Effect = DragDropEffects.Copy;
        }
        else
        {
            drgevent.Effect = DragDropEffects.None;
        }
    }

    private void InsertDroppedFiles(nint dropHandle)
    {
        var paths = DroppedFilePaths(dropHandle);
        NativeMethods.DragFinish(dropHandle);

        InsertDroppedFiles(paths);
    }

    private void InsertDroppedFiles(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        ActivateTerminal();
        InputReceived?.Invoke(this, string.Join(" ", paths.Select(FormatCommandPath)));
    }

    private void HandleDragDrop(object? sender, DragEventArgs drgevent)
    {
        if (drgevent.Data?.GetDataPresent(SessionTab.DragDataFormat) == true)
        {
            SessionDragDrop?.Invoke(this, drgevent);
            return;
        }

        if (drgevent.Data?.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
        {
            ActivateTerminal();
            InputReceived?.Invoke(this, string.Join(" ", paths.Select(FormatCommandPath)));
        }
    }

    private async Task InitializeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        await webView.EnsureCoreWebView2Async();
        if (IsDisposed || webView.CoreWebView2 is null)
        {
            return;
        }

        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        RegisterWebViewDropTargets();
        webView.Source = new Uri(Path.Combine(AppContext.BaseDirectory, "web", "index.html"));
    }

    private void HandleWebViewReady(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            return;
        }

        webView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            if (IsDisposed)
            {
                return;
            }

            webReady = true;
            await ApplySettingsAsync();
            await ExecuteScriptAsync($"window.zmd?.setBrightness({outputBrightness});");
            while (pendingOutput.Count > 0)
            {
                await WriteAsync(pendingOutput.Dequeue());
            }

            await FitAsync();
        };
    }

    private void HandleWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        using var document = JsonDocument.Parse(e.WebMessageAsJson);
        var root = document.RootElement;
        if (!root.TryGetProperty("type", out var typeProperty))
        {
            return;
        }

        var type = typeProperty.GetString();
        if (type == "input" && root.TryGetProperty("data", out var dataProperty))
        {
            InputReceived?.Invoke(this, dataProperty.GetString() ?? string.Empty);
            return;
        }

        if (type == "paste")
        {
            PasteClipboardText();
            return;
        }

        if (type == "copy" && root.TryGetProperty("data", out var copyProperty))
        {
            CopyTextToClipboard(copyProperty.GetString());
            return;
        }

        if (type is "resize" or "ready")
        {
            UpdateSize(root);
        }
    }

    private void PasteClipboardText()
    {
        if (!Clipboard.ContainsText())
        {
            return;
        }

        var text = Clipboard.GetText(TextDataFormat.UnicodeText);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        InputReceived?.Invoke(this, text.Replace("\r\n", "\r").Replace("\n", "\r"));
    }

    private static void CopyTextToClipboard(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        try
        {
            Clipboard.SetText(text, TextDataFormat.UnicodeText);
        }
        catch (ExternalException)
        {
        }
    }

    private async Task WriteAsync(string text)
    {
        await ExecuteScriptAsync($"window.zmd?.write({JsonSerializer.Serialize(text)});");
    }

    private async Task ApplySettingsAsync()
    {
        var script = "window.zmd?.applySettings(" + JsonSerializer.Serialize(new
        {
            fontFamily = settings.FontFamily,
            fontSize = settings.FontSize
        }) + ");";

        var result = await ExecuteScriptAsync(script);
        UpdateSizeFromScriptResult(result);
    }

    private async Task FitAsync(int version = 0)
    {
        if (!webReady)
        {
            return;
        }

        var result = await ExecuteScriptAsync("window.zmd?.fit();");
        if (version > 0 && version != fitVersion)
        {
            return;
        }

        UpdateSizeFromScriptResult(result);
    }

    private void QueueFit()
    {
        if (resizeQueued)
        {
            return;
        }

        var version = ++fitVersion;
        resizeQueued = true;
        if (!IsHandleCreated)
        {
            resizeQueued = false;
            return;
        }

        BeginInvoke(async () =>
        {
            if (IsDisposed)
            {
                return;
            }

            resizeQueued = false;
            await FitAsync(version);

            if (!IsDisposed)
            {
                await Task.Delay(50);
                await FitAsync(version);
            }
        });
    }

    private async Task<string> ExecuteScriptAsync(string script)
    {
        if (webView.CoreWebView2 is null || IsDisposed)
        {
            return string.Empty;
        }

        try
        {
            return await webView.ExecuteScriptAsync(script);
        }
        catch (ObjectDisposedException)
        {
            return string.Empty;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
    }

    private void UpdateSizeFromScriptResult(string result)
    {
        if (string.IsNullOrWhiteSpace(result) || result == "null")
        {
            return;
        }

        using var document = JsonDocument.Parse(result);
        UpdateSize(document.RootElement);
    }

    private void UpdateSize(JsonElement element)
    {
        var previousColumns = columns;
        var previousRows = rows;

        if (element.TryGetProperty("cols", out var colsProperty) && colsProperty.TryGetInt32(out var cols))
        {
            columns = Math.Max(20, cols);
        }

        if (element.TryGetProperty("rows", out var rowsProperty) && rowsProperty.TryGetInt32(out var currentRows))
        {
            rows = Math.Max(5, currentRows);
        }

        if (columns != previousColumns || rows != previousRows)
        {
            TerminalSizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string FormatCommandPath(string path)
    {
        var normalized = path.Replace("\"", "\\\"");
        return normalized.Any(char.IsWhiteSpace) ? $"\"{normalized}\"" : normalized;
    }

    internal static IReadOnlyList<string> DroppedFilePaths(nint dropHandle)
    {
        var count = NativeMethods.DragQueryFile(dropHandle, uint.MaxValue, null, 0);
        if (count == 0)
        {
            return Array.Empty<string>();
        }

        var paths = new List<string>((int)count);
        for (uint i = 0; i < count; i++)
        {
            var length = NativeMethods.DragQueryFile(dropHandle, i, null, 0);
            if (length == 0)
            {
                continue;
            }

            var buffer = new char[length + 1];
            var written = NativeMethods.DragQueryFile(dropHandle, i, buffer, (uint)buffer.Length);
            if (written > 0)
            {
                paths.Add(new string(buffer, 0, (int)written));
            }
        }

        return paths;
    }

    private void RegisterWebViewDropTargets()
    {
        if (!webView.IsHandleCreated)
        {
            return;
        }

        NativeMethods.DragAcceptFiles(webView.Handle, true);
        RegisterDropTarget(webView.Handle);
        NativeMethods.EnumChildWindows(webView.Handle, (hwnd, _) =>
        {
            NativeMethods.DragAcceptFiles(hwnd, true);
            RegisterDropTarget(hwnd);
            return true;
        }, nint.Zero);
    }

    private void RegisterDropTarget(nint handle)
    {
        if (handle == nint.Zero || registeredDropHandles.Contains(handle))
        {
            return;
        }

        var hr = NativeMethods.RegisterDragDrop(handle, fileDropTarget);
        if (hr == 0 || hr == NativeMethods.DRAGDROP_E_ALREADYREGISTERED)
        {
            registeredDropHandles.Add(handle);
        }
    }

    private void RevokeDropTargets()
    {
        foreach (var handle in registeredDropHandles)
        {
            NativeMethods.DragAcceptFiles(handle, false);
            NativeMethods.RevokeDragDrop(handle);
        }

        registeredDropHandles.Clear();
    }
}
