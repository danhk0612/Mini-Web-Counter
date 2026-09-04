using JCMS_Mini_Monitoring.Models;
using JCMS_Mini_Monitoring.Services;

namespace JCMS_Mini_Monitoring;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var singleInstanceMutex = new Mutex(true, "JCMS_Mini_Monitoring_SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            var settings = new SettingsService().Load();
            var programName = string.IsNullOrWhiteSpace(settings.ProgramName)
                ? AppSettings.DefaultProgramName
                : settings.ProgramName.Trim();

            MessageBox.Show(
                "이미 실행 중입니다.",
                programName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new MainForm());
    }
}
