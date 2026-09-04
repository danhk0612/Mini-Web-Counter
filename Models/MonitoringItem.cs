namespace JCMS_Mini_Monitoring.Models;

public sealed class MonitoringItem
{
    public string ValueName { get; set; } = "value";
    public string DisplayName { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = "#666666";
    public string TextColor { get; set; } = "#FFFFFF";
    public bool Visible { get; set; } = true;
}
