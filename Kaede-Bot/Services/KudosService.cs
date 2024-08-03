using System.Text.Json;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Models.Database;
using Kaede_Bot.Models.Internal;
using OpenAI.Chat;

namespace Kaede_Bot.Services;

public class KudosService
{
    private const string PostClosedMessage = 
        @"🌸 Meow, it looks like this support post is all wrapped up! Thank you for letting us help you, nya!
Remember, Maple family is here for you anytime! Have a purr-fect day! 🌸

{KudosRewards}";

    private readonly DiscordRestClient _restClient;
    private readonly KaedeDbContext _kaedeDbContext;
    private readonly ChatClient _chatClient;
    private readonly int _baseKudosValue;
    private readonly float _minContributionFactorThreshold;
    private readonly string _systemMessage;
    private readonly ulong _supportChannelId;
    
    public KudosService(DiscordRestClient restClient, KaedeDbContext kaedeDbContext, ChatClient chatClient, ConfigurationManager config)
    {
        _restClient = restClient;
        _kaedeDbContext = kaedeDbContext;
        _chatClient = chatClient;
        _baseKudosValue = config.KudosConfiguration.BaseKudosValue;
        _minContributionFactorThreshold = config.KudosConfiguration.MinContributionFactorThreshold;
        _systemMessage = config.KudosConfiguration.GTPSystemMessage;
        _supportChannelId = config.ServerChannels.SupportChannelId;
    }

    public List<KudosModel> GetAllKudos()
    {
        return _kaedeDbContext.Kudos.ToList().Where(k => k.Kudos > 0).ToList();
    }

    public int GetUserKudos(IUser user)
    {
        var kudosUser = _kaedeDbContext.Kudos.SingleOrDefault(k => k.UserId == user.Id);
        if (kudosUser == null)
            return 0;

        return kudosUser.Kudos;
    }

    public async Task<bool> SendKudos(IUser sender, IUser recipient, int amount)
    {
        var kudosSender = _kaedeDbContext.Kudos.SingleOrDefault(k => k.UserId == sender.Id);
        if (kudosSender == null || kudosSender.Kudos < amount)
            return false;
        
        var kudosRecipient = _kaedeDbContext.Kudos.SingleOrDefault(k => k.UserId == recipient.Id);
        if (kudosRecipient == null)
        {
            _kaedeDbContext.Kudos.Add(new KudosModel
            {
                Id = Guid.NewGuid(),
                UserId = recipient.Id,
                Kudos = amount
            });
        }
        else
        {
            kudosRecipient.Kudos += amount;
            _kaedeDbContext.Kudos.Update(kudosRecipient);
        }

        kudosSender.Kudos -= amount;
        _kaedeDbContext.Kudos.Update(kudosSender);

        await _kaedeDbContext.SaveChangesAsync();

        return true;
    }

    public async Task ClientOnThreadUpdated(Cacheable<SocketThreadChannel, ulong> arg1, SocketThreadChannel arg2)
    {
        try
        {
            if (arg2.ParentChannel.Id != _supportChannelId)
                return;

            if (arg1.HasValue && !arg1.Value.IsLocked && arg2.IsLocked)
            {
                var messagesPages = await arg2.GetMessagesAsync(1000).Reverse().ToListAsync();
                var messages = new List<IMessage>();
                foreach (var page in messagesPages)
                    messages.AddRange(page.Reverse());
                
                if (!messages.Any(m => m.Author.IsBot && m.Content.Contains("Meow, it looks like this support post is all wrapped up! Thank you for letting us help you, nya!")))
                {
                    var messagesJson = JsonSerializer.Serialize(messages.Where(m => !m.Author.IsBot && !string.IsNullOrEmpty(m.Content)).Select(m => new
                    {
                        UserId = m.Author.Id,
                        Message = m.Content
                    }));
                
                    var gptMessages = new List<ChatMessage>
                    {
                        new SystemChatMessage(_systemMessage.Replace("{AuthorId}", arg2.Owner.Id.ToString())),
                        new UserChatMessage(messagesJson)
                    };
                
                    var chatCompletion = await _chatClient.CompleteChatAsync(gptMessages);
                    if (chatCompletion.Value.FinishReason == ChatFinishReason.Stop)
                    {
                        var kudosEvaluation = JsonSerializer.Deserialize<KudosEvaluationModel>(chatCompletion.Value.ToString());

                        if (kudosEvaluation != null)
                        {
                            var eligibleContributors = kudosEvaluation.Contributions.Where(u => u.UserId != arg2.Owner.Id && u.ContributionFactor >= _minContributionFactorThreshold);
                            if (eligibleContributors.Any())
                            {
                                var kudosRewards = "Kudos rewards:\n";
                                foreach (var contribution in eligibleContributors)
                                {
                                    var kudosValue = (int)Math.Round(_baseKudosValue * kudosEvaluation.ComplexityMultiplier * contribution.ContributionFactor);
                                    var username = (await _restClient.GetUserAsync(contribution.UserId)).GetFullname();
                                
                                    kudosRewards += $"**{username}**: {kudosValue} kudos\n";
                                
                                    var kudosEntry = _kaedeDbContext.Kudos.SingleOrDefault(k => k.UserId == contribution.UserId);
                                    if (kudosEntry == null)
                                    {
                                        _kaedeDbContext.Kudos.Add(new KudosModel
                                        {
                                            Id = Guid.NewGuid(),
                                            UserId = contribution.UserId,
                                            Kudos = kudosValue
                                        });
                                    }
                                    else
                                    {
                                        kudosEntry.Kudos += kudosValue;
                                        _kaedeDbContext.Kudos.Update(kudosEntry);
                                    }
                                }

                                await _kaedeDbContext.SaveChangesAsync();
                            
                                await arg2.SendMessageAsync(PostClosedMessage.Replace("{KudosRewards}", kudosRewards).Trim());
                            }
                            else
                            {
                                await arg2.SendMessageAsync(PostClosedMessage.Replace("{KudosRewards}", string.Empty).Trim());
                            }
                        }
                        else
                        {
                            await arg2.SendMessageAsync(PostClosedMessage.Replace("{KudosRewards}", string.Empty).Trim());
                        }
                    }
                    else
                        await arg2.SendMessageAsync(PostClosedMessage.Replace("{KudosRewards}", string.Empty).Trim());
                }
                else
                    await arg2.SendMessageAsync(PostClosedMessage.Replace("{KudosRewards}", string.Empty).Trim());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            
            await arg2.SendMessageAsync(PostClosedMessage.Replace("{KudosRewards}", string.Empty).Trim());
        }
    }
}