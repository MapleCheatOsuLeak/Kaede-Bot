using System.Net.Http.Json;
using Discord;
using Discord.Commands;
using Kaede_Bot.Services;

namespace Kaede_Bot.Modules;

public class UserModule : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient _httpClient;
    private readonly CommandService _commandService;
    private readonly EmbedService _embedService;
    
    public UserModule(HttpClient httpClient, CommandService commandService, EmbedService embedService)
    {
        _httpClient = httpClient;
        _commandService = commandService;
        _embedService = embedService;
    }
    
    [Command("help")]
    [Summary("Shows a list of all commands. (Or a specific command information if `commandName` is specified.)")]
    public async Task Help([Summary("Command name.")][Remainder]string? commandName = null)
    {
        if (string.IsNullOrEmpty(commandName))
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateHelpListEmbed(Context.User, _commandService));
        }
        else
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateHelpCommandEmbed(Context.User, _commandService, commandName));
        }
    }
    
    [Command("userinfo")]
    [Summary("Shows information about a user. (Shows information about a user who invoked the command if `mentionOrId` is not specified.)")]
    public async Task UserInfo([Summary("ID or mention of a user.")]string? mentionOrId = null)
    {
        if (mentionOrId == null)
            mentionOrId = Context.User.Id.ToString();

        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            var user = Context.Guild.GetUser(userId);
            if (user != null)
            {
                var response = await _httpClient.GetAsync($"https://maple.software/backend/api/discord?t=0&u={user.Id}");
                var userinfo = await response.Content.ReadFromJsonAsync<UserInfoModel>();
                if (userinfo!.Code == 0)
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateUserInfoEmbed(user, userinfo));
                else
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateUserInfoEmbed(user, null));
            }
            else
            {
                await Context.Channel.SendMessageAsync("",
                    embed: _embedService.CreateErrorEmbed(Context.User, "User info", "User not found!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(Context.User, "User info", "User not found!"));
        }
    }
}