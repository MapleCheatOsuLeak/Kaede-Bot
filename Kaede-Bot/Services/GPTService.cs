using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Discord;
using Discord.Commands;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Models.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Kaede_Bot.Services;

public class GPTService
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private static readonly SemaphoreSlim PromptAsyncLock = new(1, 1);
    
    private readonly IServiceProvider _services;
    private readonly KaedeDbContext _kaedeDbContext;
    private readonly HttpClient _httpClient;

    private string _model;
    private float _temperature;
    private int _maxTokens;
    private string _systemMessage;

    public GPTService(IServiceProvider services)
    {
        _services = services;
        _kaedeDbContext = _services.GetRequiredService<KaedeDbContext>();
        _httpClient = new();
    }

    public async Task InitializeAsync()
    {
        var config = _services.GetRequiredService<ConfigurationManager>();
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.GPTModelConfiguration.Token}");

        _model = config.GPTModelConfiguration.Model;
        _temperature = config.GPTModelConfiguration.Temperature;
        _maxTokens = config.GPTModelConfiguration.MaxTokens;
        _systemMessage = config.GPTModelConfiguration.SystemMessage;
    }

    public async Task HandlePrompt(SocketCommandContext context, int promptPos)
    {
        if (context.Message.Content[promptPos..].Trim().Length <= 0)
            return;

        if (context.Message.Content[promptPos..].Trim().Length > 200)
            return;

        if (_kaedeDbContext.GPTMessages.Count(m => m.UserId == context.User.Id && m.Role == "user" && m.Timestamp.AddMinutes(5) > DateTime.UtcNow) >= 3)
            return;

        await PromptAsyncLock.WaitAsync();

        try
        {
            using (context.Channel.EnterTypingState())
            {
                var messages = new List<Message>
                {
                    new()
                    {
                        Role = "system",
                        Content = _systemMessage.Replace("{username}", context.User.GetNicknameOrUsername())
                    }
                };

                int messageCount = _kaedeDbContext.GPTMessages.Count(m => m.UserId == context.User.Id);
                if (messageCount >= 10)
                {
                    _kaedeDbContext.GPTMessages.RemoveRange(_kaedeDbContext.GPTMessages
                        .Where(m => m.UserId == context.User.Id).OrderBy(m => m.Timestamp).Take(messageCount - 8));
                    await _kaedeDbContext.SaveChangesAsync();
                }

                messages.AddRange(_kaedeDbContext.GPTMessages.Where(m => m.UserId == context.User.Id)
                    .OrderBy(m => m.Timestamp).Select(m => new Message()
                    {
                        Role = m.Role,
                        Content = m.Content
                    }));

                var userMessage = new Message
                {
                    Role = "user",
                    Content = context.Message.Content[promptPos..].Trim()
                };

                messages.Add(userMessage);

                _kaedeDbContext.GPTMessages.Add(new GPTMessageModel
                {
                    Id = Guid.NewGuid(),
                    UserId = context.User.Id,
                    Role = userMessage.Role,
                    Content = userMessage.Content,
                    Timestamp = DateTime.UtcNow
                });

                await _kaedeDbContext.SaveChangesAsync();

                var requestData = new Request
                {
                    ModelId = _model,
                    Messages = messages,
                    Temperature = _temperature,
                    MaxTokens = _maxTokens
                };

                using var response = await _httpClient.PostAsJsonAsync(Endpoint, requestData);
                ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();
                var choices = responseData?.Choices ?? new List<Choice>();
                if (choices.Count != 0)
                {
                    messages.Add(choices[0].Message);

                    _kaedeDbContext.GPTMessages.Add(new GPTMessageModel
                    {
                        Id = Guid.NewGuid(),
                        UserId = context.User.Id,
                        Role = choices[0].Message.Role,
                        Content = choices[0].Message.Content,
                        Timestamp = DateTime.UtcNow.AddMilliseconds(100)
                    });

                    await _kaedeDbContext.SaveChangesAsync();

                    await context.Message.ReplyAsync(choices[0].Message.Content.Replace("@everyone", "@ everyone")
                        .Replace("@here", "@ here"));
                }
            }
        }
        finally
        {
            PromptAsyncLock.Release();
        }
    }
}

public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";
}

public class Request
{
    [JsonPropertyName("model")]
    public string ModelId { get; set; } = "";
    
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();
    
    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 1;
    
    [JsonPropertyName("max_tokens")]
    public float MaxTokens { get; set; } = 150;
}
 
public class ResponseData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("object")]
    public string Object { get; set; } = "";
    
    [JsonPropertyName("created")]
    public ulong Created { get; set; }
    
    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new();
    
    [JsonPropertyName("usage")]
    public Usage Usage { get; set; } = new();
}
 
public class Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    
    [JsonPropertyName("message")]
    public Message Message { get; set; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = "";
}
 
public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}