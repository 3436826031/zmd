using System.Drawing.Drawing2D;

namespace Zmd;

internal static class SessionIconCatalog
{
    public const string DefaultKey = "code";

    private static readonly IReadOnlyDictionary<string, string> ImageFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["cmd"] = "cmd (1).png",
        ["close"] = "icon-19-*.png",
        ["settings-image"] = "设置.png",
        ["add-image"] = "新建.png",
        ["branchbox"] = "功能-01.png",
        ["claude"] = "Claude.png",
        ["openai"] = "openai-白底.png",
        ["opencode"] = "icon-opencode.png",
        ["gemini"] = "gemini-ai.png"
    };

    public static readonly IReadOnlyList<SessionIconDefinition> All = new[]
    {
        new SessionIconDefinition("terminal", "终端"),
        new SessionIconDefinition("prompt", "提示符"),
        new SessionIconDefinition("powershell", "PowerShell"),
        new SessionIconDefinition("code", "代码"),
        new SessionIconDefinition("server", "服务"),
        new SessionIconDefinition("folder", "目录"),
        new SessionIconDefinition("spark", "灵感"),
        new SessionIconDefinition("branchbox", "分支盒"),
        new SessionIconDefinition("cmd", "cmd"),
        new SessionIconDefinition("close", "关闭"),
        new SessionIconDefinition("settings-image", "设置"),
        new SessionIconDefinition("claude", "Claude"),
        new SessionIconDefinition("openai", "OpenAI"),
        new SessionIconDefinition("opencode", "OpenCode"),
        new SessionIconDefinition("gemini", "Gemini")
    };

    public static bool Contains(string key)
    {
        return All.Any(icon => icon.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string key)
    {
        return Contains(key) ? key : DefaultKey;
    }

    public static string NameOf(string key)
    {
        return All.FirstOrDefault(icon => icon.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Name ?? "代码";
    }

    public static Bitmap CreateBitmap(string key, Color color, int size = 18)
    {
        if (TryCreateImageBitmap(key, size, out var imageBitmap))
        {
            return imageBitmap;
        }

        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        Draw(graphics, key, new Rectangle(1, 1, size - 2, size - 2), color);
        return bitmap;
    }

    public static void Draw(Graphics graphics, string key, Rectangle bounds, Color color)
    {
        key = Normalize(key);
        if (TryLoadImage(key, out var image))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(image, bounds);
            image.Dispose();
            return;
        }

        using var pen = new Pen(color, Math.Max(1.5f, bounds.Width / 11f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(color);

        var x = bounds.Left;
        var y = bounds.Top;
        var w = bounds.Width;
        var h = bounds.Height;
        var cx = x + w / 2f;
        var cy = y + h / 2f;

        switch (key)
        {
            case "prompt":
                graphics.DrawLines(pen, new[]
                {
                    new PointF(x + w * 0.20f, y + h * 0.30f),
                    new PointF(x + w * 0.45f, cy),
                    new PointF(x + w * 0.20f, y + h * 0.70f)
                });
                graphics.DrawLine(pen, x + w * 0.52f, y + h * 0.72f, x + w * 0.82f, y + h * 0.72f);
                break;

            case "powershell":
                graphics.DrawLines(pen, new[]
                {
                    new PointF(x + w * 0.25f, y + h * 0.28f),
                    new PointF(x + w * 0.52f, cy),
                    new PointF(x + w * 0.25f, y + h * 0.72f)
                });
                graphics.DrawLine(pen, x + w * 0.58f, y + h * 0.66f, x + w * 0.80f, y + h * 0.66f);
                graphics.DrawArc(pen, x + w * 0.06f, y + h * 0.10f, w * 0.88f, h * 0.80f, -18, 306);
                break;

            case "code":
                graphics.DrawLines(pen, new[]
                {
                    new PointF(x + w * 0.35f, y + h * 0.25f),
                    new PointF(x + w * 0.15f, cy),
                    new PointF(x + w * 0.35f, y + h * 0.75f)
                });
                graphics.DrawLines(pen, new[]
                {
                    new PointF(x + w * 0.65f, y + h * 0.25f),
                    new PointF(x + w * 0.85f, cy),
                    new PointF(x + w * 0.65f, y + h * 0.75f)
                });
                break;

            case "server":
                DrawRoundedRect(graphics, pen, x + w * 0.18f, y + h * 0.20f, w * 0.64f, h * 0.24f, 3);
                DrawRoundedRect(graphics, pen, x + w * 0.18f, y + h * 0.56f, w * 0.64f, h * 0.24f, 3);
                graphics.FillEllipse(brush, x + w * 0.64f, y + h * 0.29f, w * 0.06f, h * 0.06f);
                graphics.FillEllipse(brush, x + w * 0.64f, y + h * 0.65f, w * 0.06f, h * 0.06f);
                break;

            case "folder":
                using (var path = new GraphicsPath())
                {
                    path.AddLines(new[]
                    {
                        new PointF(x + w * 0.10f, y + h * 0.32f),
                        new PointF(x + w * 0.38f, y + h * 0.32f),
                        new PointF(x + w * 0.48f, y + h * 0.44f),
                        new PointF(x + w * 0.90f, y + h * 0.44f),
                        new PointF(x + w * 0.90f, y + h * 0.78f),
                        new PointF(x + w * 0.10f, y + h * 0.78f)
                    });
                    path.CloseFigure();
                    graphics.DrawPath(pen, path);
                }

                break;

            case "spark":
                graphics.DrawLine(pen, cx, y + h * 0.14f, cx, y + h * 0.86f);
                graphics.DrawLine(pen, x + w * 0.14f, cy, x + w * 0.86f, cy);
                graphics.DrawLine(pen, x + w * 0.28f, y + h * 0.28f, x + w * 0.72f, y + h * 0.72f);
                graphics.DrawLine(pen, x + w * 0.72f, y + h * 0.28f, x + w * 0.28f, y + h * 0.72f);
                break;

            case "branchbox":
                using (var branchPen = new Pen(color, Math.Max(2.0f, bounds.Width / 8.5f))
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round
                })
                {
                    var hexagon = new[]
                    {
                        new PointF(cx, y + h * 0.08f),
                        new PointF(x + w * 0.82f, y + h * 0.26f),
                        new PointF(x + w * 0.82f, y + h * 0.74f),
                        new PointF(cx, y + h * 0.92f),
                        new PointF(x + w * 0.18f, y + h * 0.74f),
                        new PointF(x + w * 0.18f, y + h * 0.26f)
                    };

                    graphics.DrawPolygon(branchPen, hexagon);
                    graphics.DrawLine(branchPen, cx, y + h * 0.28f, cx, y + h * 0.60f);
                    graphics.DrawLine(branchPen, cx, y + h * 0.60f, x + w * 0.34f, y + h * 0.73f);
                    graphics.DrawLine(branchPen, cx, y + h * 0.60f, x + w * 0.66f, y + h * 0.73f);
                }

                break;

            default:
                DrawRoundedRect(graphics, pen, x + w * 0.14f, y + h * 0.18f, w * 0.72f, h * 0.64f, 4);
                graphics.DrawLines(pen, new[]
                {
                    new PointF(x + w * 0.28f, y + h * 0.38f),
                    new PointF(x + w * 0.42f, cy),
                    new PointF(x + w * 0.28f, y + h * 0.62f)
                });
                graphics.DrawLine(pen, x + w * 0.52f, y + h * 0.62f, x + w * 0.72f, y + h * 0.62f);
                break;
        }
    }

    private static void DrawRoundedRect(Graphics graphics, Pen pen, float x, float y, float width, float height, int radius)
    {
        using var path = UiTheme.CreateRoundedRectangle(Rectangle.Round(new RectangleF(x, y, width, height)), radius);
        graphics.DrawPath(pen, path);
    }

    private static bool TryCreateImageBitmap(string key, int size, out Bitmap bitmap)
    {
        bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        if (!TryLoadImage(Normalize(key), out var image))
        {
            bitmap.Dispose();
            bitmap = null!;
            return false;
        }

        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(image, new Rectangle(1, 1, size - 2, size - 2));
        image.Dispose();
        return true;
    }

    private static bool TryLoadImage(string key, out Image image)
    {
        image = null!;
        if (!ImageFiles.TryGetValue(key, out var fileName))
        {
            return false;
        }

        var iconDirectory = Path.Combine(AppContext.BaseDirectory, "icon");
        var path = fileName.Contains('*')
            ? Directory.EnumerateFiles(iconDirectory, fileName).FirstOrDefault()
            : Path.Combine(iconDirectory, fileName);

        if (path is not null && !File.Exists(path) && key.Equals("settings-image", StringComparison.OrdinalIgnoreCase))
        {
            path = Directory.EnumerateFiles(iconDirectory, "*.png")
                .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file).StartsWith("设置", StringComparison.OrdinalIgnoreCase));
        }

        if (path is null || !File.Exists(path))
        {
            return false;
        }

        image = Image.FromFile(path);
        return true;
    }
}

internal readonly record struct SessionIconDefinition(string Key, string Name);
