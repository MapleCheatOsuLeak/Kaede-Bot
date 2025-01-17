﻿namespace Kaede_Bot.Configuration;

public class Configuration
{
    public string Token { get; set; }
    public string DatabasePath { get; set; }
    public ulong GuildId { get; set; }
    public List<string> Activities { get; set; }
    public ServerRoles ServerRoles { get; set; }
    public ServerChannels ServerChannels { get; set; }
    public List<ulong> BotsChannelBypassRoleIds { get; set; }
    public KudosConfiguration KudosConfiguration { get; set; }
    public GPTModelConfiguration GPTModelConfiguration { get; set; } = new GPTModelConfiguration();
}