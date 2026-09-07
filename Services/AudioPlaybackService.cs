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
