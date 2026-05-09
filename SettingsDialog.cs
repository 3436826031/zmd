namespace Zmd;

internal sealed class SettingsDialog : Form
{
    private readonly ComboBox fontFamilyInput = new();
    private readonly NumericUpDown fontSizeInput = new();
    private readonly Panel previewPanel = new();
    private readonly Label previewLabel = new();
    private readonly Button okButton = new();
    private readonly Button cancelButton = new();
    private Font? previewFont;

    public SettingsDialog(TerminalSettings settings)
    {
        Text = "Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 190);
        BackColor = Color.FromArgb(24, 24, 24);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
        Padding = new Padding(16);

        var fontFamilyLabel = CreateLabel("Font", 16, 18);
        fontFamilyInput.Left = 96;
        fontFamilyInput.Top = 14;
        fontFamilyInput.Width = 232;
        fontFamilyInput.DropDownStyle = ComboBoxStyle.DropDown;
        fontFamilyInput.FlatStyle = FlatStyle.Flat;
        fontFamilyInput.BackColor = Color.FromArgb(34, 34, 34);
        fontFamilyInput.ForeColor = Color.White;
        LoadFontFamilies(settings.FontFamily);
        fontFamilyInput.TextChanged += (_, _) => UpdatePreview();
        fontFamilyInput.SelectedIndexChanged += (_, _) => UpdatePreview();

        var fontSizeLabel = CreateLabel("Size", 16, 58);
        fontSizeInput.Left = 96;
        fontSizeInput.Top = 54;
        fontSizeInput.Width = 92;
        fontSizeInput.Minimum = 6;
        fontSizeInput.Maximum = 48;
        fontSizeInput.DecimalPlaces = 1;
        fontSizeInput.Increment = 0.5M;
        fontSizeInput.Value = (decimal)Math.Clamp(settings.FontSize, 6.0f, 48.0f);
        fontSizeInput.BackColor = Color.FromArgb(34, 34, 34);
        fontSizeInput.ForeColor = Color.White;
        fontSizeInput.ValueChanged += (_, _) => UpdatePreview();

        var previewTitle = CreateLabel("Preview", 360, 18);
        previewPanel.Left = 360;
        previewPanel.Top = 44;
        previewPanel.Width = 228;
        previewPanel.Height = 88;
        previewPanel.BackColor = Color.FromArgb(12, 12, 12);
        previewPanel.Padding = new Padding(10, 8, 10, 8);
        previewPanel.BorderStyle = BorderStyle.FixedSingle;

        previewLabel.Dock = DockStyle.Fill;
        previewLabel.BackColor = Color.FromArgb(12, 12, 12);
        previewLabel.ForeColor = Color.FromArgb(242, 242, 242);
        previewLabel.TextAlign = ContentAlignment.MiddleLeft;
        previewLabel.Text = "C:\\Users\\zmd> claude\r\n中文 AaBb 123 #$✓";
        previewPanel.Controls.Add(previewLabel);

        okButton.Text = "OK";
        okButton.Left = 434;
        okButton.Top = 146;
        okButton.Width = 74;
        okButton.Height = 28;
        okButton.DialogResult = DialogResult.OK;
        ConfigureButton(okButton);

        cancelButton.Text = "Cancel";
        cancelButton.Left = 514;
        cancelButton.Top = 146;
        cancelButton.Width = 74;
        cancelButton.Height = 28;
        cancelButton.DialogResult = DialogResult.Cancel;
        ConfigureButton(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(fontFamilyLabel);
        Controls.Add(fontFamilyInput);
        Controls.Add(fontSizeLabel);
        Controls.Add(fontSizeInput);
        Controls.Add(previewTitle);
        Controls.Add(previewPanel);
        Controls.Add(okButton);
        Controls.Add(cancelButton);
        UpdatePreview();
    }

    public string FontFamilyName => fontFamilyInput.Text.Trim().Length > 0 ? fontFamilyInput.Text.Trim() : "Consolas";

    public float FontSizeValue => (float)fontSizeInput.Value;

    private void LoadFontFamilies(string currentFamily)
    {
        foreach (var family in FontRegistry.FontFamilyNames())
        {
            fontFamilyInput.Items.Add(family);
        }

        fontFamilyInput.Text = currentFamily;
    }

    private static Label CreateLabel(string text, int left, int top)
    {
        return new Label
        {
            Text = text,
            Left = left,
            Top = top,
            Width = 72,
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

    private void UpdatePreview()
    {
        previewFont?.Dispose();
        previewFont = FontRegistry.CreateFont(FontFamilyName, FontSizeValue);
        previewLabel.Font = previewFont;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            previewFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}
