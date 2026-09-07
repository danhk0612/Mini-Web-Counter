$ErrorActionPreference = 'Stop'

function Replace-Required([string]$path, [string]$old, [string]$new) {
    $text = Get-Content $path -Raw -Encoding UTF8
    if (-not $text.Contains($old)) { throw "Required block not found in $path" }
    $text = $text.Replace($old, $new)
    [System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
}

# Add NAudio dependency.
Replace-Required 'Mini-Web-Counter.csproj' @'
  <ItemGroup>
    <None Update="appsettings.json">
'@ @'
  <ItemGroup>
    <PackageReference Include="NAudio" Version="2.2.1" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
'@

# Settings models.
Replace-Required 'Models/AppSettings.cs' @'
    public bool StartWithWindows { get; set; } = false;

    public List<MonitoringItem> Items { get; set; } =
'@ @'
    public bool StartWithWindows { get; set; } = false;
    public int MasterVolumePercent { get; set; } = 100;
    public string AudioDeviceId { get; set; } = string.Empty;

    public List<MonitoringItem> Items { get; set; } =
'@

Replace-Required 'Models/MonitoringItem.cs' @'
    public string SoundFile { get; set; } = string.Empty;
    public bool Visible { get; set; } = true;
'@ @'
    public string SoundFile { get; set; } = string.Empty;
    public int VolumePercent { get; set; } = 100;
    public bool Visible { get; set; } = true;
'@

# Replace the audio engine with NAudio/WASAPI. Unchanged key+path+device keeps playing; volume updates are live.
@'
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace MiniWebCounter.Services;

public sealed record AudioOutputDevice(string Id, string Name);
public sealed record AudioPlaybackRequest(string FilePath, int VolumePercent);

public sealed class AudioPlaybackService : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, PlaybackEntry> _playbacks = new(StringComparer.Ordinal);
    private MMDevice? _activeDevice;
    private string? _activeDeviceId;

    public static IReadOnlyList<AudioOutputDevice> GetOutputDevices()
    {
        var devices = new List<AudioOutputDevice>
        {
            new(string.Empty, "기본 장치")
        };

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var endpoint in endpoints)
            {
                try
                {
                    devices.Add(new AudioOutputDevice(endpoint.ID, endpoint.FriendlyName));
                }
                finally
                {
                    endpoint.Dispose();
                }
            }
        }
        catch
        {
            // 장치 열거 실패 시에도 기본 장치 선택지는 유지한다.
        }

        return devices;
    }

    public void SynchronizeLooping(
        IReadOnlyDictionary<string, AudioPlaybackRequest> desired,
        string? requestedDeviceId,
        int masterVolumePercent)
    {
        lock (_syncRoot)
        {
            var resolvedDevice = ResolveDevice(requestedDeviceId);
            if (resolvedDevice is null)
            {
                StopAllCore();
                ReleaseActiveDevice();
                return;
            }

            if (!string.Equals(_activeDeviceId, resolvedDevice.ID, StringComparison.Ordinal))
            {
                StopAllCore();
                ReleaseActiveDevice();
                _activeDevice = resolvedDevice;
                _activeDeviceId = resolvedDevice.ID;
            }
            else
            {
                resolvedDevice.Dispose();
            }

            foreach (var key in _playbacks.Keys.Where(key => !desired.ContainsKey(key)).ToArray())
            {
                StopCore(key);
            }

            foreach (var pair in desired)
            {
                var request = pair.Value;
                if (string.IsNullOrWhiteSpace(request.FilePath) || !File.Exists(request.FilePath))
                {
                    StopCore(pair.Key);
                    continue;
                }

                var normalizedPath = Path.GetFullPath(request.FilePath);
                var volume = CalculateVolume(masterVolumePercent, request.VolumePercent);

                if (_playbacks.TryGetValue(pair.Key, out var current) &&
                    string.Equals(current.FilePath, normalizedPath, StringComparison.OrdinalIgnoreCase) &&
                    current.Output.PlaybackState == PlaybackState.Playing)
                {
                    current.Reader.Volume = volume;
                    continue;
                }

                StopCore(pair.Key);
                StartCore(pair.Key, normalizedPath, volume);
            }
        }
    }

    public void Stop(string key)
    {
        lock (_syncRoot)
        {
            StopCore(key);
        }
    }

    public void StopAll()
    {
        lock (_syncRoot)
        {
            StopAllCore();
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            StopAllCore();
            ReleaseActiveDevice();
        }
    }

    private void StartCore(string key, string filePath, float volume)
    {
        if (_activeDevice is null)
        {
            return;
        }

        AudioFileReader? reader = null;
        LoopStream? loop = null;
        WasapiOut? output = null;

        try
        {
            reader = new AudioFileReader(filePath) { Volume = volume };
            loop = new LoopStream(reader);
            output = new WasapiOut(_activeDevice, AudioClientShareMode.Shared, false, 100);
            output.Init(loop);
            output.Play();
            _playbacks[key] = new PlaybackEntry(filePath, reader, loop, output);
        }
        catch
        {
            try { output?.Dispose(); } catch { }
            try { loop?.Dispose(); } catch { }
            if (loop is null)
            {
                try { reader?.Dispose(); } catch { }
            }
        }
    }

    private void StopCore(string key)
    {
        if (!_playbacks.Remove(key, out var entry))
        {
            return;
        }

        try { entry.Output.Stop(); } catch { }
        try { entry.Output.Dispose(); } catch { }
        try { entry.Loop.Dispose(); } catch { }
    }

    private void StopAllCore()
    {
        foreach (var key in _playbacks.Keys.ToArray())
        {
            StopCore(key);
        }
    }

    private void ReleaseActiveDevice()
    {
        try { _activeDevice?.Dispose(); } catch { }
        _activeDevice = null;
        _activeDeviceId = null;
    }

    private static MMDevice? ResolveDevice(string? requestedDeviceId)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();

            if (!string.IsNullOrWhiteSpace(requestedDeviceId))
            {
                try
                {
                    var selected = enumerator.GetDevice(requestedDeviceId);
                    if (selected.State == DeviceState.Active)
                    {
                        return selected;
                    }

                    selected.Dispose();
                }
                catch
                {
                    // 저장된 장치를 찾지 못하면 아래에서 기본 장치로 fallback한다.
                }
            }

            return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch
        {
            return null;
        }
    }

    private static float CalculateVolume(int masterVolumePercent, int itemVolumePercent)
    {
        var master = Math.Clamp(masterVolumePercent, 0, 100) / 100F;
        var item = Math.Clamp(itemVolumePercent, 0, 100) / 100F;
        return master * item;
    }

    private sealed record PlaybackEntry(
        string FilePath,
        AudioFileReader Reader,
        LoopStream Loop,
        WasapiOut Output);

    private sealed class LoopStream : WaveStream
    {
        private readonly WaveStream _source;

        public LoopStream(WaveStream source)
        {
            _source = source;
        }

        public override WaveFormat WaveFormat => _source.WaveFormat;
        public override long Length => long.MaxValue;
        public override long Position
        {
            get => _source.Position;
            set => _source.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = _source.Read(buffer, offset + totalRead, count - totalRead);
                if (read > 0)
                {
                    totalRead += read;
                    continue;
                }

                if (_source.Position == 0 || _source.Length == 0)
                {
                    break;
                }

                _source.Position = 0;
            }

            return totalRead;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _source.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
'@ | Set-Content 'Services/AudioPlaybackService.cs' -Encoding UTF8

# MainForm: desired audio now includes per-item volume and selected output device/master volume.
$main = Get-Content 'MainForm.cs' -Raw -Encoding UTF8
$oldSyncStart = '        var desired = new Dictionary<string, string>(StringComparer.Ordinal);'
$oldAssign = '                    desired[item.ValueName] = path;'
$oldCall = '        _audioPlaybackService.SynchronizeLooping(desired);'
if (-not $main.Contains($oldSyncStart) -or -not $main.Contains($oldAssign) -or -not $main.Contains($oldCall)) {
    throw 'Current audio synchronization block not found in MainForm.cs'
}
$main = $main.Replace($oldSyncStart, '        var desired = new Dictionary<string, AudioPlaybackRequest>(StringComparer.Ordinal);')
$main = $main.Replace($oldAssign, '                    desired[item.ValueName] = new AudioPlaybackRequest(path, Math.Clamp(item.VolumePercent, 0, 100));')
$main = $main.Replace($oldCall, '        _audioPlaybackService.SynchronizeLooping(desired, _settings.AudioDeviceId, _settings.MasterVolumePercent);')
[System.IO.File]::WriteAllText((Resolve-Path 'MainForm.cs'), $main, [System.Text.UTF8Encoding]::new($false))

# SettingsForm fields.
Replace-Required 'SettingsForm.cs' @'
    private readonly CheckBox _startWithWindowsCheckBox = new();
    private readonly DataGridView _itemsGrid = new();
'@ @'
    private readonly CheckBox _startWithWindowsCheckBox = new();
    private readonly NumericUpDown _masterVolumeNumeric = new();
    private readonly ComboBox _audioDeviceComboBox = new();
    private readonly Button _refreshAudioDevicesButton = new();
    private readonly DataGridView _itemsGrid = new();
'@

Replace-Required 'SettingsForm.cs' '        ClientSize = new Size(1160, 620);' '        ClientSize = new Size(1160, 660);'

Replace-Required 'SettingsForm.cs' @'
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
'@ @'
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 11 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
'@

Replace-Required 'SettingsForm.cs' @'
        root.Controls.Add(behaviorOptionsPanel, 0, 5);

        root.Controls.Add(new Label { Text = "색상은 #RRGGBB 형식으로 직접 입력합니다. 알림음은 WAV 또는 MP3 파일을 사용할 수 있습니다.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 6);
        ConfigureItemsGrid();
        root.Controls.Add(_itemsGrid, 0, 7);
'@ @'
        root.Controls.Add(behaviorOptionsPanel, 0, 5);

        var audioOptionsPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        audioOptionsPanel.Controls.Add(CreateOptionLabel("전체 볼륨"));
        _masterVolumeNumeric.Minimum = 0; _masterVolumeNumeric.Maximum = 100; _masterVolumeNumeric.Width = 70;
        audioOptionsPanel.Controls.Add(_masterVolumeNumeric);
        audioOptionsPanel.Controls.Add(CreateOptionLabel("%", 3, 5, 18));
        audioOptionsPanel.Controls.Add(CreateOptionLabel("출력 장치", 4, 5, 6));
        _audioDeviceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _audioDeviceComboBox.Width = 430;
        audioOptionsPanel.Controls.Add(_audioDeviceComboBox);
        _refreshAudioDevicesButton.Text = "새로고침";
        _refreshAudioDevicesButton.Width = 80;
        _refreshAudioDevicesButton.Height = 25;
        _refreshAudioDevicesButton.Margin = new Padding(6, 0, 0, 0);
        _refreshAudioDevicesButton.Click += (_, _) => RefreshAudioDevices();
        audioOptionsPanel.Controls.Add(_refreshAudioDevicesButton);
        root.Controls.Add(audioOptionsPanel, 0, 6);

        root.Controls.Add(new Label { Text = "색상은 #RRGGBB 형식으로 직접 입력합니다. 알림음은 WAV 또는 MP3 파일을 사용할 수 있으며 항목별 볼륨은 0~100%입니다.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 7);
        ConfigureItemsGrid();
        root.Controls.Add(_itemsGrid, 0, 8);
'@

Replace-Required 'SettingsForm.cs' '        root.Controls.Add(itemButtons, 0, 8);' '        root.Controls.Add(itemButtons, 0, 9);'
Replace-Required 'SettingsForm.cs' '        root.Controls.Add(buttons, 0, 9);' '        root.Controls.Add(buttons, 0, 10);'

# Grid: per-item volume.
Replace-Required 'SettingsForm.cs' @'
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LinkUrl", HeaderText = "링크 URL", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 190 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoundFile", HeaderText = "알림음 파일", Width = 250 });
'@ @'
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LinkUrl", HeaderText = "링크 URL", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 150 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "VolumePercent", HeaderText = "볼륨 %", Width = 70 });
        _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SoundFile", HeaderText = "알림음 파일", Width = 210 });
'@

# Load audio settings and devices.
Replace-Required 'SettingsForm.cs' @'
        _startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        _itemsGrid.Rows.Clear();
'@ @'
        _startWithWindowsCheckBox.Checked = settings.StartWithWindows;
        _masterVolumeNumeric.Value = Math.Clamp(settings.MasterVolumePercent, 0, 100);
        RefreshAudioDevices(settings.AudioDeviceId);
        _itemsGrid.Rows.Clear();
'@

# Add row volume value.
Replace-Required 'SettingsForm.cs' @'
        _itemsGrid.Rows.Add(item.Visible, displayName, item.ValueName, item.BackgroundColor, item.TextColor, item.LinkUrl, item.SoundFile, "...");
'@ @'
        _itemsGrid.Rows.Add(item.Visible, displayName, item.ValueName, item.BackgroundColor, item.TextColor, item.LinkUrl, Math.Clamp(item.VolumePercent, 0, 100), item.SoundFile, "...");
'@

# Add device refresh helper before AddItemRow.
Replace-Required 'SettingsForm.cs' @'
    private void AddItemRow()
'@ @'
    private void RefreshAudioDevices(string? requestedDeviceId = null)
    {
        var selectedId = requestedDeviceId;
        if (selectedId is null && _audioDeviceComboBox.SelectedItem is AudioOutputDevice selected)
        {
            selectedId = selected.Id;
        }

        selectedId ??= string.Empty;
        var devices = AudioPlaybackService.GetOutputDevices().ToList();
        if (!string.IsNullOrWhiteSpace(selectedId) &&
            devices.All(device => !string.Equals(device.Id, selectedId, StringComparison.Ordinal)))
        {
            devices.Add(new AudioOutputDevice(selectedId, "저장된 출력 장치를 찾을 수 없음 (기본 장치로 대체됨)"));
        }

        _audioDeviceComboBox.DataSource = null;
        _audioDeviceComboBox.DisplayMember = nameof(AudioOutputDevice.Name);
        _audioDeviceComboBox.ValueMember = nameof(AudioOutputDevice.Id);
        _audioDeviceComboBox.DataSource = devices;
        _audioDeviceComboBox.SelectedValue = selectedId;
        if (_audioDeviceComboBox.SelectedIndex < 0 && devices.Count > 0)
        {
            _audioDeviceComboBox.SelectedIndex = 0;
        }
    }

    private void AddItemRow()
'@

# Validate/save per-item volume and global audio settings.
Replace-Required 'SettingsForm.cs' @'
            var displayName = (Convert.ToString(row.Cells["DisplayName"].Value) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(displayName)) displayName = valueName;
            items.Add(new MonitoringItem
'@ @'
            var displayName = (Convert.ToString(row.Cells["DisplayName"].Value) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(displayName)) displayName = valueName;
            if (!int.TryParse(Convert.ToString(row.Cells["VolumePercent"].Value), out var itemVolume) || itemVolume < 0 || itemVolume > 100)
            {
                MessageBox.Show(this, $"항목별 볼륨은 0~100 사이의 숫자여야 합니다: {displayName}", "설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            items.Add(new MonitoringItem
'@

Replace-Required 'SettingsForm.cs' @'
                LinkUrl = (Convert.ToString(row.Cells["LinkUrl"].Value) ?? string.Empty).Trim(),
                SoundFile = (Convert.ToString(row.Cells["SoundFile"].Value) ?? string.Empty).Trim()
'@ @'
                LinkUrl = (Convert.ToString(row.Cells["LinkUrl"].Value) ?? string.Empty).Trim(),
                VolumePercent = itemVolume,
                SoundFile = (Convert.ToString(row.Cells["SoundFile"].Value) ?? string.Empty).Trim()
'@

Replace-Required 'SettingsForm.cs' @'
            StartWithWindows = _startWithWindowsCheckBox.Checked,
            Items = items
'@ @'
            StartWithWindows = _startWithWindowsCheckBox.Checked,
            MasterVolumePercent = (int)_masterVolumeNumeric.Value,
            AudioDeviceId = (_audioDeviceComboBox.SelectedItem as AudioOutputDevice)?.Id ?? string.Empty,
            Items = items
'@

# Clone settings includes audio controls.
Replace-Required 'SettingsForm.cs' @'
        StartWithWindows = source.StartWithWindows,
        Items = (source.Items ?? []).Select(item => new MonitoringItem
'@ @'
        StartWithWindows = source.StartWithWindows,
        MasterVolumePercent = source.MasterVolumePercent,
        AudioDeviceId = source.AudioDeviceId,
        Items = (source.Items ?? []).Select(item => new MonitoringItem
'@

Replace-Required 'SettingsForm.cs' @'
            SoundFile = item.SoundFile,
            Visible = item.Visible
'@ @'
            SoundFile = item.SoundFile,
            VolumePercent = item.VolumePercent,
            Visible = item.Visible
'@

# Default JSON config.
Replace-Required 'appsettings.json' @'
  "StartWithWindows": false,
  "Items": [
'@ @'
  "StartWithWindows": false,
  "MasterVolumePercent": 100,
  "AudioDeviceId": "",
  "Items": [
'@

$json = Get-Content 'appsettings.json' -Raw -Encoding UTF8
$json = $json.Replace('"SoundFile": "", "Visible": true', '"SoundFile": "", "VolumePercent": 100, "Visible": true')
[System.IO.File]::WriteAllText((Resolve-Path 'appsettings.json'), $json, [System.Text.UTF8Encoding]::new($false))
