namespace Zmd;

internal sealed class SessionTab : UserControl
{
    internal const string DragDataFormat = "Zmd.TerminalSession";

    private static readonly Color DefaultFill = Color.FromArgb(31, 34, 39);
    private static readonly Color HoverFill = Color.FromArgb(39, 44, 48);
    private static readonly Color SelectedFill = Color.FromArgb(48, 72, 58);
    private static readonly Color SelectedBar = Color.FromArgb(82, 196, 112);

    private readonly SessionIconView iconView = new();
    private readonly Label titleLabel = new();
    private readonly RoundedButton closeButton = new();
    private readonly ToolTip toolTip = new();
    private TextBox? renameBox;
    private bool hovered;
    private bool selected;
    private bool dragging;
    private bool dragArmed;
    private bool compact;
    private Point dragStartScreen;

    public SessionTab(TerminalSession session)
    {
        Session = session;
        Height = 34;
        Dock = DockStyle.Top;
        Cursor = Cursors.Hand;
        BackColor = Color.FromArgb(18, 19, 22);
        Padding = new Padding(8, 3, 4, 3);
        Margin = new Padding(0, 0, 0, 3);

        iconView.IconKey = session.Icon;
        iconView.Dock = DockStyle.Left;
        iconView.Width = 26;
        iconView.IconColor = UiTheme.MutedText;

        titleLabel.Text = session.Title;
        titleLabel.AutoEllipsis = true;
        titleLabel.Dock = DockStyle.Fill;
        titleLabel.ForeColor = UiTheme.MutedText;
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        titleLabel.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        titleLabel.BackColor = DefaultFill;

        closeButton.Dock = DockStyle.Right;
        closeButton.Width = 20;
        closeButton.Text = "x";
        closeButton.Glyph = ButtonGlyph.None;
        closeButton.ImageKey = "close";
        closeButton.NormalBackColor = DefaultFill;
        closeButton.HoverBackColor = Color.FromArgb(62, 48, 50);
        closeButton.PressedBackColor = Color.FromArgb(84, 52, 56);
        closeButton.BorderColor = DefaultFill;
        closeButton.ForeColor = Color.FromArgb(170, 176, 184);
        closeButton.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        closeButton.CornerRadius = 0;
        closeButton.TabStop = false;
        ConfigureToolTip();
        UpdateToolTip();

        Controls.Add(titleLabel);
        Controls.Add(iconView);
        Controls.Add(closeButton);

        session.MetadataChanged += HandleSessionMetadataChanged;
        MouseEnter += (_, _) => SetHovered(true);
        MouseLeave += (_, _) => SetHovered(false);
        iconView.MouseEnter += (_, _) => SetHovered(true);
        iconView.MouseLeave += (_, _) => SetHovered(false);
        titleLabel.MouseEnter += (_, _) => SetHovered(true);
        titleLabel.MouseLeave += (_, _) => SetHovered(false);
        closeButton.MouseEnter += (_, _) => SetHovered(true);

        MouseDown += BeginDragCapture;
        MouseMove += StartDragIfNeeded;
        MouseUp += CompleteDragIfNeeded;
        iconView.MouseDown += BeginDragCapture;
        iconView.MouseMove += StartDragIfNeeded;
        iconView.MouseUp += CompleteDragIfNeeded;
        titleLabel.MouseDown += BeginDragCapture;
        titleLabel.MouseMove += StartDragIfNeeded;
        titleLabel.MouseUp += CompleteDragIfNeeded;
        Click += (_, _) => ActivateRequested?.Invoke(this, EventArgs.Empty);
        iconView.Click += (_, _) => ActivateRequested?.Invoke(this, EventArgs.Empty);
        titleLabel.Click += (_, _) => ActivateRequested?.Invoke(this, EventArgs.Empty);
        closeButton.Click += (_, e) => CloseRequested?.Invoke(this, e);
    }

    public event EventHandler? ActivateRequested;

    public event EventHandler? CloseRequested;

    public event EventHandler? RenameCommitted;

    public event EventHandler<MouseEventArgs>? MenuRequested;

    public event EventHandler<SessionTabDragEventArgs>? DragCompleted;

