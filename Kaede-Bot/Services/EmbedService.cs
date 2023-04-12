using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Kaede_Bot.Models.Web;

namespace Kaede_Bot.Services;

public class EmbedService
{
    public Embed CreateInfoEmbed(IUser user, string title, string description)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = $":white_check_mark: {title}",
            Description = description,
            Color = new Color(Constants.AccentColour),
            Footer = new EmbedFooterBuilder
            {
                Text = $"Triggered by {user.GetFullname()}",
                IconUrl = user.GetAvatarUrl()
            }

        }.WithCurrentTimestamp();

        return embed.Build();
    }
        
    public Embed CreateErrorEmbed(IUser user, string title, string description)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = $":x: {title}",
            Description = description,
            Color = new Color(Constants.ErrorColour),
            Footer = new EmbedFooterBuilder
            {
                Text = $"Triggered by {user.GetFullname()}",
                IconUrl = user.GetAvatarUrl()
            }

        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateBanEmbed(IUser bannedUser, IUser executionUser, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = ":no_entry: User banned!",
            Description = bannedUser.GetFullname(),
            ThumbnailUrl = bannedUser.GetAvatarUrl(),
            Color = new Color(Constants.AccentColour),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Reason",
                    Value = reason
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {executionUser.GetFullname()}",
                IconUrl = executionUser.GetAvatarUrl()
            }
        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateUnbanEmbed(IUser unbannedUser, IUser executionUser, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = ":white_check_mark: User unbanned!",
            Description = unbannedUser.GetFullname(),
            ThumbnailUrl = unbannedUser.GetAvatarUrl(),
            Color = new Color(Constants.AccentColour),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Reason",
                    Value = reason
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {executionUser.GetFullname()}",
                IconUrl = executionUser.GetAvatarUrl()
            }
        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateKickEmbed(IUser kickedUser, IUser executionUser, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = ":no_entry: User kicked!",
            Description = kickedUser.GetFullname(),
            ThumbnailUrl = kickedUser.GetAvatarUrl(),
            Color = new Color(Constants.AccentColour),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Reason",
                    Value = reason
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {executionUser.GetFullname()}",
                IconUrl = executionUser.GetAvatarUrl()
            }
        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateMuteEmbed(IUser mutedUser, IUser executionUser, TimeSpan duration, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = ":mute: User muted!",
            Description = mutedUser.GetFullname(),
            ThumbnailUrl = mutedUser.GetAvatarUrl(),
            Color = new Color(Constants.AccentColour),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Duration",
                    Value = duration.Humanize(3, Constants.Culture)
                },
                new()
                {
                    Name = "Reason",
                    Value = reason
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {executionUser.GetFullname()}",
                IconUrl = executionUser.GetAvatarUrl()
            }
        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateUnmuteEmbed(IUser unmutedUser, IUser executionUser, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = ":sound: User unmuted!",
            Description = unmutedUser.GetFullname(),
            ThumbnailUrl = unmutedUser.GetAvatarUrl(),
            Color = new Color(Constants.AccentColour),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Reason",
                    Value = reason
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {executionUser.GetFullname()}",
                IconUrl = executionUser.GetAvatarUrl()
            }
        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateModActionEmbed(IUser user, string action, string description, string reason)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = ":memo: Action completed successfully",
            Description = action,
            ThumbnailUrl = user.GetAvatarUrl(),
            Color = new Color(Constants.AccentColour),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Description",
                    Value = description
                },
                new()
                {
                    Name = "Reason",
                    Value = reason
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {user.GetFullname()} ({user.Id})",
            }
        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateHelpListEmbed(IUser user, CommandService commandService)
    {
        List<CommandInfo> commands = commandService.Commands.ToList();
        List<CommandInfo> moderationCommands = new();
        foreach (var command in commands)
        {
            foreach (var precondition in command.Preconditions)
            {
                if (precondition is RequireUserPermissionAttribute)
                {
                    moderationCommands.Add(command);

                    break;
                }
            }
        }
        
        commands = commands.Except(moderationCommands).ToList();

        EmbedBuilder embed = new EmbedBuilder
        {
            Title = "Help",
            Description = "Type `!help` `<command>` to see information about a specific command.\n\nLooking for support? I may be able to assist you!\n**@Kaede** *<your message>*",
            Color = new Color(Constants.AccentColour),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Commands",
                    Value = string.Join(", ", commands.OrderBy(c => c.Name).Select(c => string.Concat("`", c.Name, "`")))
                },
                new()
                {
                    Name = "Moderation Commands",
                    Value = string.Join(", ", moderationCommands.OrderBy(c => c.Name).Select(c => string.Concat("`", c.Name, "`")))
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {user.GetFullname()}",
                IconUrl = user.GetAvatarUrl()
            }

        }.WithCurrentTimestamp();

        return embed.Build();
    }
    
    public Embed CreateHelpCommandEmbed(IUser user, CommandService commandService, string name)
    {
        CommandInfo? command = commandService.Commands.ToList().FirstOrDefault(c => c.Name.ToLower() == name.ToLower());

        if (command == null)
            return CreateErrorEmbed(user, "Help",
                $"`{name}` command does not exist!\n\nType `!help` for a list of all commands.");

        string args = string.Empty;
        string argsExtended = string.Empty;
        foreach (var arg in command.Parameters)
        {
            args += arg.IsOptional ? $" `[{arg.Name}]`" : $" `<{arg.Name}>`";
            argsExtended += $"{(arg.IsOptional ? "`[Optional]` " : "")}`[{arg.Type.ToString().Split('.').Last()}]` `{arg.Name}`: {arg.Summary}\n";
        }

        EmbedBuilder embed = new EmbedBuilder
        {
            Title = "Help",
            Description = $"`!{name}`{args}: {command.Summary}",
            Color = new Color(Constants.AccentColour),
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {user.GetFullname()}",
                IconUrl = user.GetAvatarUrl()
            }

        }.WithCurrentTimestamp();

        if (!string.IsNullOrEmpty(argsExtended))
        {
            embed.AddField(delegate(EmbedFieldBuilder builder)
            {
                builder.Name = "Arguments";
                builder.Value = argsExtended;
            });
        }

        return embed.Build();
    }

    public Embed CreateUserInfoEmbed(SocketGuildUser user, UserInfoModel? userinfo)
    {
        bool canProvideMapleInfo = userinfo != null;
        
        string roles = string.Join(", ", user.Roles.ToList().OrderBy(r => r.Name).Select(r => string.Concat("`", r.Name, "`"))).Replace("`@everyone`, ", string.Empty).Replace("`@everyone`", string.Empty);
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = " ",
            Description = user.Mention,
            Author = new EmbedAuthorBuilder
            {
                Name = $"User information for {user.GetFullname()}",
                IconUrl = user.GetAvatarUrl()
            },
            Color = new Color(Constants.AccentColour),
            ThumbnailUrl = user.GetAvatarUrl(),
            Fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "⠀",
                    Value = "**Discord information**",
                    IsInline = false
                },
                new()
                {
                    Name = "Registered on",
                    Value = user.CreatedAt.GetHumanReadableString(),
                    IsInline = true
                },
                new()
                {
                    Name = "Joined on",
                    Value = user.JoinedAt.GetHumanReadableString(),
                    IsInline = true
                },
                new()
                {
                    Name = "Roles",
                    Value = string.IsNullOrEmpty(roles) ? "None" : roles,
                    IsInline = false
                },
                new()
                {
                    Name = "⠀",
                    Value = "**Maple information**",
                    IsInline = false
                }
            },
            Footer = new EmbedFooterBuilder
            {
                Text = $"ID: {user.Id}"
            }

        }.WithCurrentTimestamp();

        if (canProvideMapleInfo)
        {
            embed.AddField("User ID", userinfo!.UserID, true);
            embed.AddField("Joined on", userinfo.JoinedOn, true);
            embed.AddField("⠀", "**Subscriptions**", false);
            if (userinfo.Subscriptions.Count > 0)
            {
                foreach (var subscription in userinfo.Subscriptions)
                    embed.AddField(subscription.Name, subscription.Expiration == "lifetime" ? subscription.Expiration : $"Expires on {subscription.Expiration}", false);
            }
            else
                embed.AddField("None", "Subscribe now at https://maple.software/dashboard/store", false);
        }
        else
            embed.AddField("Your discord account is not linked!", "Link your account at https://maple.software/dashboard/settings to view more information!", false);

        return embed.Build();
    }
    
    public Embed CreateSoftwareStatus(IUser user, List<CheatStatusModel> statuses)
    {
        EmbedBuilder embed = new EmbedBuilder
        {
            Title = ":shield: Software status",
            Color = new Color(Constants.AccentColour),
            
            Footer = new EmbedFooterBuilder
            {
                Text = $"Executed by {user.GetFullname()}",
                IconUrl = user.GetAvatarUrl()
            }
        }.WithCurrentTimestamp();

        foreach (var status in statuses)
            embed.AddField(status.Name, status.Status, false);

        return embed.Build();
    }
}