using System.Drawing.Drawing2D;

namespace Zmd;

internal sealed class RoundedButton : Control
{
    private int cornerRadius = UiTheme.SmallRadius;
    private Color normalBackColor = Color.Transparent;
    private Color hoverBackColor = UiTheme.SurfaceHover;
    private Color pressedBackColor = UiTheme.AccentSoft;
    private Color borderColor = Color.Transparent;
    private ButtonGlyph glyph = ButtonGlyph.None;
    private Color glyphColor = UiTheme.MutedText;
    private string? imagePath;
    private string? imageKey;
    private bool isHovered;
    private bool isPressed;
    private bool mouseDownInside;

    public RoundedButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        SetStyle(ControlStyles.StandardClick, false);

        BackColor = UiTheme.Surface;
        ForeColor = UiTheme.Text;
        TabStop = false;
        Cursor = Cursors.Hand;
    }

    public int CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Math.Max(0, value);
            Invalidate();
        }
    }

    public Color NormalBackColor
    {
        get => normalBackColor;
        set
        {
            normalBackColor = value;
            Invalidate();
        }
    }

    public Color HoverBackColor
    {
        get => hoverBackColor;
        set
        {
            hoverBackColor = value;
            Invalidate();
        }
    }

    public Color PressedBackColor
    {
        get => pressedBackColor;
        set
        {
            pressedBackColor = value;
            Invalidate();
        }
    }

    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;
            Invalidate();
        }
    }

    public ButtonGlyph Glyph
    {
        get => glyph;
        set
        {
            glyph = value;
            Invalidate();
        }
    }

    public Color GlyphColor
    {
        get => glyphColor;
        set
        {
            glyphColor = value;
            Invalidate();
        }
    }

    public string? ImagePath
    {
        get => imagePath;
        set
        {
            imagePath = value;
            Invalidate();
        }
    }

    public string? ImageKey
    {
        get => imageKey;
        set
        {
            imageKey = value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        Invalidate();
        base.OnTextChanged(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        isHovered = false;
        isPressed = false;
        mouseDownInside = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        if (mevent.Button == MouseButtons.Left)
        {
            mouseDownInside = true;
            isPressed = true;
            Capture = true;
            Invalidate();
        }

        base.OnMouseDown(mevent);
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        var shouldClick = mevent.Button == MouseButtons.Left
            && mouseDownInside
            && ClientRectangle.Contains(mevent.Location);

        isPressed = false;
        mouseDownInside = false;
        Capture = false;
        Invalidate();
        base.OnMouseUp(mevent);

        if (shouldClick)
        {
            OnClick(EventArgs.Empty);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
        var background = Parent?.BackColor ?? UiTheme.Surface;
        using var brush = new SolidBrush(background);
        pevent.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiTheme.CreateRoundedRectangle(bounds, CornerRadius);
        var currentBackColor = CurrentBackColor();
        if (currentBackColor.A > 0)
        {
            using var fillBrush = new SolidBrush(currentBackColor);
            e.Graphics.FillPath(fillBrush, path);
        }

        if (BorderColor.A > 0)
        {
            using var borderPen = new Pen(BorderColor);
            e.Graphics.DrawPath(borderPen, path);
        }

        if (TryDrawCatalogImage(e.Graphics, bounds) || TryDrawImage(e.Graphics, bounds))
        {
            return;
        }

        if (Glyph == ButtonGlyph.None)
        {
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                bounds,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            return;
        }

        DrawGlyph(e.Graphics, bounds);
    }

    private Color CurrentBackColor()
    {
        if (isPressed)
        {
            return PressedBackColor;
        }

        return isHovered ? HoverBackColor : NormalBackColor;
    }

    private bool TryDrawImage(Graphics graphics, Rectangle bounds)
    {
        if (string.IsNullOrWhiteSpace(ImagePath))
        {
            return false;
        }

        var path = Path.Combine(AppContext.BaseDirectory, ImagePath);
        if (!File.Exists(path))
        {
            return false;
        }

        using var image = Image.FromFile(path);
        var size = Math.Min(bounds.Width, bounds.Height) - 8;
        if (size <= 0)
        {
            return false;
        }

        var imageBounds = new Rectangle(
            bounds.Left + (bounds.Width - size) / 2,
            bounds.Top + (bounds.Height - size) / 2,
            size,
            size);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(image, imageBounds);
        return true;
    }

    private bool TryDrawCatalogImage(Graphics graphics, Rectangle bounds)
    {
        if (string.IsNullOrWhiteSpace(ImageKey))
        {
            return false;
        }

        var size = Math.Min(bounds.Width, bounds.Height) - 8;
        if (size <= 0)
        {
            return false;
        }

        var iconBounds = new Rectangle(
            bounds.Left + (bounds.Width - size) / 2,
            bounds.Top + (bounds.Height - size) / 2,
            size,
            size);
        SessionIconCatalog.Draw(graphics, ImageKey, iconBounds, GlyphColor);
        return true;
    }

    private void DrawGlyph(Graphics graphics, Rectangle bounds)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(GlyphColor, 1.7f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        var cx = bounds.Left + bounds.Width / 2f;
        var cy = bounds.Top + bounds.Height / 2f;

        switch (Glyph)
        {
            case ButtonGlyph.Add:
                graphics.DrawLine(pen, cx - 5, cy, cx + 5, cy);
                graphics.DrawLine(pen, cx, cy - 5, cx, cy + 5);
                break;
            case ButtonGlyph.Menu:
                using (var brush = new SolidBrush(GlyphColor))
                {
                    graphics.FillEllipse(brush, cx - 6, cy - 1, 2.2f, 2.2f);
                    graphics.FillEllipse(brush, cx - 1, cy - 1, 2.2f, 2.2f);
                    graphics.FillEllipse(brush, cx + 4, cy - 1, 2.2f, 2.2f);
                }

                break;
            case ButtonGlyph.Pin:
                graphics.DrawLine(pen, cx - 4, cy - 5, cx + 4, cy - 5);
                graphics.DrawLine(pen, cx - 1, cy - 5, cx - 4, cy + 1);
                graphics.DrawLine(pen, cx + 1, cy - 5, cx + 4, cy + 1);
                graphics.DrawLine(pen, cx - 4, cy + 1, cx + 4, cy + 1);
                graphics.DrawLine(pen, cx, cy + 1, cx, cy + 7);
                break;
            case ButtonGlyph.Settings:
                graphics.DrawEllipse(pen, cx - 5, cy - 5, 10, 10);
                graphics.DrawEllipse(pen, cx - 1.5f, cy - 1.5f, 3, 3);
                graphics.DrawLine(pen, cx, cy - 8, cx, cy - 6);
                graphics.DrawLine(pen, cx, cy + 6, cx, cy + 8);
                graphics.DrawLine(pen, cx - 8, cy, cx - 6, cy);
                graphics.DrawLine(pen, cx + 6, cy, cx + 8, cy);
                break;
            case ButtonGlyph.Close:
                graphics.DrawLine(pen, cx - 4, cy - 4, cx + 4, cy + 4);
                graphics.DrawLine(pen, cx + 4, cy - 4, cx - 4, cy + 4);
                break;
            case ButtonGlyph.CollapseRight:
                graphics.DrawLine(pen, cx - 3, cy - 6, cx + 3, cy);
                graphics.DrawLine(pen, cx + 3, cy, cx - 3, cy + 6);
                break;
            case ButtonGlyph.CollapseLeft:
                graphics.DrawLine(pen, cx + 3, cy - 6, cx - 3, cy);
                graphics.DrawLine(pen, cx - 3, cy, cx + 3, cy + 6);
                break;
            case ButtonGlyph.ExpandLeft:
                graphics.DrawLine(pen, cx + 3, cy - 6, cx - 3, cy);
                graphics.DrawLine(pen, cx - 3, cy, cx + 3, cy + 6);
                break;
            case ButtonGlyph.ExpandRight:
                graphics.DrawLine(pen, cx - 3, cy - 6, cx + 3, cy);
                graphics.DrawLine(pen, cx + 3, cy, cx - 3, cy + 6);
                break;
            case ButtonGlyph.CompactMode:
                graphics.DrawRectangle(pen, cx - 6, cy - 5, 12, 10);
                graphics.DrawLine(pen, cx - 2, cy - 1, cx - 6, cy - 5);
                graphics.DrawLine(pen, cx + 2, cy - 1, cx + 6, cy + 3);
                break;
        }
    }
}

internal enum ButtonGlyph
{
    None,
    Add,
    Menu,
    Pin,
    Settings,
    Close,
    CollapseRight,
    CollapseLeft,
    ExpandLeft,
    ExpandRight,
    CompactMode
}