    public TerminalSession Session { get; }

    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;
            titleLabel.ForeColor = selected ? Color.FromArgb(234, 248, 238) : UiTheme.MutedText;
            iconView.IconColor = selected ? Color.FromArgb(210, 242, 220) : Color.FromArgb(168, 176, 184);
            iconView.Invalidate();
            closeButton.ForeColor = selected ? Color.FromArgb(210, 238, 218) : Color.FromArgb(150, 158, 166);
            Invalidate();
        }
    }

    public bool Compact
    {
        get => compact;
        set
        {
            if (compact == value)
            {
                return;
            }

            compact = value;
            titleLabel.Visible = !compact && renameBox is null;
            closeButton.Visible = !compact;
            Padding = compact ? new Padding(4, 3, 4, 3) : new Padding(8, 3, 4, 3);
            iconView.Width = compact ? 28 : 26;
            Invalidate();
        }
    }

    public void BeginRename()
    {
        if (renameBox is not null)
        {
            renameBox.Focus();
            renameBox.SelectAll();
            return;
        }

        titleLabel.Visible = false;
        compact = false;
        closeButton.Visible = true;
        Padding = new Padding(8, 3, 4, 3);
        renameBox = new TextBox
        {
            Text = Session.Title,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(24, 26, 30),
            ForeColor = UiTheme.Text,
            Font = titleLabel.Font,
            Dock = DockStyle.Fill
        };

        renameBox.KeyDown += HandleRenameKeyDown;
        renameBox.Leave += (_, _) => CommitRename();
        Controls.Add(renameBox);
        renameBox.BringToFront();
        closeButton.BringToFront();
        renameBox.Focus();
        renameBox.SelectAll();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var fillColor = CurrentFillColor();
        titleLabel.BackColor = fillColor;
        iconView.BackColor = fillColor;
        closeButton.NormalBackColor = fillColor;
        closeButton.BorderColor = fillColor;
        using var fillBrush = new SolidBrush(fillColor);

        e.Graphics.FillRectangle(fillBrush, ClientRectangle);
        DrawTag(e.Graphics);
        base.OnPaint(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Session.MetadataChanged -= HandleSessionMetadataChanged;
            toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private Color CurrentFillColor()
    {
        if (selected)
        {
            return SelectedFill;
        }

        return hovered ? HoverFill : DefaultFill;
    }

    private void SetHovered(bool value)
    {
        hovered = value;
        Invalidate();
    }

    private void DrawTag(Graphics graphics)
    {
        if (selected)
        {
            using var selectedBrush = new SolidBrush(SelectedBar);
            graphics.FillRectangle(selectedBrush, 0, 0, 4, Height);
            return;
        }

        if (Session.TagColor.A == 0)
        {
            return;
        }

        var bounds = new Rectangle(0, 7, 3, Height - 14);
        using var brush = new SolidBrush(Session.TagColor);
        graphics.FillRectangle(brush, bounds);
    }

    private void HandleSessionMetadataChanged(object? sender, EventArgs e)
    {
        titleLabel.Text = Session.Title;
        iconView.IconKey = Session.Icon;
        UpdateToolTip();
        Invalidate();
    }

    private void ConfigureToolTip()
    {
        DarkToolTip.Configure(toolTip, () => Font);
    }

    private void UpdateToolTip()
    {
        var switchText = $"切换到终端：{Session.Title}";
        toolTip.SetToolTip(this, switchText);
        toolTip.SetToolTip(iconView, switchText);
        toolTip.SetToolTip(titleLabel, switchText);
        toolTip.SetToolTip(closeButton, "关闭终端");
    }

    private void HandleRenameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            CommitRename();
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            CancelRename();
            e.SuppressKeyPress = true;
        }
    }

    private void CommitRename()
    {
        if (renameBox is null)
        {
            return;
        }

        var value = renameBox.Text;
        EndRename();
        Session.Title = value;
        RenameCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void CancelRename()
    {
        if (renameBox is null)
        {
            return;
        }

        EndRename();
    }

    private void EndRename()
    {
        if (renameBox is null)
        {
            return;
        }

        var box = renameBox;
        renameBox = null;
        box.KeyDown -= HandleRenameKeyDown;
        Controls.Remove(box);
        box.Dispose();
        titleLabel.Visible = !compact;
        closeButton.Visible = !compact;
    }

    private void BeginDragCapture(object? sender, MouseEventArgs e)
    {
        if (renameBox is not null)
        {
            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            MenuRequested?.Invoke(this, e);
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            dragArmed = true;
            dragging = false;
            dragStartScreen = Cursor.Position;
            Capture = true;
            iconView.Capture = true;
            titleLabel.Capture = true;
        }
    }

    private void StartDragIfNeeded(object? sender, MouseEventArgs e)
    {
        if (renameBox is not null)
        {
            return;
        }

        if (!dragArmed || e.Button != MouseButtons.Left)
        {
            return;
        }

        var currentPosition = Cursor.Position;
        var dragBounds = new Rectangle(
            dragStartScreen.X - SystemInformation.DragSize.Width / 2,
            dragStartScreen.Y - SystemInformation.DragSize.Height / 2,
            SystemInformation.DragSize.Width,
            SystemInformation.DragSize.Height);

        if (dragBounds.Contains(currentPosition))
        {
            return;
        }

        dragging = true;
        Cursor = Cursors.SizeAll;
    }

    private void CompleteDragIfNeeded(object? sender, MouseEventArgs e)
    {
        if (!dragArmed)
        {
            return;
        }

        var wasDragging = dragging;
        dragArmed = false;
        dragging = false;
        Capture = false;
        iconView.Capture = false;
        titleLabel.Capture = false;
        Cursor = Cursors.Hand;

        if (wasDragging && e.Button == MouseButtons.Left)
        {
            DragCompleted?.Invoke(this, new SessionTabDragEventArgs(Session, Cursor.Position));
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            ActivateRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

internal sealed class SessionTabDragEventArgs : EventArgs
{
    public SessionTabDragEventArgs(TerminalSession session, Point screenLocation)
    {
        Session = session;
        ScreenLocation = screenLocation;
    }

    public TerminalSession Session { get; }

    public Point ScreenLocation { get; }
}
