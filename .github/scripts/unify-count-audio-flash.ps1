$ErrorActionPreference = 'Stop'

function Replace-Required([string]$path, [string]$old, [string]$new) {
    $text = Get-Content $path -Raw -Encoding UTF8
    if (-not $text.Contains($old)) { throw "Required block not found in $path" }
    $text = $text.Replace($old, $new)
    [System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
}

# AudioPlaybackService: make looping idempotent and synchronize desired states without restarting unchanged audio.
@'
using System.Runtime.InteropServices;
using System.Text;

namespace MiniWebCounter.Services;

public sealed class AudioPlaybackService : IDisposable
{
    private readonly HashSet<string> _openAliases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _playingPaths = new(StringComparer.Ordinal);

    public void PlayLooping(string key, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Stop(key);
            return;
        }

        var normalizedPath = Path.GetFullPath(filePath);
        if (_playingPaths.TryGetValue(key, out var currentPath) &&
            string.Equals(currentPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Stop(key);

        var alias = CreateAlias(key);
        var extension = Path.GetExtension(normalizedPath);
        var deviceType = string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase)
            ? "waveaudio"
            : "mpegvideo";

        var escapedPath = normalizedPath.Replace("\"", "\"\"");
        if (SendCommand($"open \"{escapedPath}\" type {deviceType} alias {alias}") != 0)
        {
            return;
        }

        _openAliases.Add(alias);
        if (SendCommand($"play {alias} from 0 repeat") != 0)
        {
            CloseAlias(alias);
            return;
        }

        _playingPaths[key] = normalizedPath;
    }

    public void SynchronizeLooping(IReadOnlyDictionary<string, string> desired)
    {
        foreach (var key in _playingPaths.Keys.Where(key => !desired.ContainsKey(key)).ToArray())
        {
            Stop(key);
        }

        foreach (var pair in desired)
        {
            PlayLooping(pair.Key, pair.Value);
        }
    }

    public void Stop(string key)
    {
        _playingPaths.Remove(key);
        CloseAlias(CreateAlias(key));
    }

    public void StopAll()
    {
        _playingPaths.Clear();
        foreach (var alias in _openAliases.ToArray())
        {
            CloseAlias(alias);
        }
    }

    public void Dispose()
    {
        StopAll();
    }

    private void CloseAlias(string alias)
    {
        if (_openAliases.Remove(alias))
        {
            SendCommand($"stop {alias}");
            SendCommand($"close {alias}");
        }
    }

    private static string CreateAlias(string key)
    {
        var hash = StringComparer.Ordinal.GetHashCode(key);
        return $"mwc_{unchecked((uint)hash):X8}";
    }

    private static int SendCommand(string command)
    {
        var buffer = new StringBuilder(256);
        return mciSendString(command, buffer, buffer.Capacity, IntPtr.Zero);
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(
        string command,
        StringBuilder returnValue,
        int returnLength,
        IntPtr callback);
}
'@ | Set-Content 'Services/AudioPlaybackService.cs' -Encoding UTF8

# Mute button: always synchronize desired audio after toggling.
Replace-Required 'MainForm.cs' @'
                if (!_mutedItems.Add(item.ValueName))
                {
                    _mutedItems.Remove(item.ValueName);
                    PlayItemSoundIfActive(item);
                }
                else
                {
                    _audioPlaybackService.Stop(item.ValueName);
                }

                soundIcon.Invalidate();
'@ @'
                if (!_mutedItems.Add(item.ValueName))
                {
                    _mutedItems.Remove(item.ValueName);
                }

                SyncAudioWithSettings();
                soundIcon.Invalidate();
'@

# Value transitions: no flash when current value is zero; flash for 0->positive and positive->different-positive.
Replace-Required 'MainForm.cs' @'
            if (_lastValues.TryGetValue(pair.Key, out var previousValue) && previousValue != value)
            {
                card.IsFlashing = true;
                card.FlashUntilUtc = DateTime.UtcNow.AddSeconds(1);
                hasActiveFlash = true;
            }

            _lastValues[pair.Key] = value;
            ApplyCardAppearance(card, false);
'@ @'
            if (_lastValues.TryGetValue(pair.Key, out var previousValue) &&
                previousValue != value &&
                value > 0)
            {
                card.IsFlashing = true;
                card.FlashUntilUtc = DateTime.UtcNow.AddSeconds(1);
                hasActiveFlash = true;
            }
            else if (value == 0)
            {
                card.IsFlashing = false;
            }

            _lastValues[pair.Key] = value;
            ApplyCardAppearance(card, false);
'@

# Replace old audio helpers with desired-state synchronizer.
$main = Get-Content 'MainForm.cs' -Raw -Encoding UTF8
$startMarker = '    private void PlayActiveSounds()'
$endMarker = '    private static string ResolveSoundPath(string soundFile)'
$start = $main.IndexOf($startMarker)
$end = $main.IndexOf($endMarker)
if ($start -lt 0 -or $end -le $start) { throw 'Audio helper region not found in MainForm.cs' }
$newRegion = @'
    private void SyncAudioWithSettings()
    {
        var configuredKeys = _settings.Items
            .Select(item => item.ValueName)
            .ToHashSet(StringComparer.Ordinal);
        _mutedItems.RemoveWhere(valueName => !configuredKeys.Contains(valueName));

        var desired = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(_settings.DataUrl))
        {
            foreach (var item in _settings.Items)
            {
                if (!item.Visible ||
                    string.IsNullOrWhiteSpace(item.SoundFile) ||
                    _mutedItems.Contains(item.ValueName) ||
                    !_statusData.TryGetValue(item.ValueName, out var value) ||
                    value <= 0)
                {
                    continue;
                }

                var path = ResolveSoundPath(item.SoundFile);
                if (File.Exists(path))
                {
                    desired[item.ValueName] = path;
                }
            }
        }

        _audioPlaybackService.SynchronizeLooping(desired);
    }

'@
$main = $main.Substring(0, $start) + $newRegion + $main.Substring($end)
[System.IO.File]::WriteAllText((Resolve-Path 'MainForm.cs'), $main, [System.Text.UTF8Encoding]::new($false))

# Poll success: do not StopAll/restart. Update values first, then reconcile audio continuously.
Replace-Required 'MainForm.cs' @'
            _statusData = data;
            _audioPlaybackService.StopAll();
            UpdateValues();
            PlayActiveSounds();
'@ @'
            _statusData = data;
            UpdateValues();
            SyncAudioWithSettings();
'@
