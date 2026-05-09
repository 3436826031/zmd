using System.Reflection;

namespace Zmd;

internal sealed class AboutDialog : Form
{
    private readonly Button okButton = new();

    public AboutDialog()
    {
        Text = "关于 zmd";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 250);
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(18);

        var titleLabel = new Label
        {
            Text = "zmd",
            Left = 20,
            Top = 18,
            Width = 360,
            Height = 38,
            Font = new Font("Segoe UI", 18.0f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var versionLabel = CreateLabel($"版本：{VersionText()}", 22, 66, 360);
        var authorLabel = CreateLabel("作者：34368", 22, 96, 360);
        var userLabel = CreateLabel($"当前用户：{Environment.UserName}", 22, 126, 360);
        var runtimeLabel = CreateLabel($".NET：{Environment.Version}", 22, 156, 360);
        var descriptionLabel = CreateLabel("轻量 Windows 终端，面向多会话、分屏和 AI 终端工作流。", 22, 186, 360);

        okButton.Text = "确定";
        okButton.Left = 326;
        okButton.Top = 210;
        okButton.Width = 74;
        okButton.Height = 28;
        okButton.DialogResult = DialogResult.OK;
        ConfigureButton(okButton);

        AcceptButton = okButton;
        CancelButton = okButton;

        Controls.Add(titleLabel);
        Controls.Add(versionLabel);
        Controls.Add(authorLabel);
        Controls.Add(userLabel);
        Controls.Add(runtimeLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(okButton);
    }

    private static string VersionText()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null)
        {
            return "未知";
        }

        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static Label CreateLabel(string text, int left, int top, int width)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 24,
            ForeColor = Color.FromArgb(220, 224, 230),
            TextAlign = ContentAlignment.MiddleLeft
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
