using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class TrackEntityConfiguration : IEntityTypeConfiguration<TrackEntity>
{
    public void Configure(EntityTypeBuilder<TrackEntity> builder)
    {
        builder.HasKey(k => k.Id);

        builder.HasOne(o => o.Cue)
            .WithMany(m => m.Tracks)
            .HasForeignKey(f => f.CueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}