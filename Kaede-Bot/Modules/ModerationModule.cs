using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Kaede_Bot.Modules;

public class ModerationModule : ModuleBase<SocketCommandContext>
{
    public DiscordRestClient RestClient { get; set; }
    
    [Command("ban"), RequireUserPermission(GuildPermission.BanMembers)]
    [Summary("Bans a user from this server. Works even on non-member users")]
    public async Task Ban([Summary("Id or mention of a user to be banned")]string mentionOrId, [Summary("Delete message history for the last X days")]int pruneDays, [Summary("Ban reason")][Remainder]string reason)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            await Context.Guild.AddBanAsync(userId, pruneDays, reason);
        }
    }
    
    [Command("unban"), RequireUserPermission(GuildPermission.BanMembers)]
    [Summary("Unbans a user. Works even on non-member users")]
    public async Task Unban([Summary("Id or mention of a user to be unbanned")]string mentionOrId, [Summary("Unban reason")][Remainder]string reason)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            await Context.Guild.RemoveBanAsync(userId);
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
            }
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
                }
            }
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
            }
        }
    }
    
    [Command("purge"), RequireUserPermission(GuildPermission.ManageMessages)]
    [Summary("Deletes a specified amount of messages.")]
    public async Task Purge([Summary("Amount of messages to delete.")]int count)
    {
        await Context.Message.DeleteAsync();
            
        int messagesToDelete = count > 100 ? 100 : count;
        var messages = await Context.Channel.GetMessagesAsync(messagesToDelete).FlattenAsync();
        
        if (Context.Channel is SocketTextChannel channel) 
            await channel.DeleteMessagesAsync(messages);
    }
}