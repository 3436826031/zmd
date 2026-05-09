using System.Drawing.Text;

namespace Zmd;

internal static class FontRegistry
{
    private const string GenericMonospaceName = "monospace";
    private static readonly PrivateFontCollection PrivateFonts = new();

    static FontRegistry()
    {
        LoadPrivateFonts();
    }

    public static IEnumerable<string> FontFamilyNames()
    {
        return new[] { "JetBrains Mono", "Consolas", "Courier New", GenericMonospaceName }
            .Concat(PrivateFonts.Families
            .Select(family => family.Name)
            .Concat(FontFamily.Families.Select(family => family.Name)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name);
    }

    public static Font CreateFont(string familyName, float size)
    {
        if (familyName.Equals(GenericMonospaceName, StringComparison.OrdinalIgnoreCase))
        {
            return new Font(FontFamily.GenericMonospace, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        var privateFamily = PrivateFonts.Families.FirstOrDefault(
            family => family.Name.Equals(familyName, StringComparison.OrdinalIgnoreCase));

        if (privateFamily is not null)
        {
            return new Font(privateFamily, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        try
        {
            return new Font(familyName, size, FontStyle.Regular, GraphicsUnit.Point);
        }
        catch
        {
            return new Font(FontFamily.GenericMonospace, size, FontStyle.Regular, GraphicsUnit.Point);
        }
    }

    private static void LoadPrivateFonts()
    {
        var fontsDirectory = Path.Combine(AppContext.BaseDirectory, "fonts");
        if (!Directory.Exists(fontsDirectory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(fontsDirectory, "*.*", SearchOption.TopDirectoryOnly)
                     .Where(IsFontFile))
        {
            try
            {
                PrivateFonts.AddFontFile(path);
            }
            catch
            {
                // Ignore invalid or unsupported font files so one bad file does not block app startup.
            }
        }
    }

    private static bool IsFontFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
    }
}
