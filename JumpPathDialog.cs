namespace Zmd;

internal sealed class JumpPathDialog : Form
{
    private readonly TextBox pathInput = new();
    private readonly Button okButton = new();
    private readonly Button cancelButton = new();

    public JumpPathDialog(string initialPath)
    {
        Text = "跳转路径";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 128);
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);

        var pathLabel = new Label
        {
            Text = "路径",
            Left = 18,
            Top = 22,
            Width = 56,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(230, 230, 230)
        };

        pathInput.Left = 74;
        pathInput.Top = 20;
        pathInput.Width = 360;
        pathInput.Height = 24;
        pathInput.BorderStyle = BorderStyle.FixedSingle;
        pathInput.BackColor = Color.FromArgb(34, 34, 34);
        pathInput.ForeColor = Color.White;
        pathInput.Text = initialPath;

        okButton.Text = "确定";
        okButton.Left = 280;
        okButton.Top = 78;
        okButton.Width = 74;
        okButton.Height = 28;
        okButton.DialogResult = DialogResult.OK;
        ConfigureButton(okButton);

        cancelButton.Text = "取消";
        cancelButton.Left = 360;
        cancelButton.Top = 78;
        cancelButton.Width = 74;
        cancelButton.Height = 28;
        cancelButton.DialogResult = DialogResult.Cancel;
        ConfigureButton(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(pathLabel);
        Controls.Add(pathInput);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
    }

    public string PathText => pathInput.Text.Trim();

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        pathInput.Focus();
        pathInput.SelectAll();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(PathText))
        {
            MessageBox.Show(this, "请输入要跳转的路径。", "zmd", MessageBoxButtons.OK, MessageBoxIcon.Information);
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
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
