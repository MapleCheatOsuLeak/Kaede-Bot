using System.Text.Json.Serialization;

namespace Kaede_Bot.Models.Web;

public class SoftwareStatusModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    public int OnlineCount { get; set; }
    public List<CheatStatusModel> Statuses { get; set; }
}

public class CheatStatusModel
{
    public string Name { get; set; }
    public CheatStatus Status { get; set; }
}

public enum CheatStatus
{
    Undetected = 0,
    Outdated = 1,
    Detected = 2,
    Unknown = 3
}