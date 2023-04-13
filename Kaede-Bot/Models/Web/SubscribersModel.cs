using System.Text.Json.Serialization;

namespace Kaede_Bot.Models.Web;

public class SubscribersModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    public List<string> Subscribers { get; set; }
}