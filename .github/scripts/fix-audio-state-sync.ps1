$ErrorActionPreference = 'Stop'

$path = 'MainForm.cs'
$text = Get-Content $path -Raw -Encoding UTF8

function Replace-Required([string]$old, [string]$new) {
    if (-not $script:text.Contains($old)) { throw 'Required MainForm block not found.' }
    $script:text = $script:text.Replace($old, $new)
}

Replace-Required @'
            _settings = dialog.ResultSettings;
            _settingsService.Save(_settings);
            ApplySettingsToUi();
            ConfigurePolling();
            await RefreshStatusAsync();
'@ @'
            _settings = dialog.ResultSettings;
            _settingsService.Save(_settings);
            ApplySettingsToUi();
            ConfigurePolling();
            SyncAudioWithSettings();
            await RefreshStatusAsync();
'@

Replace-Required @'
        item.Visible = visible;
        _settingsService.Save(_settings);
        ApplySettingsToUi();
'@ @'
        item.Visible = visible;
        _settingsService.Save(_settings);
        ApplySettingsToUi();
        SyncAudioWithSettings();
'@

Replace-Required @'
                if (!_mutedItems.Add(item.ValueName))
                {
                    _mutedItems.Remove(item.ValueName);
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
                    PlayItemSoundIfActive(item);
                }
                else
                {
                    _audioPlaybackService.Stop(item.ValueName);
                }

                soundIcon.Invalidate();
'@

Replace-Required @'
    private void PlayActiveSounds()
    {
        foreach (var item in _settings.Items)
        {
            if (string.IsNullOrWhiteSpace(item.SoundFile) || _mutedItems.Contains(item.ValueName))
            {
                continue;
            }

            if (!_statusData.TryGetValue(item.ValueName, out var value) || value == 0)
            {
                continue;
            }

            var path = ResolveSoundPath(item.SoundFile);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                _audioPlaybackService.PlayLooping(item.ValueName, path);
            }
            catch
            {
                // 알림음 재생 실패 시 다음 폴링에서 다시 시도한다.
            }
        }
    }
'@ @'
    private void PlayActiveSounds()
    {
        foreach (var item in _settings.Items.Where(item => item.Visible))
        {
            PlayItemSoundIfActive(item);
        }
    }

    private void PlayItemSoundIfActive(MonitoringItem item)
    {
        if (!item.Visible ||
            string.IsNullOrWhiteSpace(item.SoundFile) ||
            _mutedItems.Contains(item.ValueName))
        {
            return;
        }

        if (!_statusData.TryGetValue(item.ValueName, out var value) || value == 0)
        {
            return;
        }

        var path = ResolveSoundPath(item.SoundFile);
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            _audioPlaybackService.PlayLooping(item.ValueName, path);
        }
        catch
        {
            // 알림음 재생 실패 시 다음 폴링에서 다시 시도한다.
        }
    }

    private void SyncAudioWithSettings()
    {
        var configuredKeys = _settings.Items
            .Select(item => item.ValueName)
            .ToHashSet(StringComparer.Ordinal);
        _mutedItems.RemoveWhere(valueName => !configuredKeys.Contains(valueName));

        _audioPlaybackService.StopAll();

        if (string.IsNullOrWhiteSpace(_settings.DataUrl))
        {
            return;
        }

        PlayActiveSounds();
    }
'@

Replace-Required @'
        if (!string.IsNullOrWhiteSpace(_settings.DataUrl))
        {
            _pollingTimer.Start();
        }
'@ @'
        if (!string.IsNullOrWhiteSpace(_settings.DataUrl))
        {
            _pollingTimer.Start();
        }
        else
        {
            _audioPlaybackService.StopAll();
        }
'@

[System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
