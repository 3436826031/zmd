namespace Zmd;

internal sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public static readonly DarkToolStripRenderer Instance = new();

    private static readonly Color MenuBackground = Color.FromArgb(32, 32, 32);
    private static readonly Color ItemHover = Color.FromArgb(54, 84, 98);
    private static readonly Color ItemPressed = Color.FromArgb(42, 62, 70);
    private static readonly Color Border = Color.FromArgb(68, 74, 84);
    private static readonly Color CheckBackground = Color.FromArgb(48, 72, 58);
    private static readonly Color CheckMark = Color.FromArgb(120, 220, 145);

    private DarkToolStripRenderer()
        : base(new DarkColorTable())
    {
        RoundedEdges = false;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(MenuBackground);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Border);
        var bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
        bounds.Width -= 1;
        bounds.Height -= 1;
        e.Graphics.DrawRectangle(pen, bounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var color = e.Item.Pressed ? ItemPressed : e.Item.Selected ? ItemHover : MenuBackground;
        using var brush = new SolidBrush(color);
        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Color.White : Color.FromArgb(140, 146, 154);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Border);
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 6, y, e.Item.Width - 6, y);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        using var backgroundBrush = new SolidBrush(CheckBackground);
        using var checkPen = new Pen(CheckMark, 2f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };

        var bounds = e.ImageRectangle;
        bounds.Inflate(2, 2);
        e.Graphics.FillRectangle(backgroundBrush, bounds);
        e.Graphics.DrawLines(checkPen, new[]
        {
            new Point(bounds.Left + 4, bounds.Top + bounds.Height / 2),
            new Point(bounds.Left + bounds.Width / 2 - 1, bounds.Bottom - 5),
            new Point(bounds.Right - 4, bounds.Top + 4)
        });
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => MenuBackground;
        public override Color ImageMarginGradientBegin => MenuBackground;
        public override Color ImageMarginGradientMiddle => MenuBackground;
        public override Color ImageMarginGradientEnd => MenuBackground;
        public override Color MenuItemSelected => ItemHover;
        public override Color MenuItemSelectedGradientBegin => ItemHover;
        public override Color MenuItemSelectedGradientEnd => ItemHover;
        public override Color MenuItemPressedGradientBegin => ItemPressed;
        public override Color MenuItemPressedGradientMiddle => ItemPressed;
        public override Color MenuItemPressedGradientEnd => ItemPressed;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => ItemHover;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
