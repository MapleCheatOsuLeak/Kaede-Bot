using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Services;

namespace Kaede_Bot.Modules;

public class GiveawayModule: ModuleBase<SocketCommandContext>
{
    private readonly DiscordRestClient _restClient;
    private readonly GiveawayService _giveawayService;
    private readonly EmbedService _embedService;
    private readonly ulong _giveawaysRoleId;

    public GiveawayModule(DiscordRestClient restClient, GiveawayService giveawayService, EmbedService embedService, ConfigurationManager config)
    {
        _restClient = restClient;
        _giveawayService = giveawayService;
        _embedService = embedService;
        _giveawaysRoleId = config.ServerRoles.GiveawaysRoleId;
    }
    
    [Summary("Creates a new giveaway in **THIS** channel")]
    [Command("giveaway create")]
    public async Task GiveawayCreate([Summary("Giveaway host's mention or Id")]string hostMentionOrId, [Summary("Giveaway duration in the following format: d:hh:mm")]string duration, [Summary("The amount of winners")]int winnerCount, [Remainder][Summary("Giveaway prize")]string prize)
    {
        if (Context.User is SocketGuildUser guildUser && guildUser.Roles.Any(r => r.Id == _giveawaysRoleId))
        {
            if (ulong.TryParse(hostMentionOrId, out var hostId) || MentionUtils.TryParseUser(hostMentionOrId, out hostId))
            {
                var host = await _restClient.GetUserAsync(hostId);
                if (host != null)
                {
                    if (TimeSpan.TryParseExact(duration, "d\\:hh\\:mm", Constants.Culture, out var time))
                    {
                        if (winnerCount >= 1)
                        {
                            await Context.Message.DeleteAsync();

                            await _giveawayService.StartGiveaway(Context.Channel, Context.User, host, DateTime.UtcNow.Add(time), winnerCount, prize);
                        }
                        else
                        {
                            await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "Winner count should be greater than or equal to 1!"));
                        }
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "Failed to parse giveaway duration!"));
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "User not found!"));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "User not found!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "You don't have permission to use this command!"));
        }
    }
    
    [Summary("Ends the giveaway prematurely and picks the winner(s)")]
    [Command("giveaway end")]
    public async Task GiveawayEnd([Summary("Giveaway Id")]string giveawayId)
    {
        if (Context.User is SocketGuildUser guildUser && guildUser.Roles.Any(r => r.Id == _giveawaysRoleId))
        {
            if (Guid.TryParse(giveawayId, out var giveawayIdGuid))
            {
                if (_giveawayService.GiveawayExists(giveawayIdGuid))
                {
                    await Context.Message.DeleteAsync();

                    await _giveawayService.EndGiveaway(giveawayIdGuid);
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "Giveaway not found!"));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "Invalid giveaway id!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "You don't have permission to use this command!"));
        }
    }
    
    [Summary("Removes the giveaway")]
    [Command("giveaway remove")]
    public async Task GiveawayRemove([Summary("Giveaway Id")]string giveawayId)
    {
        if (Context.User is SocketGuildUser guildUser && guildUser.Roles.Any(r => r.Id == _giveawaysRoleId))
        {
            if (Guid.TryParse(giveawayId, out var giveawayIdGuid))
            {
                if (_giveawayService.GiveawayExists(giveawayIdGuid))
                {
                    if (_giveawayService.CanRemoveGiveaway(giveawayIdGuid, Context.User.Id))
                    {
                        await Context.Message.DeleteAsync();

                        await _giveawayService.RemoveGiveaway(giveawayIdGuid);
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "Giveaway not found!"));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "Invalid giveaway id!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "You don't have permission to use this command!"));
        }
    }
    
    [Summary("Shows all active giveaways")]
    [Command("giveaway list")]
    public async Task GiveawayList()
    {
        if (Context.User is SocketGuildUser guildUser && guildUser.Roles.Any(r => r.Id == _giveawaysRoleId))
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateGiveawayListEmbed(Context.User, _giveawayService.GetAllGiveaways()));
        }
        else
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Giveaway", "You don't have permission to use this command!"));
        }
    }
}