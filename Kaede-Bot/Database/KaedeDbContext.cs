using Kaede_Bot.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Kaede_Bot.Database;

public class KaedeDbContext : DbContext
{
    public DbSet<GPTMessageModel> GPTMessages { get; set; }

    public KaedeDbContext(DbContextOptions<KaedeDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
}