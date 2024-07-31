using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kaede_Bot.Services;

public class CommandHandlingService
{
    private readonly CommandService _commands;
    private readonly DiscordSocketClient _discord;
    private readonly ConfigurationManager _configuration;
    private readonly GPTService _gpt;
    private readonly EmbedService _embedService;
    private readonly IServiceProvider _services;

    public CommandHandlingService(IServiceProvider services)
    {
        _commands = services.GetRequiredService<CommandService>();
        _discord = services.GetRequiredService<DiscordSocketClient>();
        _configuration = services.GetRequiredService<ConfigurationManager>();
        _gpt = services.GetRequiredService<GPTService>();
        _embedService = services.GetRequiredService<EmbedService>();
        _services = services;
        
        _commands.CommandExecuted += CommandExecutedAsync;
        _discord.MessageReceived += MessageReceivedAsync;
    }

    public async Task InitializeAsync()
    {
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    public async Task MessageReceivedAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message)
            return;
        
        if (message.Source != MessageSource.User)
            return;

        if (message.Author is not SocketGuildUser author)
            return;

        if (message.Channel is not SocketTextChannel)
            return;

        var argPos = 0;
        if (message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
        {
            if (await checkWrongChannel(message, author))
                return;
            
            #pragma warning disable CS4014
            Task.Run(async () =>
            #pragma warning restore CS4014
            {
                var promptContext = new SocketCommandContext(_discord, message);
                await _gpt.HandlePrompt(promptContext, argPos);
            });

            return;
        }

        if (!message.HasCharPrefix('!', ref argPos))
            return;

        if (await checkWrongChannel(message, author))
            return;

        var context = new SocketCommandContext(_discord, message);

        await _commands.ExecuteAsync(context, argPos, _services);
    }

    public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
    {
        int length = context.Message.Content.IndexOf(' ');
        length = length == -1 ? context.Message.Content.Length : length;
        string commandString = context.Message.Content.Substring(0, length);
        
        if (!command.IsSpecified)
        {
            await context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(context.User, "Command Handler",
                    $"`{commandString}` command does not exist!\n\nType `!help` for a list of all commands."));
        }
        else if (!result.IsSuccess)
        {
            await context.Channel.SendMessageAsync("",
                embed: _embedService.CreateErrorEmbed(context.User, "Command Handler",
                    $"An error occured while executing `{commandString}` command:\n\n`{result.ErrorReason}`"));
        }
    }

    private async Task<bool> checkWrongChannel(SocketUserMessage message, SocketGuildUser author)
    {
        if (message.Channel.Id != _configuration.ServerChannels.BotsChannelId && !author.Roles.Select(r => r.Id).Intersect(_configuration.BotsChannelBypassRoleIds).Any())
        {
            #pragma warning disable CS4014
            Task.Run(async () =>
            #pragma warning restore CS4014
            {
                var wrongChannelMessage = await message.ReplyAsync("", embed: _embedService.CreateWrongChannelEmbed(author, _configuration.ServerChannels.BotsChannelId));
                await Task.Delay(7500);
                await wrongChannelMessage.DeleteAsync();
                await message.DeleteAsync();
            });

            return true;
        }

        return false;
    }
}