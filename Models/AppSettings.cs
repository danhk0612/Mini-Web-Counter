namespace MiniWebCounter.Models;

public sealed class AppSettings
{
    public const string DefaultProgramName = "Mini Web Counter";

    public string ProgramName { get; set; } = DefaultProgramName;
    public string ProgramBackgroundColor { get; set; } = "#FFFFFF";
    public string DataUrl { get; set; } = string.Empty;
    public int PollingSeconds { get; set; } = 5;
    public string Layout { get; set; } = "Vertical";
    public int ScalePercent { get; set; } = 100;
    public bool DimWhenInactive { get; set; } = false;
    public int InactiveOpacityPercent { get; set; } = 75;
    public bool HideTitleBarWhenInactive { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;

    public List<MonitoringItem> Items { get; set; } =
    [
        new() { ValueName = "fire", DisplayName = "화재", BackgroundColor = "#D03939", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "facility", DisplayName = "설비", BackgroundColor = "#318E5A", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "fault", DisplayName = "고장", BackgroundColor = "#D98F2A", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "block", DisplayName = "차단", BackgroundColor = "#2F6FBF", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "spare", DisplayName = "예비", BackgroundColor = "#69717C", TextColor = "#FFFFFF", Visible = true }
    ];
}
