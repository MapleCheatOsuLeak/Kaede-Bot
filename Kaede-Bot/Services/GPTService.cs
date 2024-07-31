using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Discord;
using Discord.Commands;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Models.Database;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using Tiktoken;

namespace Kaede_Bot.Services;

public class GPTService
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private static readonly SemaphoreSlim PromptAsyncLock = new(1, 1);
    
    private readonly IServiceProvider _services;
    private readonly KaedeDbContext _kaedeDbContext;
    
    private Encoder _tokenEncoder;
    private ChatClient _chatClient;
    
    private float _temperature;
    private float _topP;
    private float _frequencyPenalty;
    private float _presencePenalty;
    private int _maxInputTokens;
    private int _maxOutputTokens;
    private int _maxContextMessages;
    private int _rateLimitMinutes;
    private int _rateLimitMessages;
    private string _systemMessage;

    public GPTService(IServiceProvider services)
    {
        _services = services;
        _kaedeDbContext = _services.GetRequiredService<KaedeDbContext>();
    }

    public Task Initialize()
    {
        var config = _services.GetRequiredService<ConfigurationManager>();
        
        _tokenEncoder = ModelToEncoder.For(config.GPTModelConfiguration.Model);
        _chatClient = new ChatClient(config.GPTModelConfiguration.Model, config.GPTModelConfiguration.Token);
        
        _temperature = config.GPTModelConfiguration.Temperature;
        _topP = config.GPTModelConfiguration.TopP;
        _frequencyPenalty = config.GPTModelConfiguration.FrequencyPenalty;
        _presencePenalty = config.GPTModelConfiguration.PresencePenalty;
        _maxInputTokens = config.GPTModelConfiguration.MaxInputTokens;
        _maxOutputTokens = config.GPTModelConfiguration.MaxOutputTokens;
        _maxContextMessages = config.GPTModelConfiguration.MaxContextMessages;
        _rateLimitMinutes = config.GPTModelConfiguration.RateLimitMinutes;
        _rateLimitMessages = config.GPTModelConfiguration.RateLimitMessages;
        _systemMessage = config.GPTModelConfiguration.SystemMessage;
        
        return Task.CompletedTask;
    }

    public async Task HandlePrompt(SocketCommandContext context, int promptPos)
    {
        var prompt = context.Message.Content[promptPos..].Trim();
        
        if (prompt.Length <= 0)
            return;

        if (_tokenEncoder.CountTokens(prompt) > _maxInputTokens)
            return;

        if (_kaedeDbContext.GPTMessages.Count(m => m.UserId == context.User.Id && m.Role == "user" && m.Timestamp.AddMinutes(_rateLimitMinutes) > DateTime.UtcNow) >= _rateLimitMessages)
            return;

        await PromptAsyncLock.WaitAsync();

        try
        {
            using (context.Channel.EnterTypingState())
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(_systemMessage.Replace("{username}", context.User.GetNicknameOrUsername()))
                };

                var messageCount = _kaedeDbContext.GPTMessages.Count(m => m.UserId == context.User.Id);
                if (messageCount >= _maxContextMessages)
                {
                    _kaedeDbContext.GPTMessages.RemoveRange(_kaedeDbContext.GPTMessages
                        .Where(m => m.UserId == context.User.Id).OrderBy(m => m.Timestamp).Take(2));
                    
                    await _kaedeDbContext.SaveChangesAsync();
                }

                foreach (var message in _kaedeDbContext.GPTMessages.Where(m => m.UserId == context.User.Id).OrderBy(m => m.Timestamp))
                {
                    switch (message.Role)
                    {
                        case "user":
                            messages.Add(new UserChatMessage(message.Content));
                            break;
                        case "assistant":
                            messages.Add(new AssistantChatMessage(message.Content));
                            break;
                    }
                }

                messages.Add(new UserChatMessage(context.Message.Content[promptPos..].Trim()));

                _kaedeDbContext.GPTMessages.Add(new GPTMessageModel
                {
                    Id = Guid.NewGuid(),
                    UserId = context.User.Id,
                    Role = "user",
                    Content = prompt,
                    Timestamp = DateTime.UtcNow
                });

                await _kaedeDbContext.SaveChangesAsync();

                var chatCompletion = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                {
                    MaxTokens = _maxInputTokens + _maxOutputTokens,
                    Temperature = _temperature,
                    TopP = _topP,
                    FrequencyPenalty = _frequencyPenalty,
                    PresencePenalty = _presencePenalty
                });

                switch (chatCompletion.Value.FinishReason)
                {
                    case ChatFinishReason.Stop:
                    case ChatFinishReason.Length:
                        messages.Add(new AssistantChatMessage(chatCompletion));

                        _kaedeDbContext.GPTMessages.Add(new GPTMessageModel
                        {
                            Id = Guid.NewGuid(),
                            UserId = context.User.Id,
                            Role = "assistant",
                            Content = chatCompletion.Value.ToString(),
                            Timestamp = DateTime.UtcNow.AddMilliseconds(100)
                        });

                        await _kaedeDbContext.SaveChangesAsync();

                        await context.Message.ReplyAsync(chatCompletion.Value.ToString().Replace("@everyone", "@ everyone").Replace("@here", "@ here"));
                        break;
                    case ChatFinishReason.ContentFilter:
                        await context.Message.ReplyAsync("Filtered.");
                        break;
                    case ChatFinishReason.ToolCalls:
                        break;
                    case ChatFinishReason.FunctionCall:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("ChatCompletion.FinishReason");
                }
            }
        }
        finally
        {
            PromptAsyncLock.Release();
        }
    }
}