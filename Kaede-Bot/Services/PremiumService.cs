using System.Net.Http.Json;
using Discord;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Models.Web;

namespace Kaede_Bot.Services;

public class PremiumService
{
    private readonly HttpClient _httpClient;
    private readonly DiscordSocketClient _client;
    private readonly ulong _guildId;
    private readonly ulong _premiumRoleId;

    private SocketGuild _guild = null!;
    
    public PremiumService(HttpClient httpClient, DiscordSocketClient client, ConfigurationManager config)
    {
        _httpClient = httpClient;
        _client = client;
        _guildId = config.GuildId;
        _premiumRoleId = config.ServerRoles.PremiumRoleId;
    }

    public async Task InitializeAsync()
    {
        _guild = _client.GetGuild(_guildId);
    }
    
    public async Task OnHeartbeat(int i, int i1)
    {
        var response = await _httpClient.GetAsync("https://maple.software/backend/api/discordv2?t=1");
        if (!response.IsSuccessStatusCode)
            return;

        var subscribersModel = await response.Content.ReadFromJsonAsync<SubscribersModel>();
        if (subscribersModel == null || subscribersModel.Code != 0)
            return;

        var subscribers = subscribersModel.Subscribers.Select(ulong.Parse);
        foreach (var user in await _guild.GetUsersAsync().FlattenAsync())
        {
            if (user is not SocketGuildUser guildUser)
                    continue;

            // ReSharper disable once PossibleMultipleEnumeration
            bool isSubscribed = subscribers.Any(s => s == user.Id);
            bool hasPremiumRole = guildUser.Roles.Any(r => r.Id == _premiumRoleId);

            if (isSubscribed && !hasPremiumRole)
                await guildUser.AddRoleAsync(_premiumRoleId);
            else if (!isSubscribed && hasPremiumRole)
                await guildUser.RemoveRoleAsync(_premiumRoleId);
        }
    }
}