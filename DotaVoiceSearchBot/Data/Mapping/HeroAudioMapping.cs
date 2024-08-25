using DotaVoiceSearchBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DotaVoiceSearchBot.Data.Mapping;

public class HeroAudioMapping : IEntityTypeConfiguration<HeroAudio>
{
    public void Configure(EntityTypeBuilder<HeroAudio> builder)
    {
        builder.ToTable("HeroAudios");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.SearchVector).HasMethod("GIN");
        builder.Property(x=>x.SearchVector).HasComputedColumnSql("to_tsvector('english', \"AudioText\")",stored:true);

        builder.Property(x => x.AudioText).IsRequired();
        builder.Property(x => x.AudioWebUrl).IsRequired();
        builder.Property(x => x.AudioFilePath).IsRequired(false);
        builder.Property(x => x.TelegramFileId).IsRequired(false);
        builder.HasOne(x => x.Hero).WithMany(x => x.HeroAudios).HasForeignKey(x => x.HeroId);
    }
}