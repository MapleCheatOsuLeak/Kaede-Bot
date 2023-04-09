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
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _gpt = services.GetRequiredService<GPTService>();
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
            if (!command.IsSpecified)
                return;
            
            if (result.IsSuccess)
                return;
            
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }