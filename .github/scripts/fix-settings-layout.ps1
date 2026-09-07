$ErrorActionPreference = 'Stop'

$path = 'SettingsForm.cs'
$text = Get-Content $path -Raw -Encoding UTF8

function Replace-Required([string]$old, [string]$new) {
    if (-not $script:text.Contains($old)) { throw 'Required SettingsForm block not found.' }
    $script:text = $script:text.Replace($old, $new)
}

Replace-Required @'
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 9 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
'@ @'
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 10 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
'@

Replace-Required @'
        var optionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 9, 0, 0) };
        optionsPanel.Controls.Add(CreateOptionLabel("폴링 주기"));
        _pollingNumeric.Minimum = 1; _pollingNumeric.Maximum = 3600; _pollingNumeric.Width = 70;
        optionsPanel.Controls.Add(_pollingNumeric);
        optionsPanel.Controls.Add(CreateOptionLabel("초", 4, 5, 18));
        optionsPanel.Controls.Add(CreateOptionLabel("배치", 10, 5, 6));
        _layoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _layoutComboBox.Items.AddRange(["세로형", "가로형"]);
        _layoutComboBox.Width = 90;
        optionsPanel.Controls.Add(_layoutComboBox);
        optionsPanel.Controls.Add(CreateOptionLabel("배율", 10, 5, 6));
        _scaleNumeric.Minimum = 50; _scaleNumeric.Maximum = 200; _scaleNumeric.Increment = 10; _scaleNumeric.Width = 70;
        optionsPanel.Controls.Add(_scaleNumeric);
        optionsPanel.Controls.Add(CreateOptionLabel("%", 4, 5, 18));
        optionsPanel.Controls.Add(CreateOptionLabel("프로그램 배경", 4, 5, 6));
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
        _hideTitleBarWhenInactiveCheckBox.Margin = new Padding(4, 3, 8, 0);
        optionsPanel.Controls.Add(_hideTitleBarWhenInactiveCheckBox);
        _startWithWindowsCheckBox.Text = "Windows 시작 시 자동 실행";
        _startWithWindowsCheckBox.AutoSize = true;
        _startWithWindowsCheckBox.Margin = new Padding(4, 3, 0, 0);
        optionsPanel.Controls.Add(_startWithWindowsCheckBox);
        root.Controls.Add(optionsPanel, 0, 4);

        root.Controls.Add(new Label { Text = "색상은 #RRGGBB 형식으로 직접 입력합니다. 알림음은 WAV 또는 MP3 파일을 사용할 수 있습니다.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 5);
        ConfigureItemsGrid();
        root.Controls.Add(_itemsGrid, 0, 6);
'@ @'
        var displayOptionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        displayOptionsPanel.Controls.Add(CreateOptionLabel("폴링 주기"));
        _pollingNumeric.Minimum = 1; _pollingNumeric.Maximum = 3600; _pollingNumeric.Width = 70;
        displayOptionsPanel.Controls.Add(_pollingNumeric);
        displayOptionsPanel.Controls.Add(CreateOptionLabel("초", 4, 5, 18));
        displayOptionsPanel.Controls.Add(CreateOptionLabel("배치", 10, 5, 6));
        _layoutComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _layoutComboBox.Items.AddRange(["세로형", "가로형"]);
        _layoutComboBox.Width = 90;
        displayOptionsPanel.Controls.Add(_layoutComboBox);
        displayOptionsPanel.Controls.Add(CreateOptionLabel("배율", 10, 5, 6));
        _scaleNumeric.Minimum = 50; _scaleNumeric.Maximum = 200; _scaleNumeric.Increment = 10; _scaleNumeric.Width = 70;
        displayOptionsPanel.Controls.Add(_scaleNumeric);
        displayOptionsPanel.Controls.Add(CreateOptionLabel("%", 4, 5, 18));
        displayOptionsPanel.Controls.Add(CreateOptionLabel("프로그램 배경", 4, 5, 6));
        _programBackgroundTextBox.Width = 95;
        displayOptionsPanel.Controls.Add(_programBackgroundTextBox);
        root.Controls.Add(displayOptionsPanel, 0, 4);

        var behaviorOptionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        _dimWhenInactiveCheckBox.Text = "비활성 시 반투명";
        _dimWhenInactiveCheckBox.AutoSize = true;
        _dimWhenInactiveCheckBox.Margin = new Padding(0, 3, 6, 0);
        behaviorOptionsPanel.Controls.Add(_dimWhenInactiveCheckBox);
        behaviorOptionsPanel.Controls.Add(CreateOptionLabel("투명도", 4, 5, 4));
        _inactiveOpacityNumeric.Minimum = 20; _inactiveOpacityNumeric.Maximum = 100; _inactiveOpacityNumeric.Width = 60;
        behaviorOptionsPanel.Controls.Add(_inactiveOpacityNumeric);
        behaviorOptionsPanel.Controls.Add(CreateOptionLabel("%", 3, 5, 14));
        _hideTitleBarWhenInactiveCheckBox.Text = "비활성 시 타이틀바 숨김";
        _hideTitleBarWhenInactiveCheckBox.AutoSize = true;
        _hideTitleBarWhenInactiveCheckBox.Margin = new Padding(4, 3, 14, 0);
        behaviorOptionsPanel.Controls.Add(_hideTitleBarWhenInactiveCheckBox);
        _startWithWindowsCheckBox.Text = "Windows 시작 시 자동 실행";
        _startWithWindowsCheckBox.AutoSize = true;
        _startWithWindowsCheckBox.Margin = new Padding(4, 3, 0, 0);
        behaviorOptionsPanel.Controls.Add(_startWithWindowsCheckBox);
        root.Controls.Add(behaviorOptionsPanel, 0, 5);

        root.Controls.Add(new Label { Text = "색상은 #RRGGBB 형식으로 직접 입력합니다. 알림음은 WAV 또는 MP3 파일을 사용할 수 있습니다.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 6);
        ConfigureItemsGrid();
        root.Controls.Add(_itemsGrid, 0, 7);
'@

Replace-Required '        root.Controls.Add(itemButtons, 0, 7);' '        root.Controls.Add(itemButtons, 0, 8);'
Replace-Required '        root.Controls.Add(buttons, 0, 8);' '        root.Controls.Add(buttons, 0, 9);'

[System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
