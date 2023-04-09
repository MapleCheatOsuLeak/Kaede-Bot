namespace Kaede_Bot.Configuration;

public class GPTModelConfiguration
{
    public string Token { get; set; }
    public string Model { get; set; }
    public float Temperature { get; set; }
    public int MaxTokens { get; set; }
    public string SystemMessage { get; set; }
}