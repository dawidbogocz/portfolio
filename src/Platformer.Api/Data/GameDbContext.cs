using Microsoft.EntityFrameworkCore;
using Platformer.Api.Models;

namespace Platformer.Api.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<PlayerScore> PlayerScores { get; set; } = null!;
}