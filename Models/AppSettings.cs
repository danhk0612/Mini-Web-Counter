namespace JCMS_Mini_Monitoring.Models;

public sealed class AppSettings
{
    public string DataUrl { get; set; } = string.Empty;
    public int PollingSeconds { get; set; } = 5;
    public string Layout { get; set; } = "Vertical";
    public int ScalePercent { get; set; } = 100;

    public List<MonitoringItem> Items { get; set; } =
    [
        new() { ValueName = "fire", BackgroundColor = "#D03939", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "facility", BackgroundColor = "#318E5A", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "fault", BackgroundColor = "#D98F2A", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "block", BackgroundColor = "#2F6FBF", TextColor = "#FFFFFF", Visible = true },
        new() { ValueName = "spare", BackgroundColor = "#69717C", TextColor = "#FFFFFF", Visible = true }
    ];
}
