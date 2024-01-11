using Discord;
using Discord.WebSocket;

namespace Kaede_Bot;

public static class Extensions
{
    public static string GetNicknameOrUsername(this IUser user)
    {
        return user is SocketGuildUser guildUser ? guildUser.Nickname ?? user.Username : user.Username;
    }

    public static string GetFullname(this IUser user)
    {
        return $"{user.GetNicknameOrUsername()}";
    }
    
    public static string GetHumanReadableString(this DateTimeOffset? dateTime)
    {
        if (dateTime.HasValue)
            return $"{dateTime.Value.UtcDateTime.ToString($@"MMMM ddnn, yyyy", Constants.Culture)}".Replace("nn", DaySuffix(dateTime.Value.Day));
            
        return $"{DateTime.UnixEpoch.ToString($@"MMMM ddnn, yyyy", Constants.Culture)}".Replace("nn", DaySuffix(DateTime.UnixEpoch.Day));
    }
    
    public static string GetHumanReadableString(this DateTimeOffset dateTime)
    {
        return $"{dateTime.UtcDateTime.ToString($@"MMMM ddnn, yyyy", Constants.Culture)}".Replace("nn", DaySuffix(dateTime.Day));
    }
    
    private static string DaySuffix(int day)
    {
        switch (day)
        {
            case 1:
            case 21:
            case 31:
                return "st";
            case 2:
            case 22:
                return "nd";
            case 3:
            case 23:
                return "rd";
            default:
                return "th";
        }
    }
}