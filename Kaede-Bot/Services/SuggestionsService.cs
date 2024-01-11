using Discord;
using Discord.WebSocket;
using Kaede_Bot.Configuration;

namespace Kaede_Bot.Services;

public class SuggestionsService
{
    private const string NewSuggestionMessage =
        @"Thank you for sharing your purrfect suggestion with us! 😻 Our team will review your idea, and if it sparks interest, we might consider implementing it in the future. Feel free to continue discussing your suggestion here, and remember, the more support and feedback it receives from our lovely community, the better!

To show your support for a suggestion, don't forget to leave a reaction on the post! You can either vote with a ✅ if you think it's a great idea, or a ❌ if you think it's not quite right. The more reactions a suggestion receives, the easier it is for us to gauge the interest of our users!

Stay pawsitive! 🍁💕";
    
    private readonly ulong _suggestionsChannelId;

    public SuggestionsService(ConfigurationManager config) =>
        _suggestionsChannelId = config.ServerChannels.SuggestionsChannelId;
    
    public Task ClientOnThreadCreated(SocketThreadChannel thread)
    {
        _ = Task.Run(async () =>
        {
            if (thread.ParentChannel.Id == _suggestionsChannelId)
            {
                var message = (await thread.GetMessagesAsync(1).FirstAsync()).First();
                if (message.Author.IsBot)
                    return;

                await message.AddReactionAsync(new Emoji("✅"));
                await message.AddReactionAsync(new Emoji("❌"));

                await thread.SendMessageAsync(NewSuggestionMessage);
            }
        });
        
        return Task.CompletedTask;
    }
}