using Discord;
using Discord.WebSocket;
using Kaede_Bot.Configuration;

namespace Kaede_Bot.Services;

public class ActivityService
{
    private const int ActivityDuration = 10000;

    private readonly DiscordSocketClient _client;
    private readonly List<Game> _activities;
    
    public ActivityService(DiscordSocketClient client, ConfigurationManager config)
    {
        _client = client;
        _activities = config.Activities.Select(a => new Game(a)).ToList();
    }

    public Task Initialize()
    {
        if (_activities.Count == 0)
            return Task.CompletedTask;
        
        _ = Task.Run(async () =>
        {
            int currentActivityIndex = 0;
            while (true)
            {
                await _client.SetActivityAsync(_activities[currentActivityIndex]);

                await Task.Delay(ActivityDuration);

                currentActivityIndex = currentActivityIndex + 1 < _activities.Count ? currentActivityIndex + 1 : 0;
            }
            // ReSharper disable once FunctionNeverReturns
        });
        
        return Task.CompletedTask;
    }
}