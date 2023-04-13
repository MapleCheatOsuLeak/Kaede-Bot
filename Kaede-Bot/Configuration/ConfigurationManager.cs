using Newtonsoft.Json;

namespace Kaede_Bot.Configuration;

public class ConfigurationManager
{
    public string Token { get; private set; }
    public string DatabasePath { get; private set; }
    public ulong GuildId { get; private set; }
    public List<string> Activities { get; private set; }
    public ServerRoles ServerRoles { get; private set; }
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
        GuildId = config.GuildId;
        Activities = config.Activities;
        ServerRoles = config.ServerRoles;
        ServerChannels = config.ServerChannels;
        GPTModelConfiguration = config.GPTModelConfiguration;
    }
}