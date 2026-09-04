$ErrorActionPreference = 'Stop'

function Replace-Required([string]$path, [string]$old, [string]$new) {
    $text = Get-Content $path -Raw -Encoding UTF8
    if (-not $text.Contains($old)) { throw "Required block not found in $path" }
    $text = $text.Replace($old, $new)
    [System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
}

# AppSettings
Replace-Required 'Models/AppSettings.cs' @'
    public bool HideTitleBarWhenInactive { get; set; } = false;

    public List<MonitoringItem> Items { get; set; } =
'@ @'
    public bool HideTitleBarWhenInactive { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;

    public List<MonitoringItem> Items { get; set; } =
'@

# StartupService
@'
using Microsoft.Win32;

namespace MiniWebCounter.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MiniWebCounter";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (enabled)
            {
                var executablePath = Application.ExecutablePath;
                key.SetValue(ValueName, $"\"{executablePath}\"", RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // 시작프로그램 등록 실패가 프로그램 실행을 방해하지 않도록 한다.
        }
    }
}
'@ | Set-Content 'Services/StartupService.cs' -Encoding UTF8

# SettingsForm fields
Replace-Required 'SettingsForm.cs' @'
    private readonly CheckBox _hideTitleBarWhenInactiveCheckBox = new();
    private readonly DataGridView _itemsGrid = new();
'@ @'
    private readonly CheckBox _hideTitleBarWhenInactiveCheckBox = new();
    private readonly CheckBox _startWithWindowsCheckBox = new();
    private readonly DataGridView _itemsGrid = new();
'@

# SettingsForm UI
Replace-Required 'SettingsForm.cs' @'
        _hideTitleBarWhenInactiveCheckBox.Margin = new Padding(4, 3, 0, 0);
        optionsPanel.Controls.Add(_hideTitleBarWhenInactiveCheckBox);
        root.Controls.Add(optionsPanel, 0, 4);
'@ @'
        _hideTitleBarWhenInactiveCheckBox.Margin = new Padding(4, 3, 8, 0);
        optionsPanel.Controls.Add(_hideTitleBarWhenInactiveCheckBox);
        _startWithWindowsCheckBox.Text = "Windows 시작 시 자동 실행";
        _startWithWindowsCheckBox.AutoSize = true;
        _startWithWindowsCheckBox.Margin = new Padding(4, 3, 0, 0);
        optionsPanel.Controls.Add(_startWithWindowsCheckBox);
        root.Controls.Add(optionsPanel, 0, 4);
'@

# SettingsForm load
Replace-Required 'SettingsForm.cs' @'
        _hideTitleBarWhenInactiveCheckBox.Checked = settings.HideTitleBarWhenInactive;
        _itemsGrid.Rows.Clear();
'@ @'
        _hideTitleBarWhenInactiveCheckBox.Checked = settings.HideTitleBarWhenInactive;
        _startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        _itemsGrid.Rows.Clear();
'@

# SettingsForm save
Replace-Required 'SettingsForm.cs' @'
            HideTitleBarWhenInactive = _hideTitleBarWhenInactiveCheckBox.Checked,
            Items = items
'@ @'
            HideTitleBarWhenInactive = _hideTitleBarWhenInactiveCheckBox.Checked,
            StartWithWindows = _startWithWindowsCheckBox.Checked,
            Items = items
'@

Replace-Required 'SettingsForm.cs' @'
        DialogResult = DialogResult.OK;
        Close();
'@ @'
        StartupService.Apply(ResultSettings.StartWithWindows);
        DialogResult = DialogResult.OK;
        Close();
'@

Replace-Required 'SettingsForm.cs' 'using MiniWebCounter.Models;' "using MiniWebCounter.Models;`nusing MiniWebCounter.Services;"

# SettingsForm clone
Replace-Required 'SettingsForm.cs' @'
        HideTitleBarWhenInactive = source.HideTitleBarWhenInactive,
        Items = (source.Items ?? []).Select(item => new MonitoringItem
'@ @'
        HideTitleBarWhenInactive = source.HideTitleBarWhenInactive,
        StartWithWindows = source.StartWithWindows,
        Items = (source.Items ?? []).Select(item => new MonitoringItem
'@

# Program startup sync
Replace-Required 'Program.cs' @'
        Application.Run(new MainForm());
'@ @'
        var startupSettings = new SettingsService().Load();
        StartupService.Apply(startupSettings.StartWithWindows);

        Application.Run(new MainForm());
'@

# appsettings sample
Replace-Required 'appsettings.json' @'
  "HideTitleBarWhenInactive": false,
  "Items": [
'@ @'
  "HideTitleBarWhenInactive": false,
  "StartWithWindows": false,
  "Items": [
'@

# README
Replace-Required 'README.md' @'
- 작업표시줄에는 표시하지 않고 시스템 트레이에만 상주
- 설정은 트레이 메뉴에서 접근
'@ @'
- 작업표시줄에는 표시하지 않고 시스템 트레이에만 상주
- Windows 시작 시 자동 실행 ON/OFF 설정
- 설정은 트레이 메뉴에서 접근
'@

Replace-Required 'README.md' @'
  "HideTitleBarWhenInactive": false,
  "Items": [
'@ @'
  "HideTitleBarWhenInactive": false,
  "StartWithWindows": false,
  "Items": [
'@
