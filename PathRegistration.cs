namespace Zmd;

internal static class PathRegistration
{
    public static bool AddDirectoryToUserPath(string directory)
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
        NativeMethods.SendMessageTimeout(
            NativeMethods.HWND_BROADCAST,
            NativeMethods.WM_SETTINGCHANGE,
            0,
            "Environment",
            NativeMethods.SMTO_ABORTIFHUNG,
            5000,
            out _);
    }
}
