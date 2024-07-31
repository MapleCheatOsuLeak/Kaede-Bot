namespace Kaede_Bot.Configuration;

public class GPTModelConfiguration
{
    public string Token { get; set; }
    public string Model { get; set; }
    public float Temperature { get; set; }
    public float TopP { get; set; }
    public float FrequencyPenalty { get; set; }
    public float PresencePenalty { get; set; }
    public int MaxInputTokens { get; set; }
    public int MaxOutputTokens { get; set; }
    public int MaxContextMessages { get; set; }
    public int RateLimitMinutes { get; set; }
    public int RateLimitMessages { get; set; }
    public string SystemMessage { get; set; }
}