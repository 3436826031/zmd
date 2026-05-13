using System.Diagnostics;
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
        ClientSize = new Size(500, 310);
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(18);

        var titleLabel = new Label
        {
            Text = "zmd",
            Left = 20,
            Top = 18,
            Width = 440,
            Height = 38,
            Font = new Font("Segoe UI", 18.0f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var versionLabel = CreateLabel($"当前版本：{AppInfo.Version}", 22, 66, 440);
        var authorLabel = CreateLabel("个人作者：落云", 22, 96, 440);
        var githubLink = CreateLink("GitHub: https://github.com/3436826031/zmd", "https://github.com/3436826031/zmd", 22, 126, 440);
        var giteeLink = CreateLink("Gitee: https://gitee.com/xwasdqwe/zmd", "https://gitee.com/xwasdqwe/zmd", 22, 156, 440);
        var userLabel = CreateLabel($"当前用户：{Environment.UserName}", 22, 186, 440);
        var runtimeLabel = CreateLabel($".NET：{Environment.Version}", 22, 216, 440);
        var descriptionLabel = CreateLabel("轻量 Windows 终端，面向多会话、分屏和 AI 终端工作流。", 22, 246, 440);

        okButton.Text = "确定";
        okButton.Left = 406;
        okButton.Top = 270;
        okButton.Width = 74;
        okButton.Height = 28;
        okButton.DialogResult = DialogResult.OK;
        ConfigureButton(okButton);

        AcceptButton = okButton;
        CancelButton = okButton;

        Controls.Add(titleLabel);
        Controls.Add(versionLabel);
        Controls.Add(authorLabel);
        Controls.Add(githubLink);
        Controls.Add(giteeLink);
        Controls.Add(userLabel);
        Controls.Add(runtimeLabel);
        Controls.Add(descriptionLabel);
        Controls.Add(okButton);
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

    private static LinkLabel CreateLink(string text, string url, int left, int top, int width)
    {
        var link = new LinkLabel
        {
            Text = text,
            Left = left,
            Top = top,
            Width = width,
            Height = 24,
            LinkColor = Color.FromArgb(88, 166, 255),
            ActiveLinkColor = Color.FromArgb(140, 190, 255),
            VisitedLinkColor = Color.FromArgb(88, 166, 255),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var start = text.IndexOf("https://", StringComparison.Ordinal);
        if (start >= 0)
        {
            link.Links.Add(start, url.Length, url);
        }

        link.LinkClicked += (_, e) =>
        {
            if (e.Link?.LinkData is string target)
            {
                Process.Start(new ProcessStartInfo(target)
                {
                    UseShellExecute = true
                });
            }
        };
        return link;
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
