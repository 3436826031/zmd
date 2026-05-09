namespace Zmd;

internal sealed class AiTerminalProfileDialog : Form
{
    private readonly ComboBox iconInput = new();
    private readonly TextBox commandInput = new();
    private readonly Button okButton = new();
    private readonly Button cancelButton = new();

    public AiTerminalProfileDialog()
    {
        Text = "新增 AI 终端";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 150);
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);

        var iconLabel = CreateLabel("图标", 18, 20);
        iconInput.Left = 86;
        iconInput.Top = 16;
        iconInput.Width = 242;
        iconInput.DropDownStyle = ComboBoxStyle.DropDownList;
        iconInput.FlatStyle = FlatStyle.Flat;
        iconInput.BackColor = Color.FromArgb(34, 34, 34);
        iconInput.ForeColor = Color.White;
        foreach (var icon in SessionIconCatalog.All)
        {
            iconInput.Items.Add(icon);
        }

        iconInput.DisplayMember = nameof(SessionIconDefinition.Name);
        iconInput.SelectedIndex = Math.Max(0, SessionIconCatalog.All.ToList().FindIndex(icon => icon.Key == SessionIconCatalog.DefaultKey));

        var commandLabel = CreateLabel("命令", 18, 60);
        commandInput.Left = 86;
        commandInput.Top = 56;
        commandInput.Width = 242;
        commandInput.Height = 24;
        commandInput.BorderStyle = BorderStyle.FixedSingle;
        commandInput.BackColor = Color.FromArgb(34, 34, 34);
        commandInput.ForeColor = Color.White;

        okButton.Text = "确定";
        okButton.Left = 174;
        okButton.Top = 106;
        okButton.Width = 74;
        okButton.Height = 28;
        okButton.DialogResult = DialogResult.OK;
        ConfigureButton(okButton);

        cancelButton.Text = "取消";
        cancelButton.Left = 254;
        cancelButton.Top = 106;
        cancelButton.Width = 74;
        cancelButton.Height = 28;
        cancelButton.DialogResult = DialogResult.Cancel;
        ConfigureButton(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(iconLabel);
        Controls.Add(iconInput);
        Controls.Add(commandLabel);
        Controls.Add(commandInput);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    public string Command => commandInput.Text.Trim();

    public string IconKey => iconInput.SelectedItem is SessionIconDefinition icon
        ? icon.Key
        : SessionIconCatalog.DefaultKey;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(Command))
        {
            MessageBox.Show(this, "请输入自动执行命令。", "zmd", MessageBoxButtons.OK, MessageBoxIcon.Information);
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    private static Label CreateLabel(string text, int left, int top)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 56,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(230, 230, 230)
        };
    }

    private static void ConfigureButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(74, 74, 74);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 48, 48);
        button.BackColor = Color.FromArgb(34, 34, 34);
        button.ForeColor = Color.White;
    }
}
