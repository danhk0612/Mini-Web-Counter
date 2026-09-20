using Microsoft.Win32;

namespace MiniWebCounter.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MiniWebCounter";
    private const string LauncherFileName = "Mini-Web-Counter.exe";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

            if (enabled)
            {
                var appDirectory = Path.GetDirectoryName(Application.ExecutablePath)
                    ?? AppContext.BaseDirectory;
                var launcherPath = Path.Combine(appDirectory, LauncherFileName);
                var executablePath = File.Exists(launcherPath)
                    ? launcherPath
                    : Application.ExecutablePath;

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
