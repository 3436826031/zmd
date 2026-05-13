using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class ZmdSetup
{
    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var wizard = new SetupWizard();
        return wizard.ShowDialog() == DialogResult.OK ? 0 : 1;
    }
}

internal sealed class SetupWizard : Form
{
    private const string AppName = "zmd";
    private const string ResourceName = "app.zip";

    private readonly TextBox installPathInput = new();
    private readonly CheckBox desktopShortcutInput = new();
    private readonly CheckBox startMenuShortcutInput = new();
    private readonly CheckBox launchInput = new();
    private readonly Button installButton = new();
    private readonly ProgressBar progressBar = new();
    private readonly Label statusLabel = new();

    public SetupWizard()
    {
        Text = "zmd 安装向导";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(580, 370);
        BackColor = Color.White;
        ForeColor = Color.FromArgb(32, 36, 42);
        Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        var titleLabel = new Label
        {
            Text = "安装 zmd",
            Left = 26,
            Top = 24,
            Width = 430,
            Height = 38,
            Font = new Font("Segoe UI", 18.0f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(20, 24, 30)
        };
        var descriptionLabel = CreateLabel("选择安装位置和快捷方式选项，然后点击安装。", 28, 68, 510);
        var pathLabel = CreateLabel("安装目录", 28, 112, 500);

        installPathInput.Left = 28;
        installPathInput.Top = 138;
        installPathInput.Width = 420;
        installPathInput.Height = 26;
        installPathInput.Text = DefaultInstallDirectory();
        installPathInput.BackColor = Color.White;
        installPathInput.ForeColor = Color.FromArgb(32, 36, 42);
        installPathInput.BorderStyle = BorderStyle.FixedSingle;

        var browseButton = new Button
        {
            Text = "浏览...",
            Left = 460,
            Top = 136,
            Width = 88,
            Height = 30
        };
        ConfigureButton(browseButton, primary: false);
        browseButton.Click += (_, _) => BrowseInstallPath();

        desktopShortcutInput.Text = "创建桌面快捷方式";
        desktopShortcutInput.Left = 28;
        desktopShortcutInput.Top = 186;
        desktopShortcutInput.Width = 220;
        desktopShortcutInput.Checked = true;
        desktopShortcutInput.ForeColor = Color.FromArgb(32, 36, 42);

        startMenuShortcutInput.Text = "创建开始菜单快捷方式";
        startMenuShortcutInput.Left = 28;
        startMenuShortcutInput.Top = 216;
        startMenuShortcutInput.Width = 240;
        startMenuShortcutInput.Checked = true;
        startMenuShortcutInput.ForeColor = Color.FromArgb(32, 36, 42);

        launchInput.Text = "安装完成后启动 zmd";
        launchInput.Left = 28;
        launchInput.Top = 246;
        launchInput.Width = 220;
        launchInput.Checked = true;
        launchInput.ForeColor = Color.FromArgb(32, 36, 42);

        progressBar.Left = 28;
        progressBar.Top = 286;
        progressBar.Width = 520;
        progressBar.Height = 12;
        progressBar.Style = ProgressBarStyle.Continuous;

        statusLabel.Left = 28;
        statusLabel.Top = 306;
        statusLabel.Width = 330;
        statusLabel.Height = 24;
        statusLabel.ForeColor = Color.FromArgb(92, 99, 112);
        statusLabel.Text = "准备安装";

        var cancelButton = new Button
        {
            Text = "取消",
            Left = 374,
            Top = 326,
            Width = 78,
            Height = 30,
            DialogResult = DialogResult.Cancel
        };
        ConfigureButton(cancelButton, primary: false);

        installButton.Text = "安装";
        installButton.Left = 470;
        installButton.Top = 326;
        installButton.Width = 78;
        installButton.Height = 30;
        ConfigureButton(installButton, primary: true);
        installButton.Click += (_, _) => Install();

        Controls.Add(titleLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(pathLabel);
        Controls.Add(installPathInput);
        Controls.Add(browseButton);
        Controls.Add(desktopShortcutInput);
        Controls.Add(startMenuShortcutInput);
        Controls.Add(launchInput);
        Controls.Add(progressBar);
        Controls.Add(statusLabel);
        Controls.Add(cancelButton);
        Controls.Add(installButton);
    }

    private void BrowseInstallPath()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 zmd 安装目录",
            SelectedPath = installPathInput.Text,
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            installPathInput.Text = dialog.SelectedPath;
        }
    }

