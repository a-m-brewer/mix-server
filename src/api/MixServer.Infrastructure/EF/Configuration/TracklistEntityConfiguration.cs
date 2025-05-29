using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Infrastructure.EF.Configuration;

public class TracklistEntityConfiguration : IEntityTypeConfiguration<TracklistEntity>
{
    public void Configure(EntityTypeBuilder<TracklistEntity> builder)
    {
        builder.HasKey(k => k.Id);

        builder.HasOne(f => f.Node)
            .WithOne(o => o.Tracklist)
            .HasForeignKey<TracklistEntity>(f => f.NodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}