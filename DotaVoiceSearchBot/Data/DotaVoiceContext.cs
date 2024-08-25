using DotaVoiceSearchBot.Data.Mapping;
using DotaVoiceSearchBot.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotaVoiceSearchBot.Data;

public class DotaVoiceContext : DbContext
{
    public DbSet<Hero> Heroes { get; set; }
    public DbSet<HeroAudio> HeroAudios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeroAudioMapping).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(Constants.CnnString);
    }
}