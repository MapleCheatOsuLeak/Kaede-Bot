using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Humanizer;
using Kaede_Bot.Configuration;

namespace Kaede_Bot.Services;

public class AuditLogWatcherService
{
    private readonly DiscordRestClient _restClient;
    private readonly EmbedService _embedService;

    private readonly ulong _actionLogsChannelId;
    
    public AuditLogWatcherService(DiscordRestClient restClient, EmbedService embedService, ConfigurationManager config)
    {
        _restClient = restClient;
        _embedService = embedService;

        _actionLogsChannelId = config.ServerChannels.ActionLogsChannelId;
    }
    
    public Task ClientOnAuditLogCreated(SocketAuditLogEntry logEntry, SocketGuild guild)
    {
        if (logEntry.User.Id == _restClient.CurrentUser.Id)
            return Task.CompletedTask;
        
        _ = Task.Run(async () =>
        {
            var actionLogsChannel = guild.GetTextChannel(_actionLogsChannelId);

            switch (logEntry.Action)
            {
                case ActionType.Ban:
                {
                    var target = (logEntry.Data as SocketBanAuditLogData)?.Target;
                    var targetUser = target.HasValue ? await _restClient.GetUserAsync(target.Value.Id) : null;
                    if (targetUser != null)
                        await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(logEntry.User, "Ban", $"User {targetUser.GetFullname()} ({targetUser.Id}) has been banned.", logEntry.Reason));
                    break;
                }
                case ActionType.Unban:
                {
                    var target = (logEntry.Data as SocketUnbanAuditLogData)?.Target;
                    var targetUser = target.HasValue ? await _restClient.GetUserAsync(target.Value.Id) : null;
                    if (targetUser != null)
                        await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(logEntry.User, "Unban", $"User {targetUser.GetFullname()} ({targetUser.Id}) has been unbanned.", "No reason provided."));
                    break;
                }
                case ActionType.Kick:
                {
                    var target = (logEntry.Data as SocketKickAuditLogData)?.Target;
                    var targetUser = target.HasValue ? await _restClient.GetUserAsync(target.Value.Id) : null;
                    if (targetUser != null)
                        await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(logEntry.User, "Kick", $"User {targetUser.GetFullname()} ({targetUser.Id}) has been kicked.", "No reason provided."));
                    break;
                }
                case ActionType.MemberUpdated:
                {
                    var logData = logEntry.Data as SocketMemberUpdateAuditLogData;
                    if (logData != null)
                    {
                        var target = logData.Target;
                        var targetUser = target.HasValue ? await _restClient.GetUserAsync(target.Value.Id) : null;
                        if (targetUser != null)
                        {
                            var muted = (!logData.Before.TimedOutUntil.HasValue || logData.Before.TimedOutUntil < DateTime.UtcNow) && (logData.After.TimedOutUntil.HasValue && logData.After.TimedOutUntil > DateTime.UtcNow);
                            var wasMuted = (!logData.After.TimedOutUntil.HasValue || logData.After.TimedOutUntil < DateTime.UtcNow) && (logData.Before.TimedOutUntil.HasValue && logData.Before.TimedOutUntil > DateTime.UtcNow);

                            if (muted)
                            {
                                var muteTime = logData.After.TimedOutUntil - DateTime.UtcNow;
                                var muteTimeRounded = TimeSpan.FromSeconds((int)Math.Ceiling(muteTime!.Value.TotalMilliseconds / 1000f));
                                await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(logEntry.User, "Mute", $"User {targetUser.GetFullname()} ({targetUser.Id}) has been muted for {muteTimeRounded.Humanize(3, Constants.Culture)}", "No reason provided."));
                            }
                            else if (wasMuted)
                            {
                                await actionLogsChannel.SendMessageAsync(embed: _embedService.CreateModActionEmbed(logEntry.User, "Unmute", $"User {targetUser.GetFullname()} ({targetUser.Id}) has been unmuted.", "No reason provided."));
                            }
                        }
                    }

                    break;
                }
            }
        });

        return Task.CompletedTask;
    }
}