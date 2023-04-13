using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer;
using Kaede_Bot.Configuration;
using Kaede_Bot.Services;

namespace Kaede_Bot.Modules;

public class ModerationModule : ModuleBase<SocketCommandContext>
{
    private readonly DiscordRestClient _restClient;
    private readonly EmbedService _embedService;

    private readonly ulong _actionLogsChannelId;
    
    public ModerationModule(DiscordRestClient restClient, EmbedService embedService, ConfigurationManager config)
    {
        _restClient = restClient;
        _embedService = embedService;

        _actionLogsChannelId = config.ServerChannels.ActionLogsChannelId;
    }

    [Command("ban"), RequireUserPermission(GuildPermission.BanMembers)]
    [Summary("Bans a user from this server. Works even on non-member users")]
    public async Task Ban([Summary("Id or mention of a user to be banned")]string mentionOrId, [Summary("Delete message history for the last X days")]int pruneDays, [Summary("Ban reason")][Remainder]string reason)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            await Context.Guild.AddBanAsync(userId, pruneDays, reason);

            var restUser = await _restClient.GetUserAsync(userId);
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateBanEmbed(restUser, Context.User, reason));

            var actionLogsChannel = Context.Guild.GetTextChannel(_actionLogsChannelId);
            await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(Context.User, "Ban",
                $"User {restUser.GetFullname()} ({restUser.Id}) has been banned, prune days: {pruneDays}.", reason));
        }
        else
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(Context.User, "Ban", "User not found!"));
        }
    }
    
    [Command("unban"), RequireUserPermission(GuildPermission.BanMembers)]
    [Summary("Unbans a user. Works even on non-member users")]
    public async Task Unban([Summary("Id or mention of a user to be unbanned")]string mentionOrId, [Summary("Unban reason")][Remainder]string reason)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            await Context.Guild.RemoveBanAsync(userId);
            
            var restUser = await _restClient.GetUserAsync(userId);
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateUnbanEmbed(restUser, Context.User, reason));
            
            var actionLogsChannel = Context.Guild.GetTextChannel(_actionLogsChannelId);
            await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(Context.User, "Unban",
                $"User {restUser.GetFullname()} ({restUser.Id}) has been unbanned.", reason));
        }
        else
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(Context.User, "Unban", "User not found!"));
        }
    }
    
    [Command("kick"), RequireUserPermission(GuildPermission.KickMembers)]
    [Summary("Kicks a user from this server")]
    public async Task Kick([Summary("Id or mention of a user to be kicked")]string mentionOrId, [Summary("Kick reason")][Remainder]string reason)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            var user = Context.Guild.GetUser(userId);
            if (user != null)
            {
                await user.KickAsync(reason);
                
                await Context.Channel.SendMessageAsync(embed: _embedService.CreateKickEmbed(user, Context.User, reason));
                
                var actionLogsChannel = Context.Guild.GetTextChannel(_actionLogsChannelId);
                await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(Context.User, "Kick",
                    $"User {user.GetFullname()} ({user.Id}) has been kicked.", reason));
            }
            else
            {
                await Context.Channel.SendMessageAsync("",
                    embed: _embedService.CreateErrorEmbed(Context.User, "Kick", "User not found!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(Context.User, "Kick", "User not found!"));
        }
    }
    
    [Command("mute"), RequireUserPermission(GuildPermission.ModerateMembers)]
    [Summary("Mutes a user by Id or mention")]
    public async Task Mute([Summary("Id or mention of a user to be muted")]string mentionOrId, [Summary("Mute duration in the following format: d:hh:mm")]string duration, [Summary("Mute reason")][Remainder]string reason)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            var user = Context.Guild.GetUser(userId);
            if (user != null)
            {
                if (TimeSpan.TryParseExact(duration, "d\\:hh\\:mm", Constants.Culture, out var time))
                {
                    await user.SetTimeOutAsync(time);
                    
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateMuteEmbed(user, Context.User, time, reason));
                    
                    var actionLogsChannel = Context.Guild.GetTextChannel(_actionLogsChannelId);
                    await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(Context.User, "Mute",
                        $"User {user.GetFullname()} ({user.Id}) has been muted for {time.Humanize(3, Constants.Culture)}", reason));
                }
                else
                {
                    await Context.Channel.SendMessageAsync("",
                        embed: _embedService.CreateErrorEmbed(Context.User, "Mute", "Failed to parse mute duration!"));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("",
                    embed: _embedService.CreateErrorEmbed(Context.User, "Mute", "User not found!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(Context.User, "Mute", "User not found!"));
        }
    }
    
    [Command("unmute"), RequireUserPermission(GuildPermission.ModerateMembers)]
    [Summary("Unmutes a user by Id or mention")]
    public async Task Unmute([Summary("Id or mention of a user to be unmuted")]string mentionOrId, [Summary("Unmute reason")][Remainder]string reason)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            var user = Context.Guild.GetUser(userId);
            if (user != null)
            {
                await user.RemoveTimeOutAsync();

                await Context.Channel.SendMessageAsync(embed: _embedService.CreateUnmuteEmbed(user, Context.User, reason));
                
                var actionLogsChannel = Context.Guild.GetTextChannel(_actionLogsChannelId);
                await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(Context.User, "Unmute",
                    $"User {user.GetFullname()} ({user.Id}) has been unmuted.", reason));
            }
            else
            {
                await Context.Channel.SendMessageAsync("",
                    embed: _embedService.CreateErrorEmbed(Context.User, "Unmute", "User not found!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(Context.User, "Unmute", "User not found!"));
        }
    }
    
    [Command("purge"), RequireUserPermission(GuildPermission.ManageMessages)]
    [Summary("Deletes a specified amount of messages.")]
    public async Task Purge([Summary("Amount of messages to delete.")]int count, [Summary("Purge reason")][Remainder]string reason)
    {
        await Context.Message.DeleteAsync();
            
        int messagesToDelete = count > 100 ? 100 : count;
        var messages = await Context.Channel.GetMessagesAsync(messagesToDelete).FlattenAsync();

        if (Context.Channel is SocketTextChannel channel)
        {
            await channel.DeleteMessagesAsync(messages);

            var actionLogsChannel = Context.Guild.GetTextChannel(_actionLogsChannelId);
            await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(Context.User, "Purge",
                $"{messagesToDelete} {(messagesToDelete == 1 ? "message" : "messages")} {(messagesToDelete == 1 ? "has" : "have")} been removed from #{channel.Name} channel.",
                reason));
        }
    }
}