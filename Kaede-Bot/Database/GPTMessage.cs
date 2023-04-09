namespace Kaede_Bot.Database;

public class GPTMessage
{
    public Guid Id { get; set; }
    public ulong UserId { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}