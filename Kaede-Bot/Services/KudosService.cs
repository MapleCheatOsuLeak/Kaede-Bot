using Discord;
using Discord.WebSocket;
using Kaede_Bot.Configuration;
using Kaede_Bot.Database;
using Kaede_Bot.Models.Database;

namespace Kaede_Bot.Services;

public class KudosService
{
    private const string PostClosedMessage = 
        @"🌸 Meow, it looks like this support post is all wrapped up! Thank you for letting us help you, nya!
If you want to show your appreciation for our staff who assisted you, feel free to use the `!kudos send <user_mention_or_id>` command to give them a pat on the back!
Remember, Maple family is here for you anytime! Have a purr-fect day! 🌸";

    private readonly KaedeDbContext _kaedeDbContext;
    private readonly ulong _supportChannelId;
    
    public KudosService(KaedeDbContext kaedeDbContext, ConfigurationManager config)
    {
        _kaedeDbContext = kaedeDbContext;
        _supportChannelId = config.ServerChannels.SupportChannelId;
    }

    public List<KudosModel> GetAllKudos()
    {
        return _kaedeDbContext.Kudos.ToList();
    }

    public int GetUserKudos(IUser user)
    {
        var kudosUser = _kaedeDbContext.Kudos.SingleOrDefault(k => k.UserId == user.Id);
        if (kudosUser == null)
            return 0;

        return kudosUser.Kudos;
    }

    public bool CanSendKudos(IUser sender, IUser recipient)
    {
        var expiredUsage = _kaedeDbContext.KudosUsage.Where(k => k.SentAt.AddDays(1) <= DateTime.Now);
        _kaedeDbContext.KudosUsage.RemoveRange(expiredUsage);
        _kaedeDbContext.SaveChanges();
        
        return !_kaedeDbContext.KudosUsage.Any(k => k.SenderId == sender.Id && k.RecipientId == recipient.Id);
    }

    public DateTime GetKudosCooldown(IUser sender, IUser recipient)
    {
        return _kaedeDbContext.KudosUsage.Single(k => k.SenderId == sender.Id && k.RecipientId == recipient.Id).SentAt.AddDays(1);
    }

    public async Task SendKudos(IUser sender, IUser recipient)
    {
        if (!CanSendKudos(sender, recipient))
            return;
        
        var kudosRecipient = _kaedeDbContext.Kudos.SingleOrDefault(k => k.UserId == recipient.Id);
        if (kudosRecipient == null)
        {
            _kaedeDbContext.Kudos.Add(new KudosModel
            {
                Id = Guid.NewGuid(),
                UserId = recipient.Id,
                Kudos = 1
            });
        }
        else
            (await _kaedeDbContext.Kudos.FindAsync(kudosRecipient.Id))!.Kudos += 1;

        _kaedeDbContext.KudosUsage.Add(new KudosUsageModel
        {
            Id = Guid.NewGuid(),
            SenderId = sender.Id,
            RecipientId = recipient.Id,
            SentAt = DateTime.UtcNow
        });

        await _kaedeDbContext.SaveChangesAsync();
    }

    public async Task ClientOnThreadUpdated(Cacheable<SocketThreadChannel, ulong> arg1, SocketThreadChannel arg2)
    {
        if (arg2.ParentChannel.Id != _supportChannelId)
            return;

        if (arg1.HasValue && !arg1.Value.IsLocked && arg2.IsLocked)
            await arg2.SendMessageAsync(PostClosedMessage);
    }
}