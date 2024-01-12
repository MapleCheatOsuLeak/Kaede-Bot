using System.Net.Http.Json;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Models.Web;

namespace Kaede_Bot.Services;

public class AnticheatWarningService
{
    private readonly HttpClient _httpClient;
    private readonly DiscordSocketClient _client;
    private readonly EmbedService _embedService;
    private readonly ulong _guildId;
    private readonly ulong _anticheatUpdatesChannelId;
    
    private SocketTextChannel _anticheatUpdatesChannel = null!;
    private List<AnticheatInfoEntry> _anticheats = new();
    
    public AnticheatWarningService(HttpClient httpClient, DiscordSocketClient client, EmbedService embedService, ConfigurationManager config)
    {
        _httpClient = httpClient;
        _client = client;
        _embedService = embedService;
        _guildId = config.GuildId;
        _anticheatUpdatesChannelId = config.ServerChannels.AnticheatUpdatesChannelId;
    }

    public Task Initialize()
    {
        var guild = _client.GetGuild(_guildId);

        _anticheatUpdatesChannel = guild.GetTextChannel(_anticheatUpdatesChannelId);

        return Task.CompletedTask;
    }

    public Task OnHeartbeat(int i, int i1)
    {
        _ = Task.Run(async () =>
        {
            var response = await _httpClient.GetAsync("https://maple.software/backend/api/discord?t=2");
            if (!response.IsSuccessStatusCode)
                return;

            var anticheatInfoModel = await response.Content.ReadFromJsonAsync<AnticheatInfoModel>();
            if (anticheatInfoModel == null || anticheatInfoModel.Code != 0)
                return;

            var anticheats = anticheatInfoModel.Anticheats;
            if (_anticheats.Count > 0)
            {
                var updatedAnticheats = anticheats.Where(newAnticheat => _anticheats.Any(oldAnticheat =>
                    newAnticheat.GameName == oldAnticheat.GameName &&
                    newAnticheat.AnticheatChecksum != oldAnticheat.AnticheatChecksum));

                foreach (var updatedAnticheat in updatedAnticheats)
                    await _anticheatUpdatesChannel.SendMessageAsync("@everyone",
                        embed: _embedService.CreateAnticheatUpdateEmbed(updatedAnticheat.GameName));
            }

            _anticheats = anticheats;
        });

        return Task.CompletedTask;
    }
}