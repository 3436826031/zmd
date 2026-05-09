namespace Zmd;

internal static class DarkToolTip
{
    public static void Configure(ToolTip toolTip, Func<Font> fontProvider)
    {
        toolTip.OwnerDraw = true;
        toolTip.ShowAlways = true;
        toolTip.UseAnimation = false;
        toolTip.UseFading = false;
        toolTip.BackColor = Color.Black;
        toolTip.ForeColor = Color.White;
        toolTip.InitialDelay = 350;
        toolTip.ReshowDelay = 80;
        toolTip.AutoPopDelay = 5000;
        toolTip.Popup += (_, e) =>
        {
            var font = fontProvider();
            var text = toolTip.GetToolTip(e.AssociatedControl);
            var textSize = TextRenderer.MeasureText(text, font);
            e.ToolTipSize = new Size(textSize.Width + 18, textSize.Height + 10);
        };
        toolTip.Draw += (_, e) =>
        {
            using var backgroundBrush = new SolidBrush(Color.Black);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            var textBounds = Rectangle.Inflate(e.Bounds, -9, -5);
            TextRenderer.DrawText(
                e.Graphics,
                e.ToolTipText,
                fontProvider(),
                textBounds,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        };
    }
}
