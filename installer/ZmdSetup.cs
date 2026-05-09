using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

internal static class ZmdSetup
{
    private const string AppName = "zmd";
    private const string ResourceName = "app.zip";

    [STAThread]
    private static int Main()
    {
        try
        {
            var installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                AppName);

            Directory.CreateDirectory(installDir);
            ExtractApp(installDir);
            CreateShortcuts(installDir);
            WarnIfWebView2Missing();
            Process.Start(Path.Combine(installDir, "zmd.exe"));
            MessageBox.Show("zmd 已安装完成。", "zmd Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("安装失败：" + Environment.NewLine + ex.Message, "zmd Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static void ExtractApp(string installDir)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream(ResourceName))
        {
            if (stream == null)
            {
                throw new InvalidOperationException("安装器缺少应用资源。");
            }

            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    var targetPath = Path.GetFullPath(Path.Combine(installDir, entry.FullName));
                    if (!targetPath.StartsWith(Path.GetFullPath(installDir), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(targetPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    entry.ExtractToFile(targetPath, true);
                }
            }
        }
    }

    private static void CreateShortcuts(string installDir)
    {
        var exePath = Path.Combine(installDir, "zmd.exe");
        var startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Start Menu",
            "Programs",
            "zmd");

        Directory.CreateDirectory(startMenuDir);
        CreateShortcut(Path.Combine(startMenuDir, "zmd.lnk"), exePath, installDir);
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "zmd.lnk"), exePath, installDir);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null)
        {
            return;
        }

        dynamic shell = Activator.CreateInstance(shellType);
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.IconLocation = targetPath;
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
            "未检测到 Microsoft Edge WebView2 Runtime。若 zmd 无法显示终端界面，请安装 WebView2 Runtime。",
            "zmd Setup",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
