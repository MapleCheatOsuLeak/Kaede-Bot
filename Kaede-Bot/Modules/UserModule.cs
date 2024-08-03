using System.Net.Http.Json;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Kaede_Bot.Models.Web;
using Kaede_Bot.Services;

namespace Kaede_Bot.Modules;

public class UserModule : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient _httpClient;
    private readonly DiscordRestClient _restClient;
    private readonly CommandService _commandService;
    private readonly KudosService _kudosService;
    private readonly EmbedService _embedService;
    
    public UserModule(HttpClient httpClient, DiscordRestClient restClient, CommandService commandService, KudosService kudosService, EmbedService embedService)
    {
        _httpClient = httpClient;
        _restClient = restClient;
        _commandService = commandService;
        _kudosService = kudosService;
        _embedService = embedService;
    }
    
    [Command("help")]
    [Summary("Shows a list of all commands. (Or a specific command information if `commandName` is specified.)")]
    public async Task Help([Summary("Command name.")][Remainder]string? commandName = null)
    {
        if (string.IsNullOrEmpty(commandName))
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateHelpListEmbed(Context.User, _commandService));
        }
        else
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateHelpCommandEmbed(Context.User, _commandService, commandName));
        }
    }
    
    [Command("userinfo"), Alias("ui")]
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
                if (response.IsSuccessStatusCode)
                {
                    var userinfo = await response.Content.ReadFromJsonAsync<UserInfoModel>();
                    if (userinfo is { Code: 0 })
                    {
                        await Context.Channel.SendMessageAsync(embed: _embedService.CreateUserInfoEmbed(user, userinfo));
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(embed: _embedService.CreateUserInfoEmbed(user, null));
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateUserInfoEmbed(user, null));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "User info", "User not found!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "User info", "User not found!"));
        }
    }
    
    [Command("status")]
    [Summary("Shows server/software status")]
    public async Task Status()
    {
        var response = await _httpClient.GetAsync("https://maple.software/backend/api/discord?t=3");
        if (response.IsSuccessStatusCode)
        {
            var status = await response.Content.ReadFromJsonAsync<SoftwareStatusModel>();
            if (status != null)
            {
                await Context.Channel.SendMessageAsync(embed: _embedService.CreateStatusEmbed(Context.User, status));

                return;
            }
        }

        await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Status", "Failed to retrieve software status."));
    }

    [Command("kudos send")]
    [Summary("Sends kudos to the specified user.")]
    public async Task KudosSend([Summary("Mention or id of a user")] string mentionOrId, [Summary("Amount of kudos to send.")] int amount)
    {
        if (ulong.TryParse(mentionOrId, out var userId) || MentionUtils.TryParseUser(mentionOrId, out userId))
        {
            var user = Context.Guild.GetUser(userId);
            if (user != null)
            {
                if (user.Id != Context.User.Id)
                {
                    if (await _kudosService.SendKudos(Context.User, user, amount))
                        await Context.Channel.SendMessageAsync(embed: _embedService.CreateKudosSendEmbed(Context.User, user));
                    else
                        await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Kudos", "You don't have enough kudos!"));
                }
                else
                {
                    await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Kudos", "You can't send kudos to yourself!"));
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Kudos", "User not found!"));
            }
        }
        else
        {
            await Context.Channel.SendMessageAsync(embed: _embedService.CreateErrorEmbed(Context.User, "Kudos", "User not found!"));
        }
    }

    [Command("kudos show")]
    [Summary("Shows your kudos count")]
    public async Task KudosShow()
    {
        await Context.Channel.SendMessageAsync(embed: _embedService.CreateKudosShowEmbed(Context.User, _kudosService.GetUserKudos(Context.User)));
    }
    
    [Command("kudos leaderboard")]
    [Summary("Shows kudos leaderboard")]
    public async Task KudosLeaderboard()
    {
        List<string> leaderboard = new();
        var highestKudos = _kudosService.GetAllKudos().OrderBy(k => k.Kudos).Reverse().Take(10).ToList();
        for (int i = 0; i < highestKudos.Count; i++)
        {
            var kudos = highestKudos[i];
            var user = await _restClient.GetUserAsync(kudos.UserId);
            
            leaderboard.Add($"**{i + 1}. {(user == null ? "Unknown user" : user.GetFullname())}** - {kudos.Kudos} kudos");
        }
        
        await Context.Channel.SendMessageAsync(embed: _embedService.CreateKudosLeaderboardEmbed(Context.User, leaderboard.Any() ? string.Join("\n", leaderboard) : "Nothing here yet!"));
    }
}