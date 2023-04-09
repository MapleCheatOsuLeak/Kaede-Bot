namespace Kaede_Bot.Configuration;

public class Configuration
{
    public string Token { get; set; }
    public string DatabasePath { get; set; }
    public GPTModelConfiguration GPTModelConfiguration { get; set; } = new GPTModelConfiguration();
}