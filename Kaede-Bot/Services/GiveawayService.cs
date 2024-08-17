using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Models.Database;

namespace Kaede_Bot.Services;

public class GiveawayService
{
    private readonly KaedeDbContext _kaedeDbContext;
    private readonly DiscordSocketClient _client;
    private readonly DiscordRestClient _restClient;
    private readonly EmbedService _embedService;
    private readonly ulong _guildId;

    private SocketGuild _guild = null!;
    
    public GiveawayService(KaedeDbContext kaedeDbContext, DiscordSocketClient client, DiscordRestClient restClient, EmbedService embedService, ConfigurationManager config)
    {
        _kaedeDbContext = kaedeDbContext;
        _client = client;
        _restClient = restClient;
        _embedService = embedService;
        _guildId = config.GuildId;
    }

    public Task Initialize()
    {
        _guild = _client.GetGuild(_guildId);

        return Task.CompletedTask;
    }
    
    public async Task OnHeartbeat(int i, int i1)
    {
        var endedGiveaways = _kaedeDbContext.Giveaways.Where(g => g.EndsAt <= DateTime.UtcNow).Select(g => g.Id).ToList();
        if (endedGiveaways.Any())
            foreach (var giveaway in endedGiveaways)
                await EndGiveaway(giveaway);
    }

    public List<GiveawayModel> GetAllGiveaways()
    {
        return _kaedeDbContext.Giveaways.ToList();
    }

    public bool GiveawayExists(Guid id)
    {
        return _kaedeDbContext.Giveaways.SingleOrDefault(g => g.Id == id) != null;
    }

    public bool CanRemoveGiveaway(Guid giveawayId, ulong userId)
    {
        return _kaedeDbContext.Giveaways.Single(g => g.Id == giveawayId).CreatorId == userId;
    }

    public async Task StartGiveaway(IMessageChannel channel, IUser creator, IUser host, DateTimeOffset endsAt, int winnerCount, string prize)
    {
        var message = await channel.SendMessageAsync("@everyone", embed: _embedService.CreateGiveawayStartEmbed(host, endsAt, winnerCount, prize));
        
        await message.AddReactionAsync(new Emoji("🎁"));
        
        _kaedeDbContext.Giveaways.Add(new GiveawayModel
        {
            Id = Guid.NewGuid(),
            ChannelId = channel.Id,
            MessageId = message.Id,
            CreatorId = creator.Id,
            HostId = host.Id,
            Prize = prize,
            WinnerCount = winnerCount,
            EndsAt = endsAt.UtcDateTime
        });

        await _kaedeDbContext.SaveChangesAsync();
    }

    public async Task EndGiveaway(Guid id)
    {
        var giveaway = _kaedeDbContext.Giveaways.SingleOrDefault(g => g.Id == id);
        if (giveaway == null)
            return;

        var channel = _guild.GetTextChannel(giveaway.ChannelId);
        if (channel == null)
            return;

        var message = await channel.GetMessageAsync(giveaway.MessageId);
        if (message == null)
            return;

        IUser host = await _restClient.GetUserAsync(giveaway.HostId) ?? _client.CurrentUser as IUser;
        
        var participants = (await message.GetReactionUsersAsync(new Emoji("🎁"), 750).FlattenAsync()).Where(u => !u.IsBot && _guild.GetUser(u.Id) != null).ToList();
        if (participants.Any())
        {
            List<IUser> winners = new();
            bool shouldRemove = giveaway.WinnerCount <= participants.Count;
            for (int i = 0; i < giveaway.WinnerCount; i++)
            {
                IUser winner = participants[Random.Shared.Next(0, participants.Count)];
                if (shouldRemove)
                    participants.Remove(winner);

                winners.Add(winner);
            }

            if (message is IUserMessage socketMessage)
                await socketMessage.ReplyAsync(embed: _embedService.CreateGiveawayEndEmbed(host, giveaway.Prize, winners));
        }
        else
        {
            if (message is IUserMessage socketMessage)
                await socketMessage.ReplyAsync(embed: _embedService.CreateGiveawayFailedEmbed(host, giveaway.Prize));
        }

        _kaedeDbContext.Giveaways.Remove(giveaway);
        
        await _kaedeDbContext.SaveChangesAsync();
    }

    public async Task RemoveGiveaway(Guid giveawayId)
    {
        var giveaway = _kaedeDbContext.Giveaways.SingleOrDefault(g => g.Id == giveawayId);
        if (giveaway == null)
            return;

        var channel = _guild.GetTextChannel(giveaway.ChannelId);
        if (channel != null)
        {
            var message = await channel.GetMessageAsync(giveaway.MessageId);
            if (message != null)
                await message.DeleteAsync();
        }

        _kaedeDbContext.Remove(giveaway);

        await _kaedeDbContext.SaveChangesAsync();
    }
}