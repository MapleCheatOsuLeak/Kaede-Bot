using Discord.Commands;
using Kaede_Bot.Services;

namespace Kaede_Bot.Modules;

public class UserModule : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _commandService;
    private readonly EmbedService _embedService;
    
    public UserModule(CommandService commandService, EmbedService embedService)
    {
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
}