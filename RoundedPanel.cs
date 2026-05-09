using System.Drawing.Drawing2D;

namespace Zmd;

internal sealed class RoundedPanel : Panel
{
    public RoundedPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
    }

    public int CornerRadius { get; set; } = UiTheme.MediumRadius;

    public Color FillColor { get; set; } = UiTheme.Surface;

    public Color BorderColor { get; set; } = UiTheme.Border;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        if (Parent is not null)
        {
            using var parentBrush = new SolidBrush(Parent.BackColor);
            e.Graphics.FillRectangle(parentBrush, ClientRectangle);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiTheme.CreateRoundedRectangle(bounds, CornerRadius);
        using var fillBrush = new SolidBrush(FillColor);
        using var borderPen = new Pen(BorderColor);

        e.Graphics.FillPath(fillBrush, path);
        e.Graphics.DrawPath(borderPen, path);
        base.OnPaint(e);
    }
}
