namespace Zmd;

internal sealed class TerminalSettings
{
    public string ShellPath { get; set; } = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";

    public string WorkingDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string FontFamily { get; set; } = "Consolas";

    public float FontSize { get; set; } = 10.0f;

    public int InitialColumns { get; set; } = 120;

    public int InitialRows { get; set; } = 36;

    public string SideBarDock { get; set; } = "Right";

    public string StartupTerminal { get; set; } = "cmd";

    public List<AiTerminalProfile> AiProfiles { get; set; } = new();
}

internal sealed class AiTerminalProfile
{
    public string Command { get; set; } = string.Empty;

    public string Icon { get; set; } = SessionIconCatalog.DefaultKey;
}
