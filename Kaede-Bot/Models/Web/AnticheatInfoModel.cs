using System.Text.Json.Serialization;

namespace Kaede_Bot.Models.Web;

public class AnticheatInfoModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    public List<AnticheatInfoEntry> Anticheats { get; set; }
}

public class AnticheatInfoEntry
{
    public string GameName { get; set; }
    public string AnticheatChecksum { get; set; }
}