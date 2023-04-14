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
        await using var services = ConfigureServices();
        
        var config = services.GetRequiredService<ConfigurationManager>();
            
        var client = services.GetRequiredService<DiscordSocketClient>();    
        var restClient = services.GetRequiredService<DiscordRestClient>();

        client.Log += LogAsync;
        services.GetRequiredService<CommandService>().Log += LogAsync;
        
        // ReSharper disable once AccessToDisposedClosure
        client.Ready += async () => await Ready(services);
            
        await client.LoginAsync(TokenType.Bot, config.Token);
        await client.StartAsync();

        await restClient.LoginAsync(TokenType.Bot, config.Token);
        
        await Task.Delay(Timeout.Infinite);
    }

    private async Task Ready(IServiceProvider services)
    {
        var client = services.GetRequiredService<DiscordSocketClient>(); 
        
        await services.GetRequiredService<ActivityService>().InitializeAsync();
        
        await services.GetRequiredService<PremiumService>().InitializeAsync();
        client.LatencyUpdated += services.GetRequiredService<PremiumService>().OnHeartbeat;

        await services.GetRequiredService<AnticheatWarningService>().InitializeAsync();
        client.LatencyUpdated += services.GetRequiredService<AnticheatWarningService>().OnHeartbeat;
        
        await services.GetRequiredService<GiveawayService>().InitializeAsync();
        client.LatencyUpdated += services.GetRequiredService<GiveawayService>().OnHeartbeat;
        
        client.ThreadCreated += services.GetRequiredService<SuggestionsService>().ClientOnThreadCreated;
        client.ThreadCreated += services.GetRequiredService<BugReportsService>().ClientOnThreadCreated;
        
        client.ThreadUpdated += services.GetRequiredService<KudosService>().ClientOnThreadUpdated;
        
        await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

        await services.GetRequiredService<GPTService>().InitializeAsync();
    }

    private async Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
    }

    private ServiceProvider ConfigureServices()
    {
        var config = new ConfigurationManager("config.json");
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "mapleserver/azuki is a cutie");
        
        return new ServiceCollection()
            .AddSingleton(new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildBans 
                                 | GatewayIntents.GuildMessages | GatewayIntents.MessageContent 
                                 | GatewayIntents.GuildMessageReactions | GatewayIntents.DirectMessageTyping
            })
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<DiscordRestClient>()
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandlingService>()
            .AddSingleton<GPTService>()
            .AddSingleton<SuggestionsService>()
            .AddSingleton<BugReportsService>()
            .AddSingleton<EmbedService>()
            .AddSingleton<PremiumService>()
            .AddSingleton<ActivityService>()
            .AddSingleton<AnticheatWarningService>()
            .AddSingleton<GiveawayService>()
            .AddSingleton<KudosService>()
            .AddSingleton(config)
            .AddSingleton(httpClient)
            .AddDbContext<KaedeDbContext>(options => options.UseSqlite($"Data Source={config.DatabasePath}"))
            .BuildServiceProvider();
    }
}