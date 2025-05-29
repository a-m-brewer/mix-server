using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class CueEntityConfiguration : IEntityTypeConfiguration<CueEntity>
{
    public void Configure(EntityTypeBuilder<CueEntity> builder)
    {
        builder.HasKey(k => k.Id);
        
        builder.HasOne(o => o.Tracklist)
            .WithMany(m => m.Cues)
            .HasForeignKey(fk => fk.TracklistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}