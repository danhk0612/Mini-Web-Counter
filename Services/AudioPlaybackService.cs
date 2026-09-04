using System.Runtime.InteropServices;
using System.Text;

namespace JCMS_Mini_Monitoring.Services;

public sealed class AudioPlaybackService : IDisposable
{
    private readonly HashSet<string> _openAliases = new(StringComparer.Ordinal);

    public void Play(string key, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        var alias = CreateAlias(key);
        CloseAlias(alias);

        var extension = Path.GetExtension(filePath);
        var deviceType = string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase)
            ? "waveaudio"
            : "mpegvideo";

        var escapedPath = filePath.Replace("\"", "\"\"");
        if (SendCommand($"open \"{escapedPath}\" type {deviceType} alias {alias}") != 0)
        {
            return;
        }

        _openAliases.Add(alias);
        SendCommand($"play {alias} from 0");
    }

    public void Stop(string key)
    {
        CloseAlias(CreateAlias(key));
    }

    public void Dispose()
    {
        foreach (var alias in _openAliases.ToArray())
        {
            CloseAlias(alias);
        }
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
        return $"jcms_{unchecked((uint)hash):X8}";
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
