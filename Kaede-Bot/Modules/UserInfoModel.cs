using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Kaede_Bot.Modules;

public class UserInfoModel
{
    [JsonPropertyName("code")] public int Code { get; set; }
    public int ID { get; set; }
    public string JoinedOn { get; set; }
    public List<SubscriptionModel> Subscriptions { get; set; }

}

public class SubscriptionModel
{
    public string Name { get; set; }
    public string Expiration { get; set; }
}