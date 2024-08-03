using System.Text.Json;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;

namespace Kaede_Bot;

class Program
{
    private ServiceProvider _services = null!;
    
    public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

    private async Task MainAsync()
    {
        _services = ConfigureServices();
        
        var config = _services.GetRequiredService<ConfigurationManager>();
            
        var client = _services.GetRequiredService<DiscordSocketClient>();    
        var restClient = _services.GetRequiredService<DiscordRestClient>();

        client.Log += LogAsync;
        _services.GetRequiredService<CommandService>().Log += LogAsync;
        
        client.Ready += Ready;
            
        await client.LoginAsync(TokenType.Bot, config.Token);
        await client.StartAsync();

        await restClient.LoginAsync(TokenType.Bot, config.Token);
        
        await Task.Delay(Timeout.Infinite);
    }

    private async Task Ready()
    {
        var client = _services.GetRequiredService<DiscordSocketClient>();
        
        client.AuditLogCreated += _services.GetRequiredService<AuditLogWatcherService>().ClientOnAuditLogCreated;
        
        await _services.GetRequiredService<ActivityService>().Initialize();
        
        await _services.GetRequiredService<PremiumService>().Initialize();
        client.LatencyUpdated += _services.GetRequiredService<PremiumService>().OnHeartbeat;

        await _services.GetRequiredService<AnticheatWarningService>().Initialize();
        client.LatencyUpdated += _services.GetRequiredService<AnticheatWarningService>().OnHeartbeat;
        
        await _services.GetRequiredService<GiveawayService>().Initialize();
        client.LatencyUpdated += _services.GetRequiredService<GiveawayService>().OnHeartbeat;
        
        client.ThreadCreated += _services.GetRequiredService<SuggestionsService>().ClientOnThreadCreated;
        client.ThreadCreated += _services.GetRequiredService<BugReportsService>().ClientOnThreadCreated;
        
        client.ThreadUpdated += _services.GetRequiredService<KudosService>().ClientOnThreadUpdated;
        
        await _services.GetRequiredService<CommandHandlingService>().InitializeAsync();

        await _services.GetRequiredService<GPTService>().Initialize();

        client.Ready -= Ready;
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
            .AddSingleton(new ChatClient(config.GPTModelConfiguration.Model, config.GPTModelConfiguration.Token))
            .AddSingleton<AuditLogWatcherService>()
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