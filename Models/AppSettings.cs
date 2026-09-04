namespace JCMS_Mini_Monitoring.Models;

public sealed class AppSettings
{
    public string DataUrl { get; set; } = string.Empty;
    public int PollingSeconds { get; set; } = 5;
    public bool ShowFire { get; set; } = true;
    public bool ShowFacility { get; set; } = true;
    public bool ShowFault { get; set; } = true;
    public bool ShowBlock { get; set; } = true;
    public bool ShowSpare { get; set; } = true;
}
