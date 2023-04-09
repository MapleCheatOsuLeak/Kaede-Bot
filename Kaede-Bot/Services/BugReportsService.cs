using Discord;
using Discord.WebSocket;
using Kaede_Bot.Configuration;

namespace Kaede_Bot.Services;

public class BugReportsService
{
    private const string NewBugReportMessage =
        @"Thank you for creating a post in the bug reports section, cutie! 🐾 Just in case, please ensure your report follows our format:
1. A clear and concise description of the bug 🐞
2. Steps to reproduce the issue (if you know them) 🐾
3. Additional details if applicable (screenshots, videos, crash/runtime logs) 📸

Our adorable staff will be reviewing your report soon. While you wait, feel free to explore other parts of our Maple Discord server or chat with our lovely users. We appreciate your help in making Maple even better! 🍁💕";
    
    private readonly ulong _bugReportsChannelId;

    public BugReportsService(ConfigurationManager config) =>
        _bugReportsChannelId = config.ServerChannels.BugReportsChannelId;
    
    public async Task ClientOnThreadCreated(SocketThreadChannel thread)
    {
        #pragma warning disable CS4014
        Task.Run(async () =>
        #pragma warning restore CS4014
        {
            if (thread.ParentChannel.Id == _bugReportsChannelId)
            {
                var message = (await thread.GetMessagesAsync(1).FirstAsync()).First();
                if (message.Author.IsBot)
                    return;
                
                await thread.SendMessageAsync(NewBugReportMessage);
            }
        });
    }
}