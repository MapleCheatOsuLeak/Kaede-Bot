using System.Text.Json.Serialization;

namespace Kaede_Bot.Models.Web;

public class ProductsInfoModel
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    public List<ProductsInfoEntry> Products { get; set; }
}

public class ProductsInfoEntry
{
    public string Name { get; set; }
    public List<ProductsPriceEntry> Prices { get; set; }
}

public class ProductsPriceEntry
{
    public string Duration { get; set; }
    public int Price { get; set; }
    public int IsAvailable { get; set; }
}