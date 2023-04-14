namespace Kaede_Bot.Models.Database;

public class KudosUsageModel
{
    public Guid Id { get; set; }
    public ulong SenderId { get; set; }
    public ulong RecipientId { get; set; }
    public DateTime SentAt { get; set; }
}