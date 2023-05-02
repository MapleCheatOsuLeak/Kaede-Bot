using System.Text.Json.Serialization;

namespace Kaede_Bot.Models.Web;

public class UserInfoModel
{
    [JsonPropertyName("code")] public int Code { get; set; }
    public int UserID { get; set; }
    public string JoinedOn { get; set; }
    public List<SubscriptionModel> Subscriptions { get; set; }

}

public class SubscriptionModel
{
    public string Name { get; set; }
    public string Expiration { get; set; }
    public long ExpirationUnix { get; set; }
}