    private void Install()
    {
        var installDir = installPathInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(installDir))
        {
            MessageBox.Show(this, "请选择安装目录。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            installButton.Enabled = false;
            progressBar.Value = 10;
            statusLabel.Text = "正在解压文件...";
            Directory.CreateDirectory(installDir);
            ExtractApp(installDir);
            TryAddDirectoryToUserPath(installDir);

            progressBar.Value = 70;
            statusLabel.Text = "正在创建快捷方式...";
            if (startMenuShortcutInput.Checked)
            {
                CreateStartMenuShortcut(installDir);
            }

            if (desktopShortcutInput.Checked)
            {
                CreateDesktopShortcut(installDir);
            }

            progressBar.Value = 90;
            WarnIfWebView2Missing();

            progressBar.Value = 100;
            statusLabel.Text = "安装完成";
            if (launchInput.Checked)
            {
                Process.Start(new ProcessStartInfo(Path.Combine(installDir, "zmd.exe"))
                {
                    WorkingDirectory = installDir,
                    UseShellExecute = true
                });
            }

            MessageBox.Show(this, "zmd 已安装完成。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            installButton.Enabled = true;
            MessageBox.Show(this, "安装失败：" + Environment.NewLine + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ExtractApp(string installDir)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException("安装器缺少应用资源。");
        }

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var installRoot = Path.GetFullPath(installDir);
        foreach (var entry in archive.Entries)
        {
            var targetPath = Path.GetFullPath(Path.Combine(installRoot, entry.FullName));
            if (!targetPath.StartsWith(installRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            entry.ExtractToFile(targetPath, true);
        }
    }

    private static void CreateStartMenuShortcut(string installDir)
    {
        var startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Start Menu",
            "Programs",
            "zmd");
        Directory.CreateDirectory(startMenuDir);
        CreateShortcut(Path.Combine(startMenuDir, "zmd.lnk"), installDir);
    }

    private static void CreateDesktopShortcut(string installDir)
    {
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "zmd.lnk"), installDir);
    }

    private static void CreateShortcut(string shortcutPath, string installDir)
    {
        var exePath = Path.Combine(installDir, "zmd.exe");
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = exePath;
        shortcut.WorkingDirectory = installDir;
        shortcut.IconLocation = exePath;
        shortcut.Save();
    }

    private static void WarnIfWebView2Missing()
    {
        if (Directory.Exists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft",
                "EdgeWebView",
                "Application")))
        {
            return;
        }

        MessageBox.Show(
            "未检测到 Microsoft Edge WebView2 Runtime。如果 zmd 无法显示终端界面，请安装 WebView2 Runtime。",
            "zmd 安装提示",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private static void TryAddDirectoryToUserPath(string directory)
    {
        try
        {
            AddDirectoryToUserPath(directory);
        }
        catch
        {
            // The app retries this on first launch, so installation stays usable.
        }
    }

    private static bool AddDirectoryToUserPath(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        var normalizedDirectory = NormalizePathEntry(directory);
        var userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? string.Empty;
        var entries = SplitPathEntries(userPath).ToList();
        if (entries.Any(entry => PathsEqual(entry, normalizedDirectory)))
        {
            Environment.SetEnvironmentVariable(
                "Path",
                MergeProcessPath(Environment.GetEnvironmentVariable("Path"), normalizedDirectory),
                EnvironmentVariableTarget.Process);
            return false;
        }

        entries.Add(normalizedDirectory);
        Environment.SetEnvironmentVariable("Path", string.Join(";", entries), EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable(
            "Path",
            MergeProcessPath(Environment.GetEnvironmentVariable("Path"), normalizedDirectory),
            EnvironmentVariableTarget.Process);
        BroadcastEnvironmentChanged();
        return true;
    }

    private static IEnumerable<string> SplitPathEntries(string value)
    {
        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(entry => !string.IsNullOrWhiteSpace(entry));
    }

    private static string MergeProcessPath(string? currentPath, string newEntry)
    {
        var entries = SplitPathEntries(currentPath ?? string.Empty).ToList();
        if (!entries.Any(entry => PathsEqual(entry, newEntry)))
        {
            entries.Add(newEntry);
        }

        return string.Join(";", entries);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePathEntry(left), NormalizePathEntry(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePathEntry(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return Path.GetFullPath(expanded).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void BroadcastEnvironmentChanged()
    {
        SendMessageTimeout(
            HWND_BROADCAST,
            WM_SETTINGCHANGE,
            nint.Zero,
            "Environment",
            SMTO_ABORTIFHUNG,
            5000,
            out _);
    }

    private const int WM_SETTINGCHANGE = 0x001A;
    private const int HWND_BROADCAST = 0xFFFF;
    private const int SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint SendMessageTimeout(
        nint hwnd,
        int msg,
        nint wParam,
        string lParam,
        int flags,
        int timeout,
        out nint result);

    private static string DefaultInstallDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            AppName);
    }

    private static Label CreateLabel(string text, int left, int top, int width)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 24,
            ForeColor = Color.FromArgb(66, 73, 84),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static void ConfigureButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = primary ? Color.FromArgb(20, 184, 112) : Color.FromArgb(190, 196, 205);
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(12, 166, 100) : Color.FromArgb(241, 244, 248);
        button.BackColor = primary ? Color.FromArgb(20, 184, 112) : Color.White;
        button.ForeColor = primary ? Color.White : Color.FromArgb(32, 36, 42);
    }
}
