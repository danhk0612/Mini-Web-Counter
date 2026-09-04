$ErrorActionPreference = 'Stop'

function Replace-Required([string]$path, [string]$old, [string]$new) {
    $text = Get-Content $path -Raw -Encoding UTF8
    if (-not $text.Contains($old)) { throw "Required block not found in $path" }
    $text = $text.Replace($old, $new)
    [System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
}

# AppSettings.cs
Replace-Required 'Models/AppSettings.cs' @'
    public int ScalePercent { get; set; } = 100;

    public List<MonitoringItem> Items { get; set; } =
'@ @'
    public int ScalePercent { get; set; } = 100;
    public bool DimWhenInactive { get; set; } = false;
    public int InactiveOpacityPercent { get; set; } = 75;
    public bool HideTitleBarWhenInactive { get; set; } = false;

    public List<MonitoringItem> Items { get; set; } =
'@

# SettingsForm.cs - fields
Replace-Required 'SettingsForm.cs' @'
    private readonly NumericUpDown _scaleNumeric = new();
    private readonly DataGridView _itemsGrid = new();
'@ @'
    private readonly NumericUpDown _scaleNumeric = new();
    private readonly CheckBox _dimWhenInactiveCheckBox = new();
    private readonly NumericUpDown _inactiveOpacityNumeric = new();
    private readonly CheckBox _hideTitleBarWhenInactiveCheckBox = new();
    private readonly DataGridView _itemsGrid = new();
'@

# SettingsForm.cs - options UI
Replace-Required 'SettingsForm.cs' @'
        _programBackgroundTextBox.Width = 95;
        optionsPanel.Controls.Add(_programBackgroundTextBox);
        root.Controls.Add(optionsPanel, 0, 4);
'@ @'
        _programBackgroundTextBox.Width = 95;
        optionsPanel.Controls.Add(_programBackgroundTextBox);
        _dimWhenInactiveCheckBox.Text = "비활성 시 반투명";
        _dimWhenInactiveCheckBox.AutoSize = true;
        _dimWhenInactiveCheckBox.Margin = new Padding(12, 3, 6, 0);
        optionsPanel.Controls.Add(_dimWhenInactiveCheckBox);
        optionsPanel.Controls.Add(CreateOptionLabel("투명도", 4, 5, 4));
        _inactiveOpacityNumeric.Minimum = 20; _inactiveOpacityNumeric.Maximum = 100; _inactiveOpacityNumeric.Width = 60;
        optionsPanel.Controls.Add(_inactiveOpacityNumeric);
        optionsPanel.Controls.Add(CreateOptionLabel("%", 3, 5, 8));
        _hideTitleBarWhenInactiveCheckBox.Text = "비활성 시 타이틀바 숨김";
        _hideTitleBarWhenInactiveCheckBox.AutoSize = true;
        _hideTitleBarWhenInactiveCheckBox.Margin = new Padding(4, 3, 0, 0);
        optionsPanel.Controls.Add(_hideTitleBarWhenInactiveCheckBox);
        root.Controls.Add(optionsPanel, 0, 4);
'@

# SettingsForm.cs - load
Replace-Required 'SettingsForm.cs' @'
        _scaleNumeric.Value = Math.Clamp(settings.ScalePercent, 50, 200);
        _programBackgroundTextBox.Text = string.IsNullOrWhiteSpace(settings.ProgramBackgroundColor) ? "#FFFFFF" : settings.ProgramBackgroundColor;
        _itemsGrid.Rows.Clear();
'@ @'
        _scaleNumeric.Value = Math.Clamp(settings.ScalePercent, 50, 200);
        _programBackgroundTextBox.Text = string.IsNullOrWhiteSpace(settings.ProgramBackgroundColor) ? "#FFFFFF" : settings.ProgramBackgroundColor;
        _dimWhenInactiveCheckBox.Checked = settings.DimWhenInactive;
        _inactiveOpacityNumeric.Value = Math.Clamp(settings.InactiveOpacityPercent, 20, 100);
        _hideTitleBarWhenInactiveCheckBox.Checked = settings.HideTitleBarWhenInactive;
        _itemsGrid.Rows.Clear();
'@

# SettingsForm.cs - save
Replace-Required 'SettingsForm.cs' @'
            Layout = _layoutComboBox.SelectedIndex == 1 ? "Horizontal" : "Vertical",
            ScalePercent = (int)_scaleNumeric.Value,
            Items = items
'@ @'
            Layout = _layoutComboBox.SelectedIndex == 1 ? "Horizontal" : "Vertical",
            ScalePercent = (int)_scaleNumeric.Value,
            DimWhenInactive = _dimWhenInactiveCheckBox.Checked,
            InactiveOpacityPercent = (int)_inactiveOpacityNumeric.Value,
            HideTitleBarWhenInactive = _hideTitleBarWhenInactiveCheckBox.Checked,
            Items = items
'@

# SettingsForm.cs - clone
Replace-Required 'SettingsForm.cs' @'
        Layout = source.Layout,
        ScalePercent = source.ScalePercent,
        Items = (source.Items ?? []).Select(item => new MonitoringItem
'@ @'
        Layout = source.Layout,
        ScalePercent = source.ScalePercent,
        DimWhenInactive = source.DimWhenInactive,
        InactiveOpacityPercent = source.InactiveOpacityPercent,
        HideTitleBarWhenInactive = source.HideTitleBarWhenInactive,
        Items = (source.Items ?? []).Select(item => new MonitoringItem
'@

# MainForm.cs - events
Replace-Required 'MainForm.cs' @'
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
'@ @'
        Shown += MainForm_Shown;
        Activated += (_, _) => ApplyWindowActivityAppearance(false);
        Deactivate += (_, _) => ApplyWindowActivityAppearance(true);
        FormClosing += MainForm_FormClosing;
        FormClosed += MainForm_FormClosed;
'@

# MainForm.cs - settings apply
Replace-Required 'MainForm.cs' @'
        RebuildTrayItems();
        UpdateValues();
        UpdateWindowSize();
    }

    private void ApplyProgramName()
'@ @'
        RebuildTrayItems();
        UpdateValues();
        UpdateWindowSize();
        ApplyWindowActivityAppearance(!ContainsFocus);
    }

    private void ApplyWindowActivityAppearance(bool inactive)
    {
        var opacityPercent = Math.Clamp(_settings.InactiveOpacityPercent, 20, 100);
        Opacity = inactive && _settings.DimWhenInactive
            ? opacityPercent / 100D
            : 1D;

        var targetBorderStyle = inactive && _settings.HideTitleBarWhenInactive
            ? FormBorderStyle.None
            : FormBorderStyle.FixedSingle;

        if (FormBorderStyle != targetBorderStyle)
        {
            FormBorderStyle = targetBorderStyle;
            MaximizeBox = false;
            UpdateWindowSize();
        }
    }

    private void ApplyProgramName()
'@

# appsettings.json
Replace-Required 'appsettings.json' @'
  "Layout": "Vertical",
  "ScalePercent": 100,
  "Items": [
'@ @'
  "Layout": "Vertical",
  "ScalePercent": 100,
  "DimWhenInactive": false,
  "InactiveOpacityPercent": 75,
  "HideTitleBarWhenInactive": false,
  "Items": [
'@

# README.md - feature list
Replace-Required 'README.md' @'
- 전체 UI 배율 50~200%
- 항목 추가 / 삭제 및 위·아래 순서 변경
'@ @'
- 전체 UI 배율 50~200%
- 비활성 시 반투명 표시 옵션 및 투명도(20~100%) 설정
- 비활성 시 Windows 타이틀바 숨김 옵션
- 항목 추가 / 삭제 및 위·아래 순서 변경
'@

# README.md - config sample
Replace-Required 'README.md' @'
  "Layout": "Vertical",
  "ScalePercent": 100,
  "Items": [
'@ @'
  "Layout": "Vertical",
  "ScalePercent": 100,
  "DimWhenInactive": false,
  "InactiveOpacityPercent": 75,
  "HideTitleBarWhenInactive": false,
  "Items": [
'@
