using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kaede_Bot;

class Program
{
    public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

    private async Task MainAsync()
    {
        await using (var services = ConfigureServices())
        {
            var config = services.GetRequiredService<ConfigurationManager>();
            
            var client = services.GetRequiredService<DiscordSocketClient>();    
            var restClient = services.GetRequiredService<DiscordRestClient>();
            
            client.ThreadCreated += services.GetRequiredService<SuggestionsService>().ClientOnThreadCreated;
            client.ThreadCreated += services.GetRequiredService<BugReportsService>().ClientOnThreadCreated;

            client.Log += LogAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;
            
            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            #pragma warning disable CS4014
            Task.Run(async () =>
            #pragma warning restore CS4014
            {
                string[] activities = { "@Kaede for help!", "@Kaede to chat!", "@Kaede for anything!", "!help", "maple.software" };
                int i = 0;
                while (true)
                {
                    await client.SetActivityAsync(new Game(activities[i]));

                    i = i + 1 < activities.Length ? i + 1 : 0;

                    await Task.Delay(7500);
                }
                // ReSharper disable once FunctionNeverReturns
            });

            await restClient.LoginAsync(TokenType.Bot, config.Token);
            
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            await services.GetRequiredService<GPTService>().InitializeAsync();
            
            await Task.Delay(Timeout.Infinite);
        }
    }

    private async Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
    }

    private ServiceProvider ConfigureServices()
    {
        var config = new ConfigurationManager("config.json");
        
        return new ServiceCollection()
            .AddSingleton(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All
            })
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<DiscordRestClient>()
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandlingService>()
            .AddSingleton<GPTService>()
            .AddSingleton<SuggestionsService>()
            .AddSingleton<BugReportsService>()
            .AddSingleton(config)
            .AddDbContext<KaedeDbContext>(options => options.UseSqlite($"Data Source={config.DatabasePath}"))
            .BuildServiceProvider();
    }
}