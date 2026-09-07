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
