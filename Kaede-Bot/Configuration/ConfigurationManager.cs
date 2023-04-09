using Newtonsoft.Json;

namespace Kaede_Bot.Configuration;

public class ConfigurationManager
{
    public string Token { get; private set; }
    public string DatabasePath { get; private set; }
    public ServerChannels ServerChannels { get; private set; }
    public GPTModelConfiguration GPTModelConfiguration { get; private set; }

    public ConfigurationManager(string path) => LoadConfiguration(path);

    private void LoadConfiguration(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found at '{path}'.");

        var json = File.ReadAllText(path);
        var config = JsonConvert.DeserializeObject<Configuration>(json);

        Token = config!.Token;
        DatabasePath = config.DatabasePath;
        ServerChannels = config.ServerChannels;
        GPTModelConfiguration = config.GPTModelConfiguration;
    }
}