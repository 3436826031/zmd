using System.Text.Json;

namespace Zmd;

internal static class TerminalSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "zmd");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static TerminalSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new TerminalSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<TerminalSettings>(json, JsonOptions) ?? new TerminalSettings();
            return Normalize(settings);
        }
        catch
        {
            return new TerminalSettings();
        }
    }

    public static void Save(TerminalSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(Normalize(settings), JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Settings persistence should never block terminal shutdown.
        }
    }

    private static TerminalSettings Normalize(TerminalSettings settings)
    {
        var defaults = new TerminalSettings();

        if (string.IsNullOrWhiteSpace(settings.ShellPath))
        {
            settings.ShellPath = defaults.ShellPath;
        }

        if (string.IsNullOrWhiteSpace(settings.WorkingDirectory))
        {
            settings.WorkingDirectory = defaults.WorkingDirectory;
        }

        if (string.IsNullOrWhiteSpace(settings.FontFamily))
        {
            settings.FontFamily = defaults.FontFamily;
        }

        settings.FontSize = Math.Clamp(settings.FontSize, 6.0f, 48.0f);
        settings.InitialColumns = Math.Max(20, settings.InitialColumns);
        settings.InitialRows = Math.Max(5, settings.InitialRows);
        settings.SideBarDock = settings.SideBarDock.Equals("Left", StringComparison.OrdinalIgnoreCase) ? "Left" : "Right";
        settings.StartupTerminal = string.IsNullOrWhiteSpace(settings.StartupTerminal)
            ? defaults.StartupTerminal
            : settings.StartupTerminal.Trim();
        settings.AiProfiles = settings.AiProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Command))
            .Select(profile => new AiTerminalProfile
            {
                Command = profile.Command.Trim(),
                Icon = SessionIconCatalog.Normalize(profile.Icon)
            })
            .ToList();

        return settings;
    }
}
