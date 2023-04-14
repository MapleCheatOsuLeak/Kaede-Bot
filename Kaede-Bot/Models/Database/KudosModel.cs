namespace Kaede_Bot.Models.Database;

public class KudosModel
{
    public Guid Id { get; set; }
    public ulong UserId { get; set; }
    public int Kudos { get; set; }
}