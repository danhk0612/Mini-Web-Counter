using System.Text.Json.Serialization;

namespace JCMS_Mini_Monitoring.Models;

public sealed class StatusData
{
    [JsonPropertyName("fire")]
    public int Fire { get; set; }

    [JsonPropertyName("facility")]
    public int Facility { get; set; }

    [JsonPropertyName("fault")]
    public int Fault { get; set; }

    [JsonPropertyName("block")]
    public int Block { get; set; }

    [JsonPropertyName("spare")]
    public int Spare { get; set; }
}
