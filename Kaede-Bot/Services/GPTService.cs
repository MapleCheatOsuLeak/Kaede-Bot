using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Discord;
using Discord.Commands;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Models.Database;
using Kaede_Bot.Models.Web;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using Tiktoken;

namespace Kaede_Bot.Services;

public class GPTService
{
    private static readonly SemaphoreSlim PromptAsyncLock = new(1, 1);
    
    private static readonly ChatTool getArticleListTool = ChatTool.CreateFunctionTool(
        functionName: nameof(getArticleList),
        functionDescription: "Retrieves article filename list. Articles contain all sorts of information about Maple. For example, information about the Maple project, various tutorials, set up guides, internal policies, troubleshooting guides, rules, contacts, and other useful information."
    );
    
    private static readonly ChatTool getArticlesTool = ChatTool.CreateFunctionTool(
        functionName: nameof(getArticles),
        functionDescription: "Retrieves an array of articles.",
        functionParameters: BinaryData.FromString("""
        {
          "type": "object",
          "properties": {
              "fileNames": {
                  "type": "array",
                  "description": "Filenames of articles to retrieve.",
                  "items": {
                      "type": "string"
                  }
              }
          },
          "required": [ "fileNames" ]
        }
        """)
    );
    
    private static readonly ChatTool getMapleUserInfoTool = ChatTool.CreateFunctionTool(
        functionName: nameof(getMapleUserInfo),
        functionDescription: "Retrieves information about this user's Maple account. This information includes Maple User Id, Join date, Software subscriptions and their expiration."
    );
    
    private static readonly ChatTool getMapleProductsInfoTool = ChatTool.CreateFunctionTool(
        functionName: nameof(getMapleProductsInfo),
        functionDescription: "Retrieves information about existing Maple products and their prices in EUR."
    );
    
    private static readonly ChatTool getMapleStatusTool = ChatTool.CreateFunctionTool(
        functionName: nameof(getMapleStatus),
        functionDescription: "Retrieves online user count and status information about every Maple product (0 = Undetected, 1 = Outdated, 2 = Detected, 3 = Unknown)."
    );
    
    private readonly IServiceProvider _services;
    private readonly KaedeDbContext _kaedeDbContext;
    private readonly HttpClient _httpClient;
    
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
        _httpClient = services.GetRequiredService<HttpClient>();
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

                var requiresAction = true;
                while (requiresAction)
                {
                    var chatCompletion = await _chatClient.CompleteChatAsync(messages, new ChatCompletionOptions
                    {
                        Tools = { getArticleListTool, getArticlesTool, getMapleUserInfoTool, getMapleProductsInfoTool, getMapleStatusTool },
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
                            requiresAction = false;
                            
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
                            requiresAction = false;
                            break;
                        case ChatFinishReason.ToolCalls:
                            messages.Add(new AssistantChatMessage(chatCompletion));
                            
                            foreach (var toolCall in chatCompletion.Value.ToolCalls)
                            {
                                switch (toolCall.FunctionName)
                                {
                                    case nameof(getArticleList):
                                    {
                                        var toolResult = getArticleList();
                                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }
                                    case nameof(getArticles):
                                    {
                                        using var argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                        var hasFileName = argumentsJson.RootElement.TryGetProperty("fileNames", out var fileName);
                                        if (!hasFileName)
                                            throw new ArgumentNullException(nameof(fileName), "The fileNames argument is required.");
                                        
                                        var toolResult = getArticles(fileName.EnumerateArray().Select(a => a.ToString()).ToArray());
                                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }
                                    case nameof(getMapleUserInfo):
                                    {
                                        var toolResult = await getMapleUserInfo(context.User.Id);
                                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }
                                    case nameof(getMapleProductsInfo):
                                    {
                                        var toolResult = await getMapleProductsInfo();
                                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }
                                    case nameof(getMapleStatus):
                                    {
                                        var toolResult = await getMapleStatus();
                                        messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }
                                    default:
                                    {
                                        throw new NotImplementedException("Unknown tool call.");
                                    }
                                }
                            }
                            break;
                        case ChatFinishReason.FunctionCall:
                            requiresAction = false;
                            break;
                        default:
                            throw new NotImplementedException(chatCompletion.Value.FinishReason.ToString());
                    }
                }
            }
        }
        finally
        {
            PromptAsyncLock.Release();
        }
    }

    private string getArticleList()
    {
        return Directory.Exists("Articles") ? JsonSerializer.Serialize(Directory.EnumerateFiles("Articles").Select(Path.GetFileNameWithoutExtension).ToArray()) : "[]";
    }

    private string getArticles(string[] fileNames)
    {
        return JsonSerializer.Serialize((from fileName in fileNames select $@"Articles\{fileName}.txt" into articlePath where File.Exists(articlePath) select File.ReadAllText(articlePath)).ToArray());
    }
    
    private async Task<string> getMapleUserInfo(ulong userId)
    {
        var response = await _httpClient.GetAsync($"https://maple.software/backend/api/discord?t=0&u={userId}");
        if (!response.IsSuccessStatusCode)
            return "Server error.";
        
        var userinfo = await response.Content.ReadFromJsonAsync<UserInfoModel>();
        if (userinfo is { Code: 0 })
            return await response.Content.ReadAsStringAsync();
            
        return "Discord account is not linked to Maple account.";
    }
    
    private async Task<string> getMapleProductsInfo()
    {
        var response = await _httpClient.GetAsync($"https://maple.software/backend/api/discord?t=4");
        if (!response.IsSuccessStatusCode)
            return "Server error.";
        
        var userinfo = await response.Content.ReadFromJsonAsync<ProductsInfoModel>();
        if (userinfo is { Code: 0 })
            return await response.Content.ReadAsStringAsync();
            
        return "Server error.";
    }

    private async Task<string> getMapleStatus()
    {
        var response = await _httpClient.GetAsync($"https://maple.software/backend/api/discord?t=3");
        if (!response.IsSuccessStatusCode)
            return "Server error.";
        
        var userinfo = await response.Content.ReadFromJsonAsync<ProductsInfoModel>();
        if (userinfo is { Code: 0 })
            return await response.Content.ReadAsStringAsync();
            
        return "Server error.";
    }
}