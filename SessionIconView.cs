namespace Zmd;

internal sealed class SessionIconView : Control
{
    private string iconKey = SessionIconCatalog.DefaultKey;

    public SessionIconView()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.FromArgb(31, 34, 39);
    }

    public string IconKey
    {
        get => iconKey;
        set
        {
            var normalized = SessionIconCatalog.Normalize(value);
            if (iconKey == normalized)
            {
                return;
            }

            iconKey = normalized;
            Invalidate();
        }
    }

    public Color IconColor { get; set; } = UiTheme.MutedText;

    protected override void OnPaint(PaintEventArgs e)
    {
        using (var backgroundBrush = new SolidBrush(BackColor))
        {
            e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
        }

        var size = Math.Min(Width, Height) - 10;
        if (size <= 0)
        {
            return;
        }

        var bounds = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);
        SessionIconCatalog.Draw(e.Graphics, iconKey, bounds, IconColor);
    }
}
