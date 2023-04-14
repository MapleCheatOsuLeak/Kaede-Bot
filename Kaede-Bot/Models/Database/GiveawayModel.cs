namespace Kaede_Bot.Models.Database;

public class GiveawayModel
{
    public Guid Id { get; set; }
    public ulong ChannelId { get; set; }
    public ulong MessageId { get; set; }
    public ulong CreatorId { get; set; }
    public ulong HostId { get; set; }
    public string Prize { get; set; }
    public int WinnerCount { get; set; }
    public DateTime EndsAt { get; set; }
}