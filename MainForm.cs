namespace Zmd;

internal sealed class MainForm : Form
{
    private static readonly (string Name, Color Color)[] TagColors =
    {
        ("无", Color.Transparent),
        ("红色", Color.FromArgb(229, 91, 91)),
        ("橙色", Color.FromArgb(237, 151, 70)),
        ("黄色", Color.FromArgb(232, 198, 82)),
        ("绿色", Color.FromArgb(94, 197, 130)),
        ("蓝色", Color.FromArgb(85, 158, 220)),
        ("紫色", Color.FromArgb(164, 125, 226))
    };

    private const int DefaultOutputBrightness = 100;
    private const int LowOutputBrightness = 35;
    private static readonly Color TerminalPaneBackground = Color.FromArgb(12, 12, 12);
    private static readonly Color TerminalPaneBorder = Color.FromArgb(56, 60, 68);
    private static readonly IReadOnlyList<AiTerminalProfile> BuiltInAiProfiles = new[]
    {
        new AiTerminalProfile { Command = "claude", Icon = "claude" },
        new AiTerminalProfile { Command = "codex", Icon = "openai" },
        new AiTerminalProfile { Command = "opencode", Icon = "opencode" },
        new AiTerminalProfile { Command = "gemini", Icon = "gemini" }
    };

    private static readonly Size DefaultWindowSize = new(980, 620);
    private static readonly Size CompactWindowSize = new(720, 480);
    private readonly TerminalSettings settings = TerminalSettingsStore.Load();
    private readonly Panel terminalHost = new();
    private readonly SplitContainer splitHost = new();
    private readonly Splitter sideBarSplitter = new();
    private readonly Panel sideBar = new();
    private readonly ToolTip toolTip = new();
    private readonly FlowLayoutPanel floatingActions = new();
    private readonly RoundedButton jumpPathButton = new();
    private readonly RoundedButton compactModeButton = new();
    private readonly RoundedButton floatingPinButton = new();
    private readonly FlowLayoutPanel topActions = new();
    private readonly Panel tabHost = new();
    private readonly RoundedButton sideBarToggleButton = new();
    private readonly RoundedButton newButton = new();
    private readonly RoundedButton menuButton = new();
    private readonly RoundedButton pinButton = new();
    private readonly RoundedButton settingsButton = new();
    private readonly Dictionary<TerminalSession, TerminalControl> controls = new();
    private readonly Dictionary<TerminalSession, SessionTab> tabs = new();
    private readonly Dictionary<TerminalSession, TerminalPane> panes = new();
    private readonly Dictionary<TerminalSession, int> outputBrightnesses = new();
    private ContextMenuStrip? activeMenu;
    private TerminalSession? activeSession;
    private int expandedSideBarWidth = 188;
    private const int CollapsedSideBarWidth = 44;
    private bool sideBarCollapsed;
    private bool compactModeActive;

    public MainForm()
    {
        Text = "zmd";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
        MinimumSize = new Size(720, 420);
        Size = DefaultWindowSize;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(16, 16, 16);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        KeyPreview = true;

        terminalHost.Dock = DockStyle.Fill;
        terminalHost.BackColor = TerminalPaneBackground;
        terminalHost.AllowDrop = true;

        splitHost.Dock = DockStyle.Fill;
        splitHost.BackColor = TerminalPaneBorder;
        splitHost.Panel1.BackColor = TerminalPaneBorder;
        splitHost.Panel2.BackColor = TerminalPaneBorder;
        splitHost.Panel1.Padding = new Padding(1);
        splitHost.Panel2.Padding = new Padding(1);
        splitHost.Panel1MinSize = 0;
        splitHost.Panel2MinSize = 0;
        splitHost.SplitterWidth = 6;
        splitHost.Panel2Collapsed = true;
        splitHost.AllowDrop = true;
        splitHost.Panel1.AllowDrop = true;
        splitHost.Panel2.AllowDrop = true;

        sideBar.Dock = DockStyle.Right;
        sideBar.Width = expandedSideBarWidth;
        sideBar.MinimumSize = new Size(32, 0);
        sideBar.BackColor = Color.FromArgb(18, 19, 22);
        sideBar.Padding = new Padding(8, 8, 8, 8);

        sideBarSplitter.Dock = DockStyle.Right;
        sideBarSplitter.Width = 4;
        sideBarSplitter.MinExtra = 420;
        sideBarSplitter.MinSize = 140;
        sideBarSplitter.BackColor = Color.FromArgb(24, 26, 30);
        sideBarSplitter.Cursor = Cursors.SizeWE;

        floatingActions.Size = new Size(100, 34);
        floatingActions.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        floatingActions.FlowDirection = FlowDirection.LeftToRight;
        floatingActions.WrapContents = false;
        floatingActions.Padding = new Padding(4, 4, 4, 4);
        floatingActions.BackColor = Color.FromArgb(30, 32, 36);

        topActions.Dock = DockStyle.Top;
        topActions.Height = 34;
        topActions.FlowDirection = FlowDirection.LeftToRight;
        topActions.WrapContents = false;
        topActions.Padding = new Padding(0, 4, 0, 6);
        topActions.BackColor = Color.FromArgb(18, 19, 22);

        tabHost.Dock = DockStyle.Fill;
        tabHost.BackColor = Color.FromArgb(18, 19, 22);

        ConfigureToolTip();
        ConfigureActionButton(sideBarToggleButton, "<");
        ConfigureActionButton(newButton, "+");
        ConfigureActionButton(menuButton, "...");
        ConfigureActionButton(pinButton, "^");
        ConfigureActionButton(settingsButton, "*");
        sideBarToggleButton.Glyph = ButtonGlyph.CollapseRight;
        newButton.Glyph = ButtonGlyph.Add;
        menuButton.Glyph = ButtonGlyph.None;
        menuButton.ImageKey = "branchbox";
        pinButton.Glyph = ButtonGlyph.Pin;
        settingsButton.Glyph = ButtonGlyph.Settings;
        settingsButton.ImageKey = "settings-image";
        ConfigureFloatingActionButton(jumpPathButton, ButtonGlyph.JumpPath);
        ConfigureFloatingActionButton(compactModeButton, ButtonGlyph.CompactMode);
        ConfigureFloatingActionButton(floatingPinButton, ButtonGlyph.Pin);
        SetActionToolTips();
        topActions.Controls.Add(sideBarToggleButton);
        topActions.Controls.Add(newButton);
        topActions.Controls.Add(menuButton);
        topActions.Controls.Add(pinButton);
        topActions.Controls.Add(settingsButton);
        floatingActions.Controls.Add(jumpPathButton);
        floatingActions.Controls.Add(compactModeButton);
        floatingActions.Controls.Add(floatingPinButton);

        sideBar.Controls.Add(tabHost);
        sideBar.Controls.Add(topActions);
        terminalHost.Controls.Add(splitHost);
        Controls.Add(terminalHost);
        Controls.Add(sideBar);
        Controls.Add(sideBarSplitter);
        Controls.Add(floatingActions);
        floatingActions.BringToFront();
        PositionFloatingActions();
        ApplySideBarDock();
        UpdateTerminalHostPadding();

        sideBarToggleButton.Click += (_, _) => ToggleSideBar();
        newButton.Click += (_, _) => ShowNewTerminalMenu(newButton, new Point(0, newButton.Height));
        menuButton.Click += (_, _) => ShowShellMenu();
        pinButton.Click += (_, _) => ToggleTopMost();
        floatingPinButton.Click += (_, _) => ToggleTopMost();
        jumpPathButton.Click += (_, _) => ShowJumpPathDialog();
        compactModeButton.Click += (_, _) => ApplyCompactMode();
        settingsButton.Click += (_, _) => ShowSettingsMenu();
        Activated += (_, _) =>
        {
            if (activeSession is not null && controls.TryGetValue(activeSession, out var terminal))
            {
                terminal.ActivateTerminal();
            }
        };
        Resize += (_, _) =>
        {
            PositionFloatingActions();
            QueueVisibleTerminalFits();
        };
        Shown += (_, _) => EnsureWinRLaunchPath();
        Shown += (_, _) => CreateSession();
        Shown += (_, _) => ApplySplitMinSizes();
        FormClosing += (_, _) =>
        {
            TerminalSettingsStore.Save(settings);
            DisposeSessions();
        };
        sideBar.SizeChanged += (_, _) =>
        {
            if (!sideBarCollapsed && sideBar.Width > 360)
            {
                sideBar.Width = 360;
                return;
            }

            if (!sideBarCollapsed)
            {
                expandedSideBarWidth = Math.Max(140, sideBar.Width);
            }

            UpdateTerminalHostPadding();
            QueueVisibleTerminalFits();
        };

        terminalHost.DragEnter += HandleSessionDragEnter;
        terminalHost.DragDrop += HandleSessionDragDrop;
        splitHost.DragEnter += HandleSessionDragEnter;
        splitHost.DragDrop += HandleSessionDragDrop;
        splitHost.Panel1.DragEnter += HandleSessionDragEnter;
        splitHost.Panel1.DragDrop += (_, e) => DropSessionToPane(e, TerminalPane.Left);
        splitHost.Panel2.DragEnter += HandleSessionDragEnter;
        splitHost.Panel2.DragDrop += (_, e) => DropSessionToPane(e, TerminalPane.Right);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplySystemTitleBarTheme();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F2 && activeSession is not null)
        {
            BeginRenameSession(activeSession);
            e.SuppressKeyPress = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private TerminalSession? CreateSession(string? shellPath = null)
    {
        var sessionSettings = shellPath is null
            ? settings
            : new TerminalSettings
            {
                ShellPath = shellPath,
                WorkingDirectory = settings.WorkingDirectory,
                FontFamily = settings.FontFamily,
                FontSize = settings.FontSize,
                InitialColumns = settings.InitialColumns,
                InitialRows = settings.InitialRows
            };

        var terminal = new TerminalControl(sessionSettings)
        {
            Dock = DockStyle.Fill,
            Visible = true,
            OutputBrightness = DefaultOutputBrightness
        };

        var session = new TerminalSession(sessionSettings, terminal.Columns, terminal.Rows);
        var tab = new SessionTab(session);
        tab.Compact = sideBarCollapsed;

        session.OutputReceived += (_, text) =>
        {
            if (!terminal.IsDisposed)
            {
                terminal.BeginInvoke(() => terminal.AppendOutput(text));
            }
        };
        session.Exited += (_, _) =>
        {
            if (!IsDisposed)
            {
                BeginInvoke(() => CloseSession(session));
            }
        };
        terminal.InputReceived += (_, text) => session.Write(text);
        terminal.TerminalSizeChanged += (_, _) => session.Resize(terminal.Columns, terminal.Rows);
        terminal.DragEnter += HandleSessionDragEnter;
        terminal.SessionDragDrop += HandleSessionDragDrop;
        tab.ActivateRequested += (_, _) => ActivateSession(session);
        tab.CloseRequested += (_, _) => CloseSession(session);
        tab.MenuRequested += (_, e) =>
        {
            ActivateSession(session);
            ShowSessionMenu(session, tab, e.Location);
        };
        tab.RenameCommitted += (_, _) => ActivateSession(session);
        tab.DragCompleted += (_, e) => MoveSessionByScreenPoint(e.Session, e.ScreenLocation);

        controls.Add(session, terminal);
        tabs.Add(session, tab);
        panes.Add(session, TerminalPane.Left);
        outputBrightnesses.Add(session, DefaultOutputBrightness);
        splitHost.Panel1.Controls.Add(terminal);
        tabHost.Controls.Add(tab);
        tab.BringToFront();

        ActivateSession(session);
        return session;
    }

    private void CreateAiSession(AiTerminalProfile profile)
    {
        var session = CreateSession();
        if (session is null)
        {
            return;
        }

        session.SetIcon(profile.Icon);
        session.Title = profile.Command;
        BeginInvoke(() => session.Write(profile.Command + "\r"));
    }

    private void CreateCmdSession()
    {
        var session = CreateSession(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe");
        session?.SetIcon("cmd");
    }

    private void ActivateSession(TerminalSession session)
    {
        if (!controls.TryGetValue(session, out var terminal))
        {
            return;
        }

        if (activeSession == session && terminal.Visible)
        {
            terminal.ActivateTerminal();
            return;
        }

        activeSession = session;

        foreach (var pair in tabs)
        {
            pair.Value.Selected = pair.Key == session;
        }

        if (panes.TryGetValue(session, out var pane))
        {
            BringPaneSessionToFront(pane, session);
        }

        UpdateSplitLayout();
        terminal.ActivateTerminal();
        session.Resize(terminal.Columns, terminal.Rows);
    }

    private void CloseSession(TerminalSession session)
    {
        if (!controls.TryGetValue(session, out var terminal))
        {
            return;
        }

        var wasActive = activeSession == session;

        controls.Remove(session);
        tabs.Remove(session, out var tab);
        panes.Remove(session);
        outputBrightnesses.Remove(session);

        terminal.Parent?.Controls.Remove(terminal);
        tabHost.Controls.Remove(tab);

        terminal.Dispose();
        tab?.Dispose();
        session.Dispose();

        if (controls.Count == 0)
        {
            Close();
            return;
        }

        if (wasActive)
        {
            ActivateSession(controls.Keys.Last());
        }
        else
        {
            UpdateSplitLayout();
        }
    }

    private void ShowShellMenu()
    {
        var menu = CreateDarkMenu();
        menu.Items.Add(CreateMenuItem("清除终端内容", "close", (_, _) => ClearActiveSession()));
        menu.Items.Add("跳转路径", null, (_, _) => ShowJumpPathDialog());
        var cancelSplitItem = menu.Items.Add("取消分屏", null, (_, _) => CancelSplitLayout());
        cancelSplitItem.Enabled = panes.Values.Any(pane => pane == TerminalPane.Right);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateMenuItem("cmd", "cmd", (_, _) => CreateCmdSession()));
        menu.Items.Add("PowerShell", null, (_, _) => CreateSession("powershell.exe"));
        menu.Items.Add("Windows PowerShell", null, (_, _) => CreateSession("pwsh.exe"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateBrightnessMenuItem());
        ShowMenu(menu, menuButton);
    }

    private void ShowJumpPathDialog()
    {
        if (activeSession is null)
        {
            return;
        }

        using var dialog = new Form
        {
            Text = "跳转路径",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            BackColor = Color.FromArgb(24, 26, 30),
            ForeColor = UiTheme.Text,
            ClientSize = new Size(420, 116),
            Font = Font
        };
        var input = new TextBox
        {
            Left = 16,
            Top = 18,
            Width = 388,
            Text = settings.WorkingDirectory,
            BackColor = Color.FromArgb(34, 34, 34),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        var okButton = new Button
        {
            Text = "跳转",
            DialogResult = DialogResult.OK,
            Left = 250,
            Top = 66,
            Width = 72,
            Height = 28
        };
        var cancelButton = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Left = 332,
            Top = 66,
            Width = 72,
            Height = 28
        };
        dialog.Controls.Add(input);
        dialog.Controls.Add(okButton);
        dialog.Controls.Add(cancelButton);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var path = input.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        activeSession.Write($"cd /d \"{path.Replace("\"", "\"\"")}\"\r");
    }

    private void ShowNewTerminalMenu(Control owner, Point location)
    {
        var menu = CreateDarkMenu();
        menu.Items.Add(CreateMenuItem("cmd", "cmd", (_, _) => CreateCmdSession()));
        menu.Items.Add(new ToolStripSeparator());
        foreach (var profile in BuiltInAiProfiles)
        {
            menu.Items.Add(CreateAiProfileMenuItem(profile));
        }

        if (settings.AiProfiles.Count > 0)
        {
            menu.Items.Add(new ToolStripSeparator());
            foreach (var profile in settings.AiProfiles)
            {
                menu.Items.Add(CreateAiProfileMenuItem(profile));
            }
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("新增自定义", null, (_, _) => ShowAiProfileDialog());
        ShowMenu(menu, owner, location);
    }

    private ToolStripMenuItem CreateAiProfileMenuItem(AiTerminalProfile profile)
    {
        return CreateMenuItem(profile.Command, profile.Icon, (_, _) => CreateAiSession(profile));
    }

    private ToolStripMenuItem CreateMenuItem(string text, string iconKey, EventHandler onClick)
    {
        var item = new ToolStripMenuItem(text)
        {
            Image = SessionIconCatalog.CreateBitmap(iconKey, UiTheme.Text, 18)
        };
        item.Click += onClick;
        return item;
    }

    private void ShowAiProfileDialog()
    {
        using var dialog = new AiTerminalProfileDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var profile = new AiTerminalProfile
        {
            Command = dialog.Command,
            Icon = dialog.IconKey
        };
        settings.AiProfiles.Add(profile);
        TerminalSettingsStore.Save(settings);
        CreateAiSession(profile);
    }

    private void ShowSettingsMenu()
    {
        var menu = CreateDarkMenu();
        menu.Items.Add("Font and size...", null, (_, _) => ShowSettingsDialog());
        menu.Items.Add(CreateSideBarDockMenu());
        menu.Items.Add("一键设置 Win+R 打开 zmd", null, (_, _) => AddCurrentDirectoryToUserPath());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("检查更新", null, (_, _) => OpenReleasesPage());
        menu.Items.Add("关于 zmd", null, (_, _) => ShowAboutDialog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add($"Current: {settings.FontFamily} {settings.FontSize:0.#}pt").Enabled = false;
        menu.Items.Add($"Directory: {settings.WorkingDirectory}").Enabled = false;
        ShowMenu(menu, settingsButton);
    }

    private ToolStripMenuItem CreateSideBarDockMenu()
    {
        var item = new ToolStripMenuItem("侧边栏位置");
        var leftItem = new ToolStripMenuItem("左侧")
        {
            Checked = IsSideBarLeft()
        };
        var rightItem = new ToolStripMenuItem("右侧")
        {
            Checked = !IsSideBarLeft()
        };

        leftItem.Click += (_, _) => SetSideBarDock(left: true);
        rightItem.Click += (_, _) => SetSideBarDock(left: false);
        item.DropDownItems.Add(leftItem);
        item.DropDownItems.Add(rightItem);
        return item;
    }

    private void ShowSessionMenu(TerminalSession session, Control owner, Point location)
    {
        var menu = CreateDarkMenu();
        menu.Items.Add("重命名", null, (_, _) => BeginRenameSession(session));
        menu.Items.Add("一键清空", null, (_, _) => ClearSession(session));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateTagColorMenu(session));
        menu.Items.Add(CreateIconMenu(session));
        ShowMenu(menu, owner, location);
    }

    private ToolStripMenuItem CreateTagColorMenu(TerminalSession session)
    {
        var item = new ToolStripMenuItem("颜色标签");

        foreach (var tag in TagColors)
        {
            var colorItem = new ToolStripMenuItem(tag.Name)
            {
                Checked = session.TagColor == tag.Color
            };
            colorItem.Click += (_, _) => session.SetTagColor(tag.Color);
            item.DropDownItems.Add(colorItem);
        }

        return item;
    }

    private ToolStripMenuItem CreateIconMenu(TerminalSession session)
    {
        var item = new ToolStripMenuItem("更改图标");

        foreach (var icon in SessionIconCatalog.All)
        {
            var iconItem = new ToolStripMenuItem(icon.Name)
            {
                Checked = session.Icon == icon.Key,
                Image = SessionIconCatalog.CreateBitmap(icon.Key, UiTheme.Text, 18)
            };
            iconItem.Click += (_, _) => session.SetIcon(icon.Key);
            item.DropDownItems.Add(iconItem);
        }

        return item;
    }

    private void ShowSettingsDialog()
    {
        using var dialog = new SettingsDialog(settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        settings.FontFamily = dialog.FontFamilyName;
        settings.FontSize = dialog.FontSizeValue;

        foreach (var pair in controls)
        {
            pair.Value.ApplySettings(settings);
            pair.Value.RequestFit();
        }

        QueueVisibleTerminalFits();
    }

    private void ShowAboutDialog()
    {
        using var dialog = new AboutDialog();
        dialog.ShowDialog(this);
    }

    private static void OpenReleasesPage()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppInfo.ReleasesUrl)
        {
            UseShellExecute = true
        });
    }

    private void AddCurrentDirectoryToUserPath()
    {
        var executableDirectory = Path.GetDirectoryName(Application.ExecutablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            MessageBox.Show(this, "无法识别 zmd.exe 所在目录。", "zmd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var added = PathRegistration.AddDirectoryToUserPath(executableDirectory);
            MessageBox.Show(
                this,
                added ? "已加入用户 PATH。重新打开 Win+R 或终端后即可输入 zmd 启动。" : "当前 zmd.exe 所在目录已经在用户 PATH 中。",
                "zmd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"写入用户 PATH 失败：\n\n{ex.Message}", "zmd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EnsureWinRLaunchPath()
    {
        try
        {
            var executableDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            if (!string.IsNullOrWhiteSpace(executableDirectory))
            {
                PathRegistration.AddDirectoryToUserPath(executableDirectory);
            }
        }
        catch
        {
            // PATH registration is a convenience feature and should not block startup.
        }
    }

    private void ToggleTopMost()
    {
        TopMost = !TopMost;
        UpdateTopMostButtons();
        SetActionToolTips();
    }

    private void UpdateTopMostButtons()
    {
        ApplyPinButtonState(pinButton, Color.FromArgb(18, 19, 22));
        ApplyPinButtonState(floatingPinButton, Color.FromArgb(30, 32, 36));
    }

    private void ApplyPinButtonState(RoundedButton button, Color normalBackColor)
    {
        button.NormalBackColor = TopMost ? Color.FromArgb(44, 74, 86) : normalBackColor;
        button.BorderColor = TopMost ? Color.FromArgb(82, 150, 174) : normalBackColor;
        button.GlyphColor = TopMost ? Color.White : UiTheme.MutedText;
        button.Invalidate();
    }

    private void ApplyCompactMode()
    {
        WindowState = FormWindowState.Normal;
        compactModeActive = !compactModeActive;

        if (compactModeActive)
        {
            Size = CompactWindowSize;
            CollapseSideBar();
        }
        else
        {
            Size = DefaultWindowSize;
            ExpandSideBar();
        }

        PositionFloatingActions();
        SetActionToolTips();
        QueueVisibleTerminalFits();
    }

    private void ApplySystemTitleBarTheme()
    {
        var enabled = 1;
        var captionColor = ToColorRef(Color.FromArgb(32, 32, 32));
        var borderColor = ToColorRef(Color.FromArgb(32, 32, 32));
        var textColor = ToColorRef(Color.FromArgb(238, 238, 238));

        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, sizeof(int));
        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
        NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
    }

    private static ContextMenuStrip CreateDarkMenu()
    {
        var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(32, 32, 32),
            ForeColor = Color.White,
            Renderer = DarkToolStripRenderer.Instance
        };
        menu.ItemAdded += (_, e) =>
        {
            if (e.Item is not null)
            {
                ApplyDarkMenuStyle(e.Item);
            }
        };
        return menu;
    }

    private static void ApplyDarkMenuStyle(ToolStripItem item)
    {
        item.BackColor = Color.FromArgb(32, 32, 32);
        item.ForeColor = Color.White;

        if (item is ToolStripMenuItem menuItem)
        {
            menuItem.DropDown.BackColor = Color.FromArgb(32, 32, 32);
            menuItem.DropDown.ForeColor = Color.White;
            menuItem.DropDown.Renderer = DarkToolStripRenderer.Instance;
            menuItem.DropDown.ItemAdded += (_, e) =>
            {
                if (e.Item is not null)
                {
                    ApplyDarkMenuStyle(e.Item);
                }
            };

            foreach (ToolStripItem child in menuItem.DropDownItems)
            {
                ApplyDarkMenuStyle(child);
            }
        }
    }

    private ToolStripControlHost CreateBrightnessMenuItem()
    {
        var currentBrightness = ActiveOutputBrightness();
        var panel = new Panel
        {
            Width = 306,
            Height = 54,
            BackColor = Color.FromArgb(32, 32, 32),
            Padding = new Padding(10, 6, 10, 4)
        };

        var label = new Label
        {
            Text = $"当前终端输出亮度 {currentBrightness}%",
            Dock = DockStyle.Top,
            Height = 18,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var slider = new TrackBar
        {
            Dock = DockStyle.Fill,
            Minimum = 15,
            Maximum = 100,
            TickFrequency = 17,
            SmallChange = 5,
            LargeChange = 10,
            Value = currentBrightness,
            BackColor = Color.FromArgb(32, 32, 32)
        };

        var normalButton = new RoundedButton
        {
            Text = "正常",
            Dock = DockStyle.Right,
            Width = 48,
            CornerRadius = 7,
            NormalBackColor = Color.FromArgb(42, 45, 52),
            HoverBackColor = Color.FromArgb(54, 84, 98),
            PressedBackColor = Color.FromArgb(72, 145, 176),
            BorderColor = Color.FromArgb(68, 74, 84),
            ForeColor = UiTheme.Text,
            Glyph = ButtonGlyph.None
        };

        var lowButton = new RoundedButton
        {
            Text = "低",
            Dock = DockStyle.Right,
            Width = 34,
            CornerRadius = 7,
            NormalBackColor = Color.FromArgb(42, 45, 52),
            HoverBackColor = Color.FromArgb(54, 84, 98),
            PressedBackColor = Color.FromArgb(72, 145, 176),
            BorderColor = Color.FromArgb(68, 74, 84),
            ForeColor = UiTheme.Text,
            Glyph = ButtonGlyph.None
        };

        slider.ValueChanged += (_, _) =>
        {
            ApplyOutputBrightness(slider.Value);
            label.Text = $"当前终端输出亮度 {ActiveOutputBrightness()}%";
        };

        lowButton.Click += (_, _) =>
        {
            slider.Value = LowOutputBrightness;
        };

        normalButton.Click += (_, _) =>
        {
            slider.Value = DefaultOutputBrightness;
        };

        var controlsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(32, 32, 32)
        };

        controlsPanel.Controls.Add(slider);
        controlsPanel.Controls.Add(normalButton);
        controlsPanel.Controls.Add(lowButton);
        normalButton.BringToFront();
        lowButton.BringToFront();

        panel.Controls.Add(controlsPanel);
        panel.Controls.Add(label);

        return new ToolStripControlHost(panel)
        {
            AutoSize = false,
            Width = panel.Width,
            Height = panel.Height,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
    }

    private void ApplyOutputBrightness(int brightness)
    {
        if (activeSession is null || !controls.TryGetValue(activeSession, out var terminal))
        {
            return;
        }

        var normalizedBrightness = Math.Clamp(brightness, 15, 100);
        outputBrightnesses[activeSession] = normalizedBrightness;
        terminal.OutputBrightness = normalizedBrightness;
    }

    private int ActiveOutputBrightness()
    {
        if (activeSession is not null && outputBrightnesses.TryGetValue(activeSession, out var brightness))
        {
            return brightness;
        }

        return DefaultOutputBrightness;
    }

    private void ToggleSideBar()
    {
        if (sideBarCollapsed)
        {
            ExpandSideBar();
        }
        else
        {
            CollapseSideBar();
        }

        QueueVisibleTerminalFits();
    }

    private void SetSideBarDock(bool left)
    {
        settings.SideBarDock = left ? "Left" : "Right";
        ApplySideBarDock();
        TerminalSettingsStore.Save(settings);
        QueueVisibleTerminalFits();
    }

    private void ApplySideBarDock()
    {
        var dock = IsSideBarLeft() ? DockStyle.Left : DockStyle.Right;
        sideBar.Dock = dock;
        sideBarSplitter.Dock = dock;
        sideBarToggleButton.Glyph = SideBarToggleGlyph();
        SetActionToolTips();
        sideBar.BringToFront();
        sideBarSplitter.BringToFront();
        floatingActions.BringToFront();
        PositionFloatingActions();
        UpdateTerminalHostPadding();
        QueueVisibleTerminalFits();
    }

    private bool IsSideBarLeft()
    {
        return settings.SideBarDock.Equals("Left", StringComparison.OrdinalIgnoreCase);
    }

    private ButtonGlyph SideBarToggleGlyph()
    {
        if (sideBarCollapsed)
        {
            return IsSideBarLeft() ? ButtonGlyph.ExpandRight : ButtonGlyph.ExpandLeft;
        }

        return IsSideBarLeft() ? ButtonGlyph.CollapseLeft : ButtonGlyph.CollapseRight;
    }

    private void CollapseSideBar()
    {
        if (sideBarCollapsed)
        {
            return;
        }

        expandedSideBarWidth = Math.Max(140, sideBar.Width);
        sideBarCollapsed = true;
        sideBar.Width = CollapsedSideBarWidth;
        sideBar.Padding = new Padding(5, 8, 5, 8);
        sideBarSplitter.Visible = false;
        tabHost.Visible = true;
        topActions.Height = 34;
        topActions.FlowDirection = FlowDirection.LeftToRight;
        topActions.Padding = new Padding(3, 4, 3, 6);
        newButton.Visible = false;
        menuButton.Visible = false;
        pinButton.Visible = false;
        settingsButton.Visible = false;
        SetTabsCompact(true);
        sideBarToggleButton.Glyph = SideBarToggleGlyph();
        SetActionToolTips();
        UpdateTerminalHostPadding();
        QueueVisibleTerminalFits();
    }

    private void ExpandSideBar()
    {
        if (!sideBarCollapsed)
        {
            return;
        }

        sideBarCollapsed = false;
        sideBar.Width = Math.Clamp(expandedSideBarWidth, 140, 360);
        sideBar.Padding = new Padding(8, 8, 8, 8);
        sideBarSplitter.Visible = true;
        tabHost.Visible = true;
        topActions.Height = 34;
        topActions.FlowDirection = FlowDirection.LeftToRight;
        topActions.Padding = new Padding(0, 4, 0, 6);
        newButton.Visible = true;
        menuButton.Visible = true;
        pinButton.Visible = true;
        settingsButton.Visible = true;
        SetTabsCompact(false);
        sideBarToggleButton.Glyph = SideBarToggleGlyph();
        SetActionToolTips();
        UpdateTerminalHostPadding();
        QueueVisibleTerminalFits();
    }

    private void SetTabsCompact(bool compact)
    {
        foreach (var tab in tabs.Values)
        {
            tab.Compact = compact;
        }
    }

    private void ShowMenu(ContextMenuStrip menu, Control owner)
    {
        ShowMenu(menu, owner, new Point(0, owner.Height));
    }

    private void ShowMenu(ContextMenuStrip menu, Control owner, Point location)
    {
        activeMenu?.Close();
        activeMenu = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(activeMenu, menu))
            {
                activeMenu = null;
            }
        };
        menu.Show(owner, location);
    }

    private void BeginRenameSession(TerminalSession session)
    {
        if (!tabs.TryGetValue(session, out var tab))
        {
            return;
        }

        ActivateSession(session);
        tab.BeginRename();
    }

    private void ClearSession(TerminalSession session)
    {
        if (!controls.TryGetValue(session, out var terminal))
        {
            return;
        }

        terminal.Clear();
        terminal.Focus();
    }

    private void ClearActiveSession()
    {
        if (activeSession is not null)
        {
            ClearSession(activeSession);
        }
    }

    private void HandleSessionDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(SessionTab.DragDataFormat) == true)
        {
            e.Effect = DragDropEffects.Move;
            return;
        }

        e.Effect = DragDropEffects.None;
    }

    private void HandleSessionDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(SessionTab.DragDataFormat) != true)
        {
            return;
        }

        var localPoint = splitHost.PointToClient(new Point(e.X, e.Y));
        var targetPane = localPoint.X < splitHost.Width / 2 ? TerminalPane.Left : TerminalPane.Right;
        DropSessionToPane(e, targetPane);
    }

    private void DropSessionToPane(DragEventArgs e, TerminalPane targetPane)
    {
        if (e.Data?.GetData("Zmd.TerminalSession") is not TerminalSession session || !controls.ContainsKey(session))
        {
            return;
        }

        MoveSessionToPane(session, targetPane);
        ActivateSession(session);
    }

    private void MoveSessionByScreenPoint(TerminalSession session, Point screenLocation)
    {
        if (!controls.ContainsKey(session))
        {
            return;
        }

        var targetPane = PaneFromScreenPoint(screenLocation);
        MoveSessionToPane(session, targetPane);
        ActivateSession(session);
    }

    private TerminalPane PaneFromScreenPoint(Point screenLocation)
    {
        var splitBounds = splitHost.RectangleToScreen(splitHost.ClientRectangle);
        if (splitBounds.Contains(screenLocation))
        {
            return screenLocation.X < splitBounds.Left + splitBounds.Width / 2
                ? TerminalPane.Left
                : TerminalPane.Right;
        }

        return activeSession is not null && panes.TryGetValue(activeSession, out var currentPane)
            ? currentPane
            : TerminalPane.Left;
    }

    private void MoveSessionToPane(TerminalSession session, TerminalPane targetPane)
    {
        if (!controls.TryGetValue(session, out var terminal))
        {
            return;
        }

        var previousPane = panes.TryGetValue(session, out var existingPane) ? existingPane : targetPane;
        panes[session] = targetPane;
        terminal.Parent?.Controls.Remove(terminal);
        TargetPanel(targetPane).Controls.Add(terminal);
        terminal.Dock = DockStyle.Fill;
        terminal.Visible = true;
        terminal.BringToFront();
        RestorePaneVisibility(previousPane, except: session);
        UpdateSplitLayout();
    }

    private void CancelSplitLayout()
    {
        var rightSessions = panes
            .Where(pair => pair.Value == TerminalPane.Right)
            .Select(pair => pair.Key)
            .ToArray();
        if (rightSessions.Length == 0)
        {
            return;
        }

        foreach (var session in rightSessions)
        {
            MoveSessionToPane(session, TerminalPane.Left);
        }

        if (activeSession is not null)
        {
            ActivateSession(activeSession);
        }

        UpdateSplitLayout();
        QueueVisibleTerminalFits();
    }

    private void BringPaneSessionToFront(TerminalPane pane, TerminalSession session)
    {
        if (!controls.TryGetValue(session, out var terminal))
        {
            return;
        }

        foreach (var pair in controls)
        {
            if (panes.TryGetValue(pair.Key, out var pairPane) && pairPane == pane)
            {
                pair.Value.Visible = pair.Key == session;
            }
        }

        terminal.Visible = true;
        terminal.BringToFront();
    }

    private void RestorePaneVisibility(TerminalPane pane, TerminalSession except)
    {
        if (panes.TryGetValue(except, out var exceptPane) && exceptPane == pane)
        {
            return;
        }

        var visibleSession = panes
            .Where(pair => pair.Value == pane && pair.Key != except)
            .Select(pair => pair.Key)
            .LastOrDefault();

        if (visibleSession is null)
        {
            return;
        }

        BringPaneSessionToFront(pane, visibleSession);
    }

    private void UpdateSplitLayout()
    {
        if (splitHost.Width <= 0)
        {
            return;
        }

        var hasLeft = panes.Values.Any(pane => pane == TerminalPane.Left);
        var hasRight = panes.Values.Any(pane => pane == TerminalPane.Right);

        if (!hasLeft && hasRight)
        {
            foreach (var session in panes.Where(pair => pair.Value == TerminalPane.Right).Select(pair => pair.Key).ToArray())
            {
                MoveSessionToPane(session, TerminalPane.Left);
            }

            return;
        }

        splitHost.Panel2Collapsed = !hasRight;

        if (hasLeft && hasRight)
        {
            ApplySplitMinSizes();
            var maxDistance = splitHost.Width - splitHost.Panel2MinSize - splitHost.SplitterWidth;
            if (maxDistance > splitHost.Panel1MinSize)
            {
                var splitterDistance = Math.Max(splitHost.Panel1MinSize, (splitHost.Width - splitHost.SplitterWidth) / 2);
                splitHost.SplitterDistance = Math.Min(splitterDistance, maxDistance);
            }
        }

        foreach (var pair in controls)
        {
            if (pair.Value.Visible)
            {
                pair.Value.RequestFit();
            }
        }
    }

    private void QueueVisibleTerminalFits()
    {
        foreach (var terminal in controls.Values)
        {
            if (terminal.Visible)
            {
                terminal.RequestFit();
            }
        }
    }

    private Panel TargetPanel(TerminalPane pane)
    {
        return pane == TerminalPane.Left ? splitHost.Panel1 : splitHost.Panel2;
    }

    private void UpdateTerminalHostPadding()
    {
        var reservedWidth = sideBar.Width + (sideBarSplitter.Visible ? sideBarSplitter.Width : 0);
        terminalHost.Padding = IsSideBarLeft()
            ? new Padding(reservedWidth, 0, 0, 0)
            : new Padding(0, 0, reservedWidth, 0);
    }

    private void ApplySplitMinSizes()
    {
        if (splitHost.Width < 380)
        {
            splitHost.Panel1MinSize = 0;
            splitHost.Panel2MinSize = 0;
            return;
        }

        splitHost.Panel1MinSize = 160;
        splitHost.Panel2MinSize = 160;
    }

    private void PositionFloatingActions()
    {
        floatingActions.Location = new Point(
            Math.Max(0, ClientSize.Width - floatingActions.Width - 10),
            Math.Max(0, ClientSize.Height - floatingActions.Height - 10));
        floatingActions.BringToFront();
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    private void ConfigureToolTip()
    {
        DarkToolTip.Configure(toolTip, () => Font);
    }

    private void SetActionToolTips()
    {
        toolTip.SetToolTip(sideBarToggleButton, sideBarCollapsed ? "展开侧边栏" : "收起侧边栏");
        toolTip.SetToolTip(newButton, "新建终端");
        toolTip.SetToolTip(menuButton, "终端菜单");
        toolTip.SetToolTip(pinButton, TopMost ? "取消置顶" : "窗口置顶");
        toolTip.SetToolTip(settingsButton, "设置");
        toolTip.SetToolTip(jumpPathButton, "跳转路径");
        toolTip.SetToolTip(compactModeButton, compactModeActive ? "恢复初始大小" : "简洁模式 720 x 480");
        toolTip.SetToolTip(floatingPinButton, TopMost ? "取消置顶" : "窗口置顶");
    }

    private void ConfigureActionButton(RoundedButton button, string text)
    {
        button.Text = text;
        button.Width = 28;
        button.Height = 24;
        button.Margin = new Padding(0, 0, 7, 0);
        button.CornerRadius = 7;
        button.NormalBackColor = Color.FromArgb(18, 19, 22);
        button.HoverBackColor = Color.FromArgb(40, 43, 49);
        button.PressedBackColor = Color.FromArgb(54, 84, 98);
        button.BorderColor = Color.FromArgb(18, 19, 22);
        button.GlyphColor = UiTheme.MutedText;
        button.ForeColor = UiTheme.MutedText;
        button.TabStop = false;
    }

    private void ConfigureFloatingActionButton(RoundedButton button, ButtonGlyph glyph)
    {
        button.Width = 28;
        button.Height = 26;
        button.Margin = new Padding(0, 0, 4, 0);
        button.CornerRadius = 7;
        button.NormalBackColor = Color.FromArgb(30, 32, 36);
        button.HoverBackColor = Color.FromArgb(44, 48, 56);
        button.PressedBackColor = Color.FromArgb(54, 84, 98);
        button.BorderColor = Color.FromArgb(54, 58, 66);
        button.GlyphColor = UiTheme.MutedText;
        button.ForeColor = UiTheme.MutedText;
        button.Glyph = glyph;
        button.TabStop = false;
    }

    private int ActiveControlColumns()
    {
        return activeSession is not null && controls.TryGetValue(activeSession, out var terminal)
            ? terminal.Columns
            : settings.InitialColumns;
    }

    private int ActiveControlRows()
    {
        return activeSession is not null && controls.TryGetValue(activeSession, out var terminal)
            ? terminal.Rows
            : settings.InitialRows;
    }

    private void DisposeSessions()
    {
        foreach (var session in controls.Keys.ToArray())
        {
            CloseSession(session);
        }
    }

    private enum TerminalPane
    {
        Left,
        Right
    }
}

