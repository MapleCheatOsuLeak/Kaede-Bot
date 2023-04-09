using Kaede_Bot.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Kaede_Bot.Database;

public class KaedeDbContext : DbContext
{
    public DbSet<GPTMessage> GPTMessages { get; set; }

    public KaedeDbContext(DbContextOptions<KaedeDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
}