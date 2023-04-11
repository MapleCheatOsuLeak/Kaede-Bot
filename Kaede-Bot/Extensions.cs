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
        return $"{user.GetNicknameOrUsername()}#{user.Discriminator}";
    }
}