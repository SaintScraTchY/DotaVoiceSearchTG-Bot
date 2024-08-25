using DotaVoiceSearchBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotaVoiceSearchBot.Data.Mapping;

public class HeroMapping : IEntityTypeConfiguration<Hero>
{
    public void Configure(EntityTypeBuilder<Hero> builder)
    {
        builder.ToTable("Heroes");
        
        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.HeroAudios).WithOne(x => x.Hero).HasForeignKey(x => x.HeroId);
    }
}