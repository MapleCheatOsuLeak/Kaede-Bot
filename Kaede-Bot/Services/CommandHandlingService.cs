using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Kaede_Bot.Services;

public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly GPTService _gpt;
        private readonly EmbedService _embedService;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
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
            if (!(rawMessage is SocketUserMessage message))
                return;
            
            if (message.Source != MessageSource.User)
                return;

            if (!(message.Author is SocketGuildUser))
                return;

            if (!(message.Channel is SocketTextChannel))
                return;

            var argPos = 0;
            if (message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
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
    }