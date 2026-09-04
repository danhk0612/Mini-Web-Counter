using JCMS_Mini_Monitoring.Models;

namespace JCMS_Mini_Monitoring;

public sealed class SettingsForm : Form
{
    private readonly TextBox _urlTextBox = new();
    private readonly NumericUpDown _pollingNumeric = new();
    private readonly CheckBox _fireCheckBox = new();
    private readonly CheckBox _facilityCheckBox = new();
    private readonly CheckBox _faultCheckBox = new();
    private readonly CheckBox _blockCheckBox = new();
    private readonly CheckBox _spareCheckBox = new();

    public AppSettings ResultSettings { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        ResultSettings = CloneSettings(settings);

        Text = "설정";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(430, 330);
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        LoadSettings(settings);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 6,
            AutoSize = false
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "데이터 URL",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _urlTextBox.Dock = DockStyle.Fill;
        root.Controls.Add(_urlTextBox, 0, 1);

        var pollingPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 7, 0, 0)
        };
        pollingPanel.Controls.Add(new Label
        {
            Text = "폴링 주기",
            AutoSize = true,
            Margin = new Padding(0, 5, 8, 0)
        });

        _pollingNumeric.Minimum = 1;
        _pollingNumeric.Maximum = 3600;
        _pollingNumeric.Width = 80;
        pollingPanel.Controls.Add(_pollingNumeric);
        pollingPanel.Controls.Add(new Label
        {
            Text = "초",
            AutoSize = true,
            Margin = new Padding(6, 5, 0, 0)
        });
        root.Controls.Add(pollingPanel, 0, 2);

        root.Controls.Add(new Label
        {
            Text = "표시 항목",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 3);

        var checks = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        ConfigureCheckBox(_fireCheckBox, "화재");
        ConfigureCheckBox(_facilityCheckBox, "설비");
        ConfigureCheckBox(_faultCheckBox, "고장");
        ConfigureCheckBox(_blockCheckBox, "차단");
        ConfigureCheckBox(_spareCheckBox, "예비");

        checks.Controls.AddRange([
            _fireCheckBox,
            _facilityCheckBox,
            _faultCheckBox,
            _blockCheckBox,
            _spareCheckBox
        ]);
        root.Controls.Add(checks, 0, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 7, 0, 0)
        };

        var cancelButton = new Button
        {
            Text = "취소",
            DialogResult = DialogResult.Cancel,
            Width = 80
        };

        var saveButton = new Button
        {
            Text = "저장",
            Width = 80
        };
        saveButton.Click += SaveButton_Click;

        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        root.Controls.Add(buttons, 0, 5);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private static void ConfigureCheckBox(CheckBox checkBox, string text)
    {
        checkBox.Text = text;
        checkBox.AutoSize = true;
        checkBox.Margin = new Padding(0, 4, 0, 4);
    }

    private void LoadSettings(AppSettings settings)
    {
        _urlTextBox.Text = settings.DataUrl;
        _pollingNumeric.Value = Math.Clamp(settings.PollingSeconds, 1, 3600);
        _fireCheckBox.Checked = settings.ShowFire;
        _facilityCheckBox.Checked = settings.ShowFacility;
        _faultCheckBox.Checked = settings.ShowFault;
        _blockCheckBox.Checked = settings.ShowBlock;
        _spareCheckBox.Checked = settings.ShowSpare;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        ResultSettings = new AppSettings
        {
            DataUrl = _urlTextBox.Text.Trim(),
            PollingSeconds = (int)_pollingNumeric.Value,
            ShowFire = _fireCheckBox.Checked,
            ShowFacility = _facilityCheckBox.Checked,
            ShowFault = _faultCheckBox.Checked,
            ShowBlock = _blockCheckBox.Checked,
            ShowSpare = _spareCheckBox.Checked
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static AppSettings CloneSettings(AppSettings source)
    {
        return new AppSettings
        {
            DataUrl = source.DataUrl,
            PollingSeconds = source.PollingSeconds,
            ShowFire = source.ShowFire,
            ShowFacility = source.ShowFacility,
            ShowFault = source.ShowFault,
            ShowBlock = source.ShowBlock,
            ShowSpare = source.ShowSpare
        };
    }
}
