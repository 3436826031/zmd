using System.Drawing.Drawing2D;

namespace Zmd;

internal static class UiTheme
{
    public static readonly Color AppBackground = Color.FromArgb(30, 32, 36);
    public static readonly Color TitleBarBackground = Color.FromArgb(35, 37, 42);
    public static readonly Color Surface = Color.FromArgb(39, 42, 48);
    public static readonly Color SurfaceRaised = Color.FromArgb(48, 52, 60);
    public static readonly Color SurfaceHover = Color.FromArgb(58, 63, 72);
    public static readonly Color TerminalBackground = Color.FromArgb(18, 19, 22);
    public static readonly Color Border = Color.FromArgb(68, 74, 84);
    public static readonly Color Text = Color.FromArgb(240, 242, 245);
    public static readonly Color MutedText = Color.FromArgb(206, 211, 218);
    public static readonly Color Accent = Color.FromArgb(72, 145, 176);
    public static readonly Color AccentSoft = Color.FromArgb(54, 84, 98);

    public const int SmallRadius = 8;
    public const int MediumRadius = 12;

    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));

        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);

        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }
}